#!/usr/bin/env dotnet
// Tests the packages the way a user consumes them: packs Jade and Jade.Native into artifacts/packages,
// checks that Jade depends on Jade.Native at exactly its own version, restores tests/Jade.PackageTests
// from there, runs it with `dotnet run`, then publishes it with NativeAOT for the host RID and runs
// the binary. Natives come from artifacts/native/<rid>/, so the host RID must be staged there
// (scripts/build-native.cs or scripts/fetch-native.cs).
//
// Every local pack of one commit carries the same version, and NuGet never extracts a version it
// already has. So the Jade packages a previous run extracted into artifacts/package-tests/packages/
// (the project's nuget.config) are deleted first, along with the project's outputs.
//
// --no-pack tests the packages already in artifacts/packages instead of packing them again.
//
// Usage: dotnet scripts/test-package.cs [--no-pack]

using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml.Linq;

const string Usage = "Usage: dotnet scripts/test-package.cs [--no-pack]";

var pack = true;
foreach (var argument in args)
{
    switch (argument)
    {
        case "--no-pack":
            pack = false;
            break;

        case "-h" or "--help":
            Console.WriteLine(Usage);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown argument: {argument}");
            Console.Error.WriteLine(Usage);
            return 2;
    }
}

var repositoryRoot = Path.GetFullPath(Path.Combine((string)AppContext.GetData("EntryPointFileDirectoryPath")!, ".."));
var project = Path.Combine(repositoryRoot, "tests", "Jade.PackageTests");
var testDirectory = Path.Combine(repositoryRoot, "artifacts", "package-tests");
var os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
var rid = $"{os}-{RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}";
try
{
    if (pack)
    {
        Run("dotnet", ["pack", "-c", "Release", "-o", Path.Combine(repositoryRoot, "artifacts", "packages")]);
    }

    // MinVer sets the version in a target: without -t:MinVer, MSBuild reports the SDK's default 1.0.0.
    var version = Run("dotnet", ["msbuild", Path.Combine(repositoryRoot, "src", "Jade", "Jade.csproj"), "-t:MinVer", "-getProperty:PackageVersion", "-p:Configuration=Release"], captureOutput: true);
    Console.WriteLine($"Testing Jade {version} on {rid}");

    // Jade.csproj pins the range through NuGet's internal pack items, which an SDK update could rename without notice.
    var package = Path.Combine(repositoryRoot, "artifacts", "packages", $"Jade.{version}.nupkg");
    if (!File.Exists(package))
    {
        throw new InvalidOperationException($"{Path.GetRelativePath(repositoryRoot, package)} not found. Run without --no-pack.");
    }

    using (var archive = ZipFile.OpenRead(package))
    using (var nuspec = archive.GetEntry("Jade.nuspec")!.Open())
    {
        var range = XDocument.Load(nuspec).Descendants().SingleOrDefault(e => e.Name.LocalName == "dependency" && (string?)e.Attribute("id") == "Jade.Native")?.Attribute("version")?.Value;
        if (range != $"[{version}]")
        {
            throw new InvalidOperationException($"Jade.nuspec depends on Jade.Native {range ?? "(missing)"}, not [{version}].");
        }

        Console.WriteLine($"Jade.nuspec depends on Jade.Native {range}");
    }

    // The packages folder has NuGet's layout: <lowercase id>/<lowercase version>/.
    DeleteDirectory(Path.Combine(testDirectory, "packages", "jade", version.ToLowerInvariant()));
    DeleteDirectory(Path.Combine(testDirectory, "packages", "jade.native", version.ToLowerInvariant()));
    DeleteDirectory(Path.Combine(project, "bin"));
    DeleteDirectory(Path.Combine(project, "obj"));
    var publishDirectory = Path.Combine(testDirectory, "publish", rid);
    DeleteDirectory(publishDirectory);

    var versionProperty = $"--property:JadePackageVersion={version}";
    Run("dotnet", ["run", "--project", project, "-c", "Release", versionProperty]);

    Run("dotnet", ["publish", project, "-c", "Release", "-r", rid, versionProperty, "-o", publishDirectory]);

    // A shared jade_native is not linked into the NativeAOT binary: it has to be published next to it.
    var library = OperatingSystem.IsWindows() ? "jade_native.dll" : OperatingSystem.IsMacOS() ? "libjade_native.dylib" : "libjade_native.so";
    if (!File.Exists(Path.Combine(publishDirectory, library)))
    {
        throw new InvalidOperationException($"{library} is missing from {Path.GetRelativePath(repositoryRoot, publishDirectory)}.");
    }

    Run(Path.Combine(publishDirectory, OperatingSystem.IsWindows() ? "Jade.PackageTests.exe" : "Jade.PackageTests"), []);
    Console.WriteLine($"Jade {version} passed on {rid} with dotnet run and as a NativeAOT binary.");
    return 0;
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}

// Runs a program from the repository root with its output on the console, or returns its standard output, trimmed.
string Run(string program, IReadOnlyList<string> arguments, bool captureOutput = false)
{
    var startInfo = new ProcessStartInfo(program)
    {
        UseShellExecute = false,
        WorkingDirectory = repositoryRoot,
        RedirectStandardOutput = captureOutput,
    };
    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"cannot start {program}.");
    var output = captureOutput ? process.StandardOutput.ReadToEnd() : string.Empty;
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"`{Path.GetFileName(program)} {string.Join(' ', arguments)}` failed with exit code {process.ExitCode}.");
    }

    return output.Trim();
}

static void DeleteDirectory(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, recursive: true);
    }
}
