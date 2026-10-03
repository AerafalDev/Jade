#!/usr/bin/env dotnet
// Builds jade_native for one RID with xmake (native/xmake.lua) and stages it into
// artifacts/native/<rid>/:
//   lib/        the library
//   include/    headers the binding generator reads, one folder per library
//   metadata/   versions.json (toolchain, and version, commit and SHA-256 of every upstream) and
//               licenses/
// and its separate debug symbols, when the build has some, into artifacts/native-symbols/<rid>/.
// This is the only entry point for native builds (ADR-0004).
//
// --container builds a Linux RID inside the container of native/linux/Dockerfile, on the glibc
// baseline of ADR-0013, as CI does. --prune-packages then uninstalls the package builds that no
// xmake project uses any more (xmake require --clean); CI runs it before caching the packages.
// --print-config prints the RID's xmake configuration and builds nothing: xmake's package build
// hash leaves part of it out, so CI keys each RID's package cache on it.
//
// Usage: dotnet scripts/build-native.cs [--rid <rid>] [--config release|debug] [--container] [--prune-packages] [--print-config]

#:include build-native/XmakeTarget.cs
#:include build-native/Rids.cs
#:include build-native/Command.cs
#:include build-native/Container.cs
#:include build-native/Posix.cs
#:include build-native/Xmake.cs
#:include build-native/Stage.cs
#:include build-native/Json.cs
#:include build-native/JsonContext.cs
#:include build-native/Manifest.cs
#:include build-native/ManifestHeader.cs
#:include build-native/ManifestPackage.cs
#:include build-native/Upstream.cs
#:include build-native/UpstreamResource.cs
#:include build-native/Versions.cs

using System.Diagnostics;

const string Usage = "Usage: dotnet scripts/build-native.cs [--rid <rid>] [--config release|debug] [--container] [--prune-packages] [--print-config]";

string? rid = null;
var config = "release";
var container = false;
var prunePackages = false;
var printConfig = false;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--rid" when i + 1 < args.Length:
            rid = args[++i];
            break;

        case "--config" when i + 1 < args.Length:
            config = args[++i];
            break;

        case "--container":
            container = true;
            break;

        case "--prune-packages":
            prunePackages = true;
            break;

        case "--print-config":
            printConfig = true;
            break;

        case "-h" or "--help":
            Console.WriteLine(Usage);
            Console.WriteLine($"RIDs: {string.Join(", ", Rids.All.Keys)}");
            return 0;

        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            Console.Error.WriteLine(Usage);
            return 2;
    }
}

rid ??= Rids.Host();
if (!Rids.All.TryGetValue(rid, out var target))
{
    Console.Error.WriteLine($"Unknown RID '{rid}'. Known RIDs: {string.Join(", ", Rids.All.Keys)}");
    return 2;
}

if (config is not ("release" or "debug"))
{
    Console.Error.WriteLine($"Unknown config '{config}', expected release or debug.");
    return 2;
}

if (container && target.Platform != "linux")
{
    Console.Error.WriteLine($"--container builds Linux RIDs only, not {rid}.");
    return 2;
}

// The static CRT on Windows (ADR-0003). It reaches the packages too: xmake passes the project's
// runtimes to every package it requires, and to CMake as CMAKE_MSVC_RUNTIME_LIBRARY.
List<string> runtimes = target.Platform == "windows" ? [config == "debug" ? "--runtimes=MTd" : "--runtimes=MT"] : [];
List<string> toolchain = target.Toolchain is null ? [] : [$"--toolchain={target.Toolchain}"];

// Everything the RID is configured with except the build directory, which no package build depends on.
List<string> configuration = ["-p", target.Platform, "-a", target.Architecture, "-m", config, .. toolchain, .. runtimes, .. target.ExtraArguments];
if (printConfig)
{
    Console.WriteLine(string.Join(' ', configuration));
    return 0;
}

if (!target.Verified)
{
    Console.Error.WriteLine($"warning: the xmake mapping for {rid} has not been verified yet, see design/roadmap.md.");
}

var stopwatch = Stopwatch.StartNew();
var repositoryRoot = Path.GetFullPath(Path.Combine((string)AppContext.GetData("EntryPointFileDirectoryPath")!, ".."));
if (container)
{
    try
    {
        Container.Run(repositoryRoot, rid, ["--rid", rid, "--config", config, .. prunePackages ? (string[])["--prune-packages"] : []]);
        Console.WriteLine($"Container build of {rid} done in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
        return 0;
    }
    catch (InvalidOperationException e)
    {
        Console.Error.WriteLine($"error: {e.Message}");
        return 1;
    }
}

var nativeDirectory = Path.Combine(repositoryRoot, "native");
var buildDirectory = Path.Combine(nativeDirectory, "build", rid);
var stageDirectory = Path.Combine(repositoryRoot, "artifacts", "native", rid);
var symbolsDirectory = Path.Combine(repositoryRoot, "artifacts", "native-symbols", rid);

// Rewritten by every build, even an up-to-date one; deleting it first means a stale manifest is never staged.
var manifestPath = Path.Combine(buildDirectory, target.Platform, target.Architecture, config, "jade_native.manifest.json");
if (File.Exists(manifestPath))
{
    File.Delete(manifestPath);
}

try
{
    // --require=y: xmake re-resolves packages only when the project files change, not when a recipe
    // under native/packages/ does. Forcing it lets a recipe edit (new hash, new options) take effect.
    Xmake.Run(nativeDirectory, ["f", .. configuration, "-o", buildDirectory, "--require=y", "-y"]);
    Xmake.Run(nativeDirectory, ["build", "-y", "jade_native"]);

    if (!File.Exists(manifestPath))
    {
        throw new InvalidOperationException($"xmake did not write {manifestPath}.");
    }

    var manifest = Json.Read(manifestPath, JsonContext.Default.Manifest);
    var library = Stage.Run(manifest, rid, config, stageDirectory, symbolsDirectory);
    Console.WriteLine($"Staged {Path.GetRelativePath(repositoryRoot, library)} ({new FileInfo(library).Length / 1024} KiB) in {stopwatch.Elapsed.TotalSeconds:0.0} s.");

    // Package builds are only removed once nothing references them: the configuration above has just
    // recorded which ones this project uses.
    if (prunePackages)
    {
        Xmake.Run(nativeDirectory, ["require", "--clean", "--clean_modes=package", "-y"]);
    }

    return 0;
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}
