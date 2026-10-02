#!/usr/bin/env dotnet
// Builds jade_native for one RID with xmake (native/xmake.lua) and stages it into
// artifacts/native/<rid>/:
//   lib/        the library
//   include/    headers the binding generator reads, one folder per library
//   metadata/   versions.json (version, commit and SHA-256 of every upstream) and licenses/
// This is the only entry point for native builds (ADR-0004).
//
// Usage: dotnet scripts/build-native.cs [--rid <rid>] [--config release|debug]

#:include build-native/XmakeTarget.cs
#:include build-native/Rids.cs
#:include build-native/Xmake.cs
#:include build-native/Stage.cs
#:include build-native/Json.cs
#:include build-native/JsonContext.cs
#:include build-native/Manifest.cs
#:include build-native/ManifestHeader.cs
#:include build-native/ManifestPackage.cs
#:include build-native/Upstream.cs
#:include build-native/Versions.cs

using System.Diagnostics;

const string Usage = "Usage: dotnet scripts/build-native.cs [--rid <rid>] [--config release|debug]";

string? rid = null;
var config = "release";
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

if (!target.Verified)
{
    Console.Error.WriteLine($"warning: the xmake mapping for {rid} has not been verified yet, see design/roadmap.md.");
}

var stopwatch = Stopwatch.StartNew();
var repositoryRoot = Path.GetFullPath(Path.Combine((string)AppContext.GetData("EntryPointFileDirectoryPath")!, ".."));
var nativeDirectory = Path.Combine(repositoryRoot, "native");
var buildDirectory = Path.Combine(nativeDirectory, "build", rid);
var stageDirectory = Path.Combine(repositoryRoot, "artifacts", "native", rid);

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
    Xmake.Run(nativeDirectory, ["f", "-p", target.Platform, "-a", target.Architecture, "-m", config, $"--toolchain={target.Toolchain}", "-o", buildDirectory, "--require=y", "-y", .. target.ExtraArguments]);
    Xmake.Run(nativeDirectory, ["build", "-y", "jade_native"]);

    if (!File.Exists(manifestPath))
    {
        throw new InvalidOperationException($"xmake did not write {manifestPath}.");
    }

    var manifest = Json.Read(manifestPath, JsonContext.Default.Manifest);
    var library = Stage.Run(manifest, rid, config, stageDirectory);
    Console.WriteLine($"Staged {Path.GetRelativePath(repositoryRoot, library)} ({new FileInfo(library).Length / 1024} KiB) in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
    return 0;
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}
