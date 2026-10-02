/// <summary>What the libclang reader saw for one target triple: the model plus the native facts it is checked against.</summary>
internal sealed class TargetModel
{
    /// <summary>Gets the target.</summary>
    public required Target Target { get; init; }

    /// <summary>Gets the model built from this target's parse.</summary>
    public required LibraryModel Model { get; init; }

    /// <summary>Gets the native layout of every non-opaque struct, by C name.</summary>
    public required IReadOnlyDictionary<string, RecordLayout> Layouts { get; init; }

    /// <summary>Gets <c>sizeof(void*)</c>.</summary>
    public required int PointerSize { get; init; }

    /// <summary>Gets <c>sizeof(long)</c>.</summary>
    public required int LongSize { get; init; }
}
