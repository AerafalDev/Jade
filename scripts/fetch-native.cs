#!/usr/bin/env dotnet
// Downloads jade_native as CI built it into artifacts/native/<rid>/, the layout
// scripts/build-native.cs stages, so that managed-only work needs no native toolchain (ADR-0004).
// It takes the artifacts of the latest successful run of .github/workflows/native.yml on a branch
// (main by default), or of a given run, for the host RID unless told otherwise. Needs the GitHub
// CLI (gh), signed in.
//
// Usage: dotnet scripts/fetch-native.cs [--rid <rid>]... [--branch <branch>] [--run <run-id>]

#:include fetch-native/Gh.cs

using System.Runtime.InteropServices;

const string Usage = "Usage: dotnet scripts/fetch-native.cs [--rid <rid>]... [--branch <branch>] [--run <run-id>]";

var rids = new List<string>();
var branch = "main";
string? runId = null;
for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--rid" when i + 1 < args.Length:
            rids.Add(args[++i]);
            break;

        case "--branch" when i + 1 < args.Length:
            branch = args[++i];
            break;

        case "--run" when i + 1 < args.Length:
            runId = args[++i];
            break;

        case "-h" or "--help":
            Console.WriteLine(Usage);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown or incomplete argument: {args[i]}");
            Console.Error.WriteLine(Usage);
            return 2;
    }
}

if (rids.Count == 0)
{
    var os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
    rids.Add($"{os}-{RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}");
}

var repositoryRoot = Path.GetFullPath(Path.Combine((string)AppContext.GetData("EntryPointFileDirectoryPath")!, ".."));
try
{
    Gh.EnsureAuthenticated(repositoryRoot);

    // Only a run whose every job passed: its artifacts come from one commit and one cache state.
    runId ??= Gh.Run(repositoryRoot, ["run", "list", "--workflow", "native.yml", "--branch", branch, "--status", "success", "--limit", "1", "--json", "databaseId", "--jq", ".[0].databaseId"]);
    if (runId.Length == 0)
    {
        throw new InvalidOperationException($"no successful native.yml run on {branch}.");
    }

    var commit = Gh.Run(repositoryRoot, ["run", "view", runId, "--json", "headSha", "--jq", ".headSha"]);
    Console.WriteLine($"native.yml run {runId}, commit {commit}");
    foreach (var rid in rids)
    {
        var stageDirectory = Path.Combine(repositoryRoot, "artifacts", "native", rid);
        if (Directory.Exists(stageDirectory))
        {
            Directory.Delete(stageDirectory, recursive: true);
        }

        // A single named artifact is extracted straight into --dir.
        Gh.Run(repositoryRoot, ["run", "download", runId, "--name", $"jade-native-{rid}", "--dir", stageDirectory]);
        Console.WriteLine($"Fetched {Path.GetRelativePath(repositoryRoot, stageDirectory)}");
    }

    return 0;
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}
