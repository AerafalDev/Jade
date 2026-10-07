using System.Globalization;
using Jade.BindingGenerator.Emission;
using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Projection;

namespace Jade.BindingGenerator.Reporting;

/// <summary>Writes what the generator emitted for a library and what it left out, so that omissions are reviewed (ADR 0027).</summary>
internal static class OutputReport
{
    /// <summary>Writes the report of one library's generated files and raw layer.</summary>
    /// <param name="writer">The writer to report to.</param>
    /// <param name="directory">The directory of the generated files, relative to the repository root.</param>
    /// <param name="model">The model the raw layer was projected from.</param>
    /// <param name="library">The projected raw layer.</param>
    /// <param name="update">What the update of the directory changed.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the report is written.</returns>
    public static async Task WriteAsync(TextWriter writer, string directory, ApiModel model, ProjectedLibrary library, GeneratedDirectoryUpdate update, CancellationToken cancellationToken)
    {
        var structures = library.Types.OfType<ProjectedStructure>().ToList();
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"  generated files: {update.Files} in {directory} ({update.Written} written, {update.Deleted} deleted)"),
            string.Create(
                CultureInfo.InvariantCulture,
                $"  raw layer: enums: {library.Types.OfType<ProjectedEnum>().Count()}, handles: {library.Types.OfType<ProjectedHandle>().Count()}, booleans: {library.Types.OfType<ProjectedBoolean>().Count()}, public structures: {structures.Count(static structure => structure.IsPublic)}, internal structures: {structures.Count(static structure => !structure.IsPublic)}, inline arrays: {library.Types.OfType<ProjectedInlineArray>().Count()}, constants: {library.Constants.Count}, functions: {library.Functions.Count}"),
            string.Create(CultureInfo.InvariantCulture, $"    skipped: {model.Skipped.Count}"),
        };

        lines.AddRange(model.Skipped.Select(static skipped => $"      {skipped.Name}: {skipped.Reason}"));

        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Writes the report of one library's idiomatic layer, with what it leaves out (ADR 0040).</summary>
    /// <param name="writer">The writer to report to.</param>
    /// <param name="library">The projected idiomatic layer.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the report is written.</returns>
    public static async Task WriteIdiomaticAsync(TextWriter writer, IdiomaticLibrary library, CancellationToken cancellationToken)
    {
        var lines = new List<string>
        {
            string.Create(
                CultureInfo.InvariantCulture,
                $"  idiomatic layer: {library.Handles.Sum(static handle => handle.Methods.Count)} members on {library.Handles.Count} handles, mirrors: {library.Structures.Count(static structure => structure.Role == IdiomaticRole.Mirror)}, element mirrors: {library.Structures.Count(static structure => structure.Role == IdiomaticRole.ElementMirror)}, snapshots: {library.Structures.Count(static structure => structure.Role == IdiomaticRole.Snapshot)}, extended value structures: {library.ValueStructures.Count}, extension slots: {library.Slots.Count}"),
            string.Create(CultureInfo.InvariantCulture, $"    internal constants: {string.Join(", ", library.InternalConstants)}"),
            string.Create(CultureInfo.InvariantCulture, $"    left out or written by hand: {library.Omitted.Count}"),
        };

        lines.AddRange(library.Omitted.Select(static omitted => $"      {omitted.Name}: {omitted.Reason}"));

        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Writes the report of one library's layout tests (ADR 0036).</summary>
    /// <param name="writer">The writer to report to.</param>
    /// <param name="layouts">The projected layout tests.</param>
    /// <param name="testDirectory">The directory of the generated C# tests, relative to the repository root.</param>
    /// <param name="testUpdate">What the update of that directory changed.</param>
    /// <param name="nativeSource">The generated C source, relative to the repository root.</param>
    /// <param name="nativeWritten">Whether the C source was written because it was new or differed.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the report is written.</returns>
    public static async Task WriteLayoutsAsync(TextWriter writer, ProjectedLayouts layouts, string testDirectory, GeneratedDirectoryUpdate testUpdate, string nativeSource, bool nativeWritten, CancellationToken cancellationToken)
    {
        var members = layouts.Records.Sum(static record => record.Members.Count);
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"  layout tests: {layouts.Records.Count} structures, {members} members; {testDirectory} ({testUpdate.Written} written, {testUpdate.Deleted} deleted), {nativeSource} ({(nativeWritten ? "written" : "unchanged")})");

        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
