using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using Jade.NativeBuild.Build;
using Jade.NativeBuild.Configuration;

namespace Jade.NativeBuild.Tools;

/// <summary>
/// Installs the xmake and CMake of <c>build/versions.json</c> into <c>artifacts/native/tools/</c>, from
/// their release archives checked against the pinned SHA-256, and puts them first on <c>PATH</c>.
/// </summary>
/// <param name="directory">The directory the tools are installed into.</param>
/// <param name="host">The machine the tools run on.</param>
/// <param name="output">Receives a line for each installed tool.</param>
/// <remarks>
/// CI installs the tools this way on every runner and in the glibc 2.28 container, so that the
/// versions are those of the repository rather than of a runner image. A local build can use its
/// own tools instead; <see cref="ToolRequirements"/> checks their versions either way.
/// </remarks>
internal sealed class ToolInstaller(string directory, HostPlatform host, TextWriter output)
{
    /// <summary>The name of the file that records the hash of the archive an installation came from.</summary>
    private const string StampFileName = ".sha256";

    /// <summary>The suffix of the directory an installation is written to before it is complete.</summary>
    private const string StagingSuffix = ".partial";

    /// <summary>Installs xmake and CMake, unless the same archives are installed already.</summary>
    /// <param name="toolchains">The pinned tools.</param>
    /// <param name="cancellationToken">Stops the downloads and the build of xmake.</param>
    /// <returns>A task that completes when both tools are on <c>PATH</c>.</returns>
    /// <exception cref="InvalidDataException">The host has no archive, or an archive does not match its hash.</exception>
    /// <exception cref="CommandFailedException">The build of xmake from its sources fails.</exception>
    public async Task InstallAsync(PinnedToolchains toolchains, CancellationToken cancellationToken)
    {
        var xmake = await InstallAsync("xmake", toolchains.Xmake, cancellationToken).ConfigureAwait(false);
        var cmake = await InstallAsync("cmake", toolchains.Cmake, cancellationToken).ConfigureAwait(false);

        // The build's own process resolves the programs it starts through its PATH, and passes it on.
        var path = Environment.GetEnvironmentVariable("PATH");

        Environment.SetEnvironmentVariable("PATH", string.Join(Path.PathSeparator, Path.GetDirectoryName(xmake), Path.GetDirectoryName(cmake), path));
    }

    /// <summary>Installs a tool from its archive for the host.</summary>
    /// <param name="name">The name of the tool's executable, without extension.</param>
    /// <param name="tool">The pinned tool.</param>
    /// <param name="cancellationToken">Stops the download and the build.</param>
    /// <returns>The path of the installed executable.</returns>
    private async Task<string> InstallAsync(string name, PinnedTool tool, CancellationToken cancellationToken)
    {
        var asset = tool.Assets.TryGetValue(host.Key, out var specific) ? specific
            : tool.Assets.TryGetValue(host.OperatingSystemKey, out var shared) ? shared
            : throw new InvalidDataException($"build/versions.json has no {name} archive for {host.Key}.");
        var target = Path.Combine(directory, $"{name}-{tool.Version}-{host.Key}");
        var stamp = Path.Combine(target, StampFileName);

        if (File.Exists(stamp) && string.Equals(await File.ReadAllTextAsync(stamp, cancellationToken).ConfigureAwait(false), asset.Sha256, StringComparison.Ordinal))
        {
            return FindExecutable(target, name);
        }

        var staging = target + StagingSuffix;

        DeleteDirectory(staging);
        _ = Directory.CreateDirectory(staging);

        await output.WriteLineAsync($"Installing {name} {tool.Version} from {asset.Url}".AsMemory(), cancellationToken).ConfigureAwait(false);

        var archive = Path.Combine(staging, Path.GetFileName(asset.Url.AbsolutePath));

        await DownloadAsync(asset, archive, cancellationToken).ConfigureAwait(false);

        if (archive.EndsWith(".zip", StringComparison.Ordinal))
        {
            await ZipFile.ExtractToDirectoryAsync(archive, staging, cancellationToken).ConfigureAwait(false);
            File.Delete(archive);
        }
        else if (archive.EndsWith(".tar.gz", StringComparison.Ordinal))
        {
            await ExtractTarGzAsync(archive, staging, cancellationToken).ConfigureAwait(false);
            File.Delete(archive);
        }
        else
        {
            throw new InvalidDataException($"{asset.Url} is neither a .zip nor a .tar.gz archive.");
        }

        // xmake's source archive, the one Linux and macOS install (build/versions.json).
        if (name == "xmake" && host.Platform != NativePlatform.Windows)
        {
            await BuildXmakeAsync(staging, cancellationToken).ConfigureAwait(false);
        }

        await File.WriteAllTextAsync(Path.Combine(staging, StampFileName), asset.Sha256, cancellationToken).ConfigureAwait(false);
        DeleteDirectory(target);
        Directory.Move(staging, target);

        return FindExecutable(target, name);
    }

