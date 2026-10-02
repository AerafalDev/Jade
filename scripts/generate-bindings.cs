#!/usr/bin/env dotnet
// Generates the C# bindings of jade_native (ADR-0005, ADR-0006). For every library in
// generate-bindings/Libraries.cs, it parses the headers staged by build-native.cs once per target triple
// of ADR-0007 through libclang, checks that every target yields the same declarations and that each
// generated struct has clang's layout on every target, then writes src/Jade.Interop/Generated/<Lib>/ and
// the matching layout tests under tests/Jade.Interop.Tests/Generated/<Lib>/.
//
// Usage: dotnet scripts/generate-bindings.cs [--rid <rid>]
//   --rid  the staged artifacts/native/<rid>/ to read headers from (defaults to the host RID). Headers are
//          the same for every RID; this only picks which staging folder exists locally.

#:package ClangSharp
// ClangSharp's libclang and libClangSharp packages pick their native runtime through runtime.json, which
// restore only applies with a RuntimeIdentifier. Without it, a libclang installed on the system loads instead.
#:property RuntimeIdentifier=$(NETCoreSdkRuntimeIdentifier)

#:include generate-bindings/ClangReader.cs
#:include generate-bindings/CodeWriter.cs
#:include generate-bindings/CSharpEmitter.cs
#:include generate-bindings/DocBlock.cs
#:include generate-bindings/DocBlockKind.cs
#:include generate-bindings/DocCommentParser.cs
#:include generate-bindings/Documentation.cs
#:include generate-bindings/DocWriter.cs
#:include generate-bindings/EnumMemberModel.cs
#:include generate-bindings/EnumModel.cs
#:include generate-bindings/FieldModel.cs
#:include generate-bindings/FunctionModel.cs
#:include generate-bindings/GeneratedFiles.cs
#:include generate-bindings/HandleModel.cs
#:include generate-bindings/HeaderConfig.cs
#:include generate-bindings/ImportStyle.cs
#:include generate-bindings/LayoutCalculator.cs
#:include generate-bindings/LayoutDecision.cs
#:include generate-bindings/LayoutDecisionKind.cs
#:include generate-bindings/LayoutTestEmitter.cs
#:include generate-bindings/Libraries.cs
#:include generate-bindings/LibraryConfig.cs
#:include generate-bindings/LibraryModel.cs
#:include generate-bindings/MappingException.cs
#:include generate-bindings/Naming.cs
#:include generate-bindings/ParameterKind.cs
#:include generate-bindings/ParameterModel.cs
#:include generate-bindings/ParameterRule.cs
#:include generate-bindings/PrimitiveType.cs
#:include generate-bindings/RecordLayout.cs
#:include generate-bindings/ReferenceKind.cs
#:include generate-bindings/Sdl3Config.cs
#:include generate-bindings/StructModel.cs
#:include generate-bindings/Target.cs
#:include generate-bindings/TargetModel.cs
#:include generate-bindings/Targets.cs
#:include generate-bindings/TypeKind.cs
#:include generate-bindings/TypeMapper.cs
#:include generate-bindings/TypeRef.cs
#:include generate-bindings/TypeReference.cs
#:include generate-bindings/VarianceCheck.cs

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ClangSharp.Interop;

// LibraryImport is the current .NET interop model; with blittable signatures its generated stub is a plain
// DllImport, so both run the same. Build cost was measured on the 201 slice (design/tasks/201-binding-generator-core.md).
const ImportStyle Import = ImportStyle.LibraryImport;
const string Usage = "Usage: dotnet scripts/generate-bindings.cs [--rid <rid>]";

