using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Jade.NativeBuild.Build;
using Jade.NativeBuild.Configuration;

namespace Jade.NativeBuild.Tools;

/// <summary>
/// The Emscripten toolchain of the SDK's <c>wasm-tools</c> workload, run outside MSBuild with the
/// environment the SDK's <c>BrowserWasmApp.targets</c> gives it (ADR 0025).
/// </summary>
/// <param name="ToolsDirectory">The pack's <c>tools/</c>: <c>emscripten/</c>, and LLVM and Binaryen in <c>bin/</c>.</param>
/// <param name="NodeExecutable">The <c>node</c> of the workload's Node pack.</param>
/// <param name="PackCacheDirectory">The read-only Emscripten cache of the workload's Cache pack.</param>
internal sealed partial record EmscriptenWorkload(string ToolsDirectory, string NodeExecutable, string PackCacheDirectory)
{
    /// <summary>Gets the directory of <c>emcc</c>, which xmake takes as its Emscripten SDK (<c>--emsdk</c>).</summary>
    public string EmscriptenDirectory => Path.Combine(ToolsDirectory, "emscripten");

    /// <summary>Gets the toolchain's <c>llvm-nm</c>, which reads the symbols of the archives.</summary>
    public string SymbolTool => Path.Combine(ToolsDirectory, "bin", OperatingSystem.IsWindows() ? "llvm-nm.exe" : "llvm-nm");

    /// <summary>Finds the workload's toolchain and checks its versions against <c>build/versions.json</c>.</summary>
    /// <param name="pinned">The pinned versions.</param>
    /// <param name="host">The host, which names the packs.</param>
    /// <param name="cacheDirectory">The writable Emscripten cache of the build.</param>
    /// <param name="cancellationToken">Stops <c>emcc</c>.</param>
    /// <returns>The toolchain.</returns>
    /// <exception cref="InvalidDataException">The workload is missing, or its packs or <c>emcc</c> report other versions.</exception>
    /// <remarks>
    /// The packs belong to the .NET installation that runs the build, which is the one of the SDK
    /// pinned in <c>global.json</c>.
    /// </remarks>
    public static async Task<EmscriptenWorkload> LocateAsync(PinnedEmscripten pinned, HostPlatform host, string cacheDirectory, CancellationToken cancellationToken)
    {
        var packs = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..", "packs"));
        var rid = NativeTarget.ForHost(host).RuntimeIdentifier;
        var prefix = "Microsoft.NET.Runtime.Emscripten.";
        var sdkPack = Path.Combine(packs, $"{prefix}{pinned.PackVersion}.Sdk.{rid}");

        if (!Directory.Exists(sdkPack))
        {
            var installed = Directory.Exists(packs)
                ? Directory.EnumerateDirectories(packs, $"{prefix}*.Sdk.{rid}").Select(static directory => Path.GetFileName(directory)).Order(StringComparer.Ordinal).ToList()
                : [];

            throw new InvalidDataException(installed.Count == 0
                ? $"The wasm-tools workload is not installed in '{Path.GetDirectoryName(packs)}': run 'dotnet workload install wasm-tools'."
                : $"build/versions.json pins the Emscripten packs {pinned.PackVersion}, but the workload has {string.Join(", ", installed)}.");
        }

        var tools = Path.Combine(GetSingleVersion(sdkPack), "tools");
        var node = Path.Combine(GetSingleVersion(Path.Combine(packs, $"{prefix}{pinned.PackVersion}.Node.{rid}")), "tools", "bin", OperatingSystem.IsWindows() ? "node.exe" : "node");
        var cache = Path.Combine(GetSingleVersion(Path.Combine(packs, $"{prefix}{pinned.PackVersion}.Cache.{rid}")), "tools", "emscripten", "cache");
        var workload = new EmscriptenWorkload(tools, node, cache);
        var emcc = Path.Combine(workload.EmscriptenDirectory, OperatingSystem.IsWindows() ? "emcc.bat" : "emcc");
        var output = await Command.ReadAsync(emcc, ["--version"], workload.CreateEnvironment(cacheDirectory), cancellationToken).ConfigureAwait(false);
        var match = EmccVersion().Match(output);

        return !match.Success
            ? throw new InvalidDataException($"'{emcc} --version' reports no version.")
            : match.Groups["version"].Value == pinned.Version
            ? workload
            : throw new InvalidDataException($"build/versions.json pins Emscripten {pinned.Version}, but the workload's emcc reports {match.Groups["version"].Value}.");
    }

