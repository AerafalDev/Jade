/// <summary>The size, alignment and field offsets of a struct on one target, in bytes.</summary>
internal sealed class RecordLayout
{
    /// <summary>Creates a layout.</summary>
    /// <param name="size">The size.</param>
    /// <param name="alignment">The alignment.</param>
    /// <param name="offsets">The offset of each field, in the order of <see cref="StructModel.Fields"/>.</param>
    public RecordLayout(long size, long alignment, IReadOnlyList<long> offsets)
    {
        Size = size;
        Alignment = alignment;
        Offsets = offsets;
    }

    /// <summary>Gets the size.</summary>
    public long Size { get; }

    /// <summary>Gets the alignment.</summary>
    public long Alignment { get; }

    /// <summary>Gets the offset of each field, in the order of <see cref="StructModel.Fields"/>.</summary>
    public IReadOnlyList<long> Offsets { get; }

    /// <summary>Formats the layout for reports, for example <c>size 16, align 4, offsets 0 4 8 12</c>.</summary>
    /// <returns>The description.</returns>
    public string Describe() => $"size {Size}, align {Alignment}, offsets {string.Join(' ', Offsets)}";
}