var hostRid = $"{(OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux")}-{RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}";
string? rid = hostRid;
if (args is ["--rid", var value])
{
    rid = value;
}
else if (args is ["-h" or "--help"])
{
    Console.WriteLine(Usage);
    return 0;
}
else if (args.Length > 0)
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var stopwatch = Stopwatch.StartNew();
var scriptDirectory = (string)AppContext.GetData("EntryPointFileDirectoryPath")!;
var repositoryRoot = Path.GetFullPath(Path.Combine(scriptDirectory, ".."));
var stageDirectory = Path.Combine(repositoryRoot, "artifacts", "native", rid);
var includeDirectory = Path.Combine(stageDirectory, "include");
var versionsPath = Path.Combine(stageDirectory, "metadata", "versions.json");
var sysroot = Path.Combine(scriptDirectory, "generate-bindings", "sysroot");
if (!File.Exists(versionsPath))
{
    Console.Error.WriteLine($"error: {Path.GetRelativePath(repositoryRoot, versionsPath)} not found. Run `dotnet scripts/build-native.cs --rid {rid}` first.");
    return 1;
}

// A libclang other than the one ClangSharp was built for may parse differently; refuse it rather than drift.
var bindingVersion = typeof(CXIndex).Assembly.GetName().Version!;
var expectedClang = $"clang version {bindingVersion.Major}.{bindingVersion.Minor}.{bindingVersion.Build}";
var loadedClang = clang.getClangVersion().ToString();
if (!loadedClang.StartsWith(expectedClang + " ", StringComparison.Ordinal) && loadedClang != expectedClang)
{
    Console.Error.WriteLine($"error: loaded `{loadedClang}`, expected `{expectedClang}` from the libclang NuGet package.");
    return 1;
}

try
{
    using var versions = JsonDocument.Parse(File.ReadAllText(versionsPath));
    using var index = CXIndex.Create();
    foreach (var config in Libraries.All)
    {
        var upstream = versions.RootElement.GetProperty("upstreams").EnumerateArray().Single(u => u.GetProperty("name").GetString() == config.Upstream);
        var defines = upstream.GetProperty("defines").EnumerateArray().Select(d => d.GetString()!).ToList();

        var targets = config.Targets.Select(t => ClangReader.Read(index, config, t, includeDirectory, sysroot, defines)).ToList();
        Console.WriteLine($"{config.Name}: parsed {upstream.GetProperty("version").GetString()} for {targets.Count} targets ({string.Join(", ", targets.Select(t => t.Target.Rid))}).");

        var report = new List<string>();
        var variances = VarianceCheck.Run(targets, model => new CSharpEmitter(model, Import).Emit(), report);
        Console.WriteLine(report.Count == 0
            ? $"{config.Name}: layout report: every struct has the same layout on every target."
            : $"{config.Name}: layout report: these structs differ between targets, and the generated definitions match each target:");
        report.ForEach(Console.WriteLine);
        if (variances.Count > 0)
        {
            Console.Error.WriteLine($"error: {config.Name}: unhandled layout or declaration variance between targets:\n  {string.Join("\n  ", variances)}");
            return 1;
        }

        var reference = targets[0].Model;
        var files = new CSharpEmitter(reference, Import).Emit();
        var outputDirectory = Path.Combine(repositoryRoot, "src", "Jade.Interop", "Generated", config.Name);
        var changes = GeneratedFiles.Write(outputDirectory, files);
        var testDirectory = Path.Combine(repositoryRoot, "tests", "Jade.Interop.Tests", "Generated", config.Name);
        changes += GeneratedFiles.Write(testDirectory, LayoutTestEmitter.Emit(targets));

        Console.WriteLine($"{config.Name}: {reference.Functions.Count} functions, {reference.Enums.Count} enums, {reference.Structs.Count} structs, " +
            $"{reference.Handles.Count} handles in {files.Count} files under {Path.GetRelativePath(repositoryRoot, outputDirectory)}; {changes} file(s) changed.");
    }
}
catch (InvalidOperationException e)
{
    Console.Error.WriteLine($"error: {e.Message}");
    return 1;
}

Console.WriteLine($"Done in {stopwatch.Elapsed.TotalSeconds:0.0} s.");
return 0;