    /// <summary>Creates the environment of the toolchain's tools.</summary>
    /// <param name="cacheDirectory">The writable Emscripten cache of the build.</param>
    /// <returns>The variables the pack's <c>.emscripten</c> configuration reads, and the cache.</returns>
    /// <remarks>
    /// The pack's own cache is read-only during <c>dotnet build</c> (ADR 0025); the build uses a
    /// writable copy of it, so that the system headers and libraries are the workload's. Its
    /// <c>sanity.txt</c> names the LLVM directory of the machine that built the pack, and a
    /// writable cache whose sanity file differs is erased (<c>check_sanity</c> in
    /// <c>tools/shared.py</c>): the copy comes from the same pack as <c>emcc</c>, so the check is
    /// skipped instead.
    /// </remarks>
    public Dictionary<string, string?> CreateEnvironment(string cacheDirectory)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["DOTNET_EMSCRIPTEN_LLVM_ROOT"] = Path.Combine(ToolsDirectory, "bin"),
            ["DOTNET_EMSCRIPTEN_BINARYEN_ROOT"] = ToolsDirectory,
            ["DOTNET_EMSCRIPTEN_NODE_JS"] = NodeExecutable,
            ["EM_CACHE"] = cacheDirectory,
            ["EM_FROZEN_CACHE"] = "0",
            ["EM_IGNORE_SANITY"] = "1",
        };
    }

    /// <summary>Copies the pack's cache into the build's writable cache, once.</summary>
    /// <param name="cacheDirectory">The writable cache.</param>
    /// <remarks>A cache from another pack version is replaced: its system libraries would not match.</remarks>
    public void PrepareCache(string cacheDirectory)
    {
        var stamp = Path.Combine(cacheDirectory, ".jade-pack");

        if (File.Exists(stamp) && File.ReadAllText(stamp) == PackCacheDirectory)
        {
            return;
        }

        if (Directory.Exists(cacheDirectory))
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }

        foreach (var file in Directory.EnumerateFiles(PackCacheDirectory, "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(cacheDirectory, Path.GetRelativePath(PackCacheDirectory, file));

            _ = Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy);

            // Emscripten adds to its cache and erases it on a version change.
            File.SetAttributes(copy, FileAttributes.Normal);
        }

        File.WriteAllText(stamp, PackCacheDirectory);
    }

    /// <summary>Gets the only version directory of a workload pack.</summary>
    /// <param name="pack">The pack directory.</param>
    /// <returns>The version directory.</returns>
    /// <exception cref="InvalidDataException">The pack is missing or has several versions installed.</exception>
    private static string GetSingleVersion(string pack)
    {
        var versions = Directory.Exists(pack) ? Directory.GetDirectories(pack) : [];

        return versions.Length == 1
            ? versions[0]
            : throw new InvalidDataException($"'{pack}' must hold exactly one version of the pack, but holds {versions.Length}.");
    }

    /// <summary>Matches the version in the first line of <c>emcc --version</c>, such as <c>… GNU ld) 6.0.3 (6ea9c28…)</c>.</summary>
    /// <returns>The pattern, with the version in the <c>version</c> group.</returns>
    [GeneratedRegex(@"^emcc \(.*\) (?<version>\d+\.\d+\.\d+)")]
    private static partial Regex EmccVersion();
}
