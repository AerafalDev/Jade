/// <summary>
/// A block of documentation. Inline text uses a small Markdown subset that every reader produces and the emitter
/// renders: <c>`code`</c> and <c>**bold**</c>.
/// </summary>
internal sealed class DocBlock
{
    /// <summary>Creates a block.</summary>
    /// <param name="kind">The kind of block.</param>
    /// <param name="lines">The paragraph text as one entry, the list items, or the code lines.</param>
    public DocBlock(DocBlockKind kind, IReadOnlyList<string> lines)
    {
        Kind = kind;
        Lines = lines;
    }

    /// <summary>Gets the kind of block.</summary>
    public DocBlockKind Kind { get; }

    /// <summary>Gets the paragraph text as one entry, the list items, or the code lines.</summary>
    public IReadOnlyList<string> Lines { get; }
}