    /// <summary>Downloads an archive and checks its hash.</summary>
    /// <param name="asset">The archive.</param>
    /// <param name="path">The file to write.</param>
    /// <param name="cancellationToken">Stops the download.</param>
    /// <returns>A task that completes when the archive is written and checked.</returns>
    /// <exception cref="InvalidDataException">The content does not match the pinned hash.</exception>
    private static async Task DownloadAsync(ToolAsset asset, string path, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(asset.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var file = File.Create(path);

        await using (file.ConfigureAwait(false))
        {
            await response.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
            file.Position = 0;

            var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(file, cancellationToken).ConfigureAwait(false));

            if (!string.Equals(hash, asset.Sha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"{asset.Url} has the SHA-256 {hash}, but build/versions.json pins {asset.Sha256}.");
            }
        }
    }

    /// <summary>Extracts a gzip-compressed tar archive.</summary>
    /// <param name="archive">The archive.</param>
    /// <param name="destination">The directory to extract into.</param>
    /// <param name="cancellationToken">Stops the extraction.</param>
    /// <returns>A task that completes when the archive is extracted.</returns>
    private static async Task ExtractTarGzAsync(string archive, string destination, CancellationToken cancellationToken)
    {
        var file = File.OpenRead(archive);

        await using (file.ConfigureAwait(false))
        {
            var gzip = new GZipStream(file, CompressionMode.Decompress);

            await using (gzip.ConfigureAwait(false))
            {
                await TarFile.ExtractToDirectoryAsync(gzip, destination, overwriteFiles: false, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Builds xmake from its source archive and installs it into the same directory.</summary>
    /// <param name="staging">The directory the sources were extracted into.</param>
    /// <param name="cancellationToken">Stops the build.</param>
    /// <returns>A task that completes when xmake is installed under <c>bin/</c>.</returns>
    /// <remarks>
    /// xmake publishes no Linux arm64 binary, and its macOS bundle keeps its scripts inside the
    /// executable, where they are not files that other programs can run (the iOS assembler is
    /// <c>scripts/gas-preprocessor.pl</c>): both build the same sources instead.
    /// </remarks>
    private static async Task BuildXmakeAsync(string staging, CancellationToken cancellationToken)
    {
        var sources = Directory.EnumerateDirectories(staging).Single();
        var jobs = Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture);

        // A relative program name would be resolved from the build's own directory, not the sources.
        await Command.RunAsync(Path.Combine(sources, "configure"), ["--prefix=" + staging], environment: null, sources, cancellationToken).ConfigureAwait(false);
        await Command.RunAsync("make", ["-j" + jobs], environment: null, sources, cancellationToken).ConfigureAwait(false);
        await Command.RunAsync("make", ["install"], environment: null, sources, cancellationToken).ConfigureAwait(false);

        // The source tree holds another xmake executable, built in place.
        DeleteDirectory(sources);
    }

    /// <summary>Finds the single executable of a tool in its installation.</summary>
    /// <param name="root">The installation directory.</param>
    /// <param name="name">The name of the executable, without extension.</param>
    /// <returns>The path of the executable.</returns>
    /// <exception cref="InvalidDataException">The installation has no such executable, or several.</exception>
    /// <remarks>
    /// A file in a <c>bin</c> directory wins: CMake's archives also hold a shell completion script
    /// named <c>cmake</c>, while xmake's Windows archive has its executable at the top.
    /// </remarks>
    private string FindExecutable(string root, string name)
    {
        var fileName = host.Platform == NativePlatform.Windows ? name + ".exe" : name;
        var files = Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).ToList();
        var inBin = files.Where(static file => Path.GetFileName(Path.GetDirectoryName(file)) == "bin").ToList();
        var candidates = inBin.Count > 0 ? inBin : files;

        return candidates.Count == 1
            ? candidates[0]
            : throw new InvalidDataException($"'{root}' holds {candidates.Count} files named {fileName} instead of one.");
    }

    /// <summary>Deletes a directory and its content if it exists.</summary>
    /// <param name="path">The directory.</param>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
