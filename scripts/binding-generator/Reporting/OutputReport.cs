using System.Globalization;
using Jade.BindingGenerator.Emission;
using Jade.BindingGenerator.Model;
using Jade.BindingGenerator.Projection;

namespace Jade.BindingGenerator.Reporting;

/// <summary>Writes what the generator emitted for a library and what it left out, so that omissions are reviewed (ADR 0027).</summary>
internal static class OutputReport
{
    /// <summary>Writes the report of one library's raw layer.</summary>
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
            string.Create(CultureInfo.InvariantCulture, $"  raw layer: {update.Files} files in {directory} ({update.Written} written, {update.Deleted} deleted)"),
            string.Create(
                CultureInfo.InvariantCulture,
                $"    enums: {library.Types.OfType<ProjectedEnum>().Count()}, handles: {library.Types.OfType<ProjectedHandle>().Count()}, booleans: {library.Types.OfType<ProjectedBoolean>().Count()}, public structures: {structures.Count(static structure => structure.IsPublic)}, internal structures: {structures.Count(static structure => !structure.IsPublic)}, inline arrays: {library.Types.OfType<ProjectedInlineArray>().Count()}, constants: {library.Constants.Count}, functions: {library.Functions.Count}"),
            string.Create(CultureInfo.InvariantCulture, $"    skipped: {model.Skipped.Count}"),
        };

        lines.AddRange(model.Skipped.Select(static skipped => $"      {skipped.Name}: {skipped.Reason}"));

        foreach (var line in lines)
        {
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
    }
}
