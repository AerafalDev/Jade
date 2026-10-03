/// <summary>
/// Emits one xunit test per generated struct that compares its size and field offsets on the test host with what
/// clang computed for the host's target. The generator already checks its own model of the CLR layout against
/// clang on every target (<see cref="VarianceCheck"/>); these tests check that model against the real runtime.
/// </summary>
internal static class LayoutTestEmitter
{
    /// <summary>Renders the layout tests of one library.</summary>
    /// <param name="targets">The per-target results; the first one's model is the emitted one.</param>
    /// <returns>The test file by file name.</returns>
    public static IReadOnlyDictionary<string, string> Emit(IReadOnlyList<TargetModel> targets)
    {
        var model = targets[0].Model;
        var writer = new CodeWriter();
        writer.Line(CSharpEmitter.Header);
        writer.Line();
        writer.Line($"using {model.Namespace};");
        writer.Line("using Shouldly;");
        writer.Line("using Xunit;");
        writer.Line();
        writer.Line("namespace Jade.Interop.Tests;");
        writer.Line();
        writer.Line($"/// <summary>Checks the generated {model.Name} structs against the layouts clang computed for the host's target.</summary>");
        writer.Open($"public sealed unsafe class {model.Name}LayoutTests");

        foreach (var structModel in model.Structs.Where(s => !s.IsOpaque))
        {
            writer.Line("[Fact]");
            writer.Open($"public void {structModel.Name}_has_the_layout_of_{structModel.NativeName.Replace('.', '_')}()");
            if (structModel.SupportedPlatforms.Count > 0)
            {
                // A type that only exists on some platforms is only touched behind a guard (CA1416). The generator has
                // already compared its layout with clang's on every target.
                writer.Open($"if (!({string.Join(" || ", structModel.SupportedPlatforms.Select(p => $"OperatingSystem.IsOSPlatform(\"{p}\")"))}))");
                writer.Line($"Assert.Skip(\"{structModel.NativeName} only exists on {string.Join(", ", structModel.SupportedPlatforms)}.\");");
                writer.Line("return;");
                writer.Close();
                writer.Line();
            }

            writer.Line($"{structModel.Name} value = default;");

            // A fixed buffer is already a pointer; anything else, inline arrays included, needs its address taken.
            var offsets = structModel.Fields.Select(f => f.Type.Kind == TypeKind.FixedArray && CSharpEmitter.IsFixedBufferElement(f.Type.Element!)
                ? $"Offset(&value, value.{f.Name})"
                : $"Offset(&value, &value.{f.Name})");
            writer.Line($"long[] actual = [sizeof({structModel.Name}), {string.Join(", ", offsets)}];");
            writer.Line("long[] expected = HostRid.Current switch");
            writer.Line("{");
            writer.Indent();
            var groups = targets
                .Select(t => (t.Target.Rid, Layout: t.Layouts[structModel.NativeName]))
                .GroupBy(t => string.Join(", ", new[] { t.Layout.Size }.Concat(t.Layout.Offsets)), StringComparer.Ordinal);
            foreach (var group in groups)
            {
                writer.Line($"{string.Join(" or ", group.Select(g => $"\"{g.Rid}\""))} => [{group.Key}],");
            }

            writer.Line($"var rid => throw new PlatformNotSupportedException($\"No {structModel.NativeName} layout for {{rid}}.\"),");
            writer.Unindent();
            writer.Line("};");
            writer.Line("actual.ShouldBe(expected);");
            writer.Close();
            writer.Line();
        }

        writer.Line("private static long Offset(void* start, void* field) => (byte*)field - (byte*)start;");
        writer.Close();
        return new Dictionary<string, string> { [$"{model.Name}LayoutTests.g.cs"] = writer.ToString() };
    }
}
