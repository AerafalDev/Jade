/// <summary>The kind of a <see cref="DocBlock"/>.</summary>
internal enum DocBlockKind
{
    /// <summary>A paragraph of inline text.</summary>
    Paragraph,

    /// <summary>A bulleted list; each item is inline text.</summary>
    List,

    /// <summary>A preformatted code block.</summary>
    Code,
}
