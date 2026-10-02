/// <summary>Upstream documentation of one declaration, independent of its source format.</summary>
internal sealed class Documentation
{
    /// <summary>Gets an empty documentation, for declarations upstream does not document.</summary>
    public static Documentation None { get; } = new();

    /// <summary>Gets the one-paragraph summary, or <see langword="null"/>.</summary>
    public string? Summary { get; init; }

    /// <summary>Gets the remaining blocks: details, notes, thread safety, availability.</summary>
    public IReadOnlyList<DocBlock> Remarks { get; init; } = [];

    /// <summary>Gets the parameter descriptions by native parameter name.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the description of the return value, or <see langword="null"/>.</summary>
    public string? Returns { get; init; }
}
