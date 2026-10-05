namespace Jade.BindingGenerator.Emission;

/// <summary>What an update of a <c>Generated/</c> directory changed.</summary>
/// <param name="Files">The number of generated files.</param>
/// <param name="Written">The number of files written because they were new or differed.</param>
/// <param name="Deleted">The number of generated files deleted because the generator no longer produces them.</param>
internal sealed record GeneratedDirectoryUpdate(int Files, int Written, int Deleted);
