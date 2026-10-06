using Jade.NativeBuild.Tools;

namespace Jade.NativeBuild.Build;

/// <summary>
/// Lists the directories of the pinned sources that the CMake builds of a target compiled or
/// included, for the check of <c>THIRD-PARTY-NOTICES.md</c> against each target (ADR 0031).
/// </summary>
/// <remarks>
/// Ninja's dependency log records the source and headers of every object it compiled. It is read
/// with the Ninja that wrote it: another version rejects the log and starts it over. miniaudio and
/// the shim are built by xmake itself, from their two known files.
/// </remarks>
internal static class SourceReport
{
    /// <summary>The part of a path that leads into the source cache, <c>artifacts/native/sources/</c>.</summary>
    private const string SourcesMarker = "/native/sources/";

    /// <summary>The number of directory levels kept under a dependency's root, enough to tell its third-party components apart.</summary>
    private const int Depth = 3;

    /// <summary>Writes the source directories of a target's package builds.</summary>
    /// <param name="buildDirectory">xmake's build directory of the target, whose <c>.packages/</c> holds the package builds.</param>
    /// <param name="output">Receives one line per directory, such as <c>dawn/third_party/abseil-cpp/absl</c>.</param>
    /// <param name="cancellationToken">Stops Ninja.</param>
    /// <returns>A task that completes when the report is written.</returns>
    public static async Task WriteAsync(string buildDirectory, TextWriter output, CancellationToken cancellationToken)
    {
        var packages = Path.Combine(buildDirectory, ".packages");
        var directories = new SortedSet<string>(StringComparer.Ordinal);

        // xmake keeps the build directory of a package's earlier build keys beside the current one.
        var builds = Directory.Exists(packages)
            ? Directory.EnumerateFiles(packages, "build.ninja", SearchOption.AllDirectories)
                .GroupBy(static file => Path.GetDirectoryName(Path.GetDirectoryName(file)), StringComparer.Ordinal)
                .Select(static group => group.MaxBy(File.GetLastWriteTimeUtc)!)
            : [];

        foreach (var build in builds)
        {
            var deps = await Command.ReadAsync("ninja", ["-C", Path.GetDirectoryName(build)!, "-t", "deps"], environment: null, cancellationToken).ConfigureAwait(false);

            foreach (var line in deps.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var path = line.Replace('\\', '/');
                var start = path.IndexOf(SourcesMarker, StringComparison.Ordinal);

                if (start < 0)
                {
                    continue;
                }

                // <dependency>/<commit>/<directories...>/<file>
                var parts = path[(start + SourcesMarker.Length)..].Split('/');

                if (parts.Length >= 3)
                {
                    _ = directories.Add(string.Join('/', [parts[0], .. parts[2..^1].Take(Depth)]));
                }
            }
        }

        await output.WriteLineAsync("Source directories compiled by the CMake builds:".AsMemory(), cancellationToken).ConfigureAwait(false);

        foreach (var directory in directories)
        {
            await output.WriteLineAsync($"  {directory}".AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}
