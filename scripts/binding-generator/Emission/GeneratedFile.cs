namespace Jade.BindingGenerator.Emission;

/// <summary>A generated source file.</summary>
/// <param name="Name">The path relative to the <c>Generated/</c> directory, with <c>/</c> separators, such as <c>TextureFormat.g.cs</c> or <c>Raw/BufferDescriptor.g.cs</c>.</param>
/// <param name="Content">The source text, with LF line endings.</param>
internal sealed record GeneratedFile(string Name, string Content);
