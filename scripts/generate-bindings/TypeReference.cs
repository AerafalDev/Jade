/// <summary>A declaration that a bound function or struct uses, so it has to be bound too.</summary>
internal sealed class TypeReference
{
    /// <summary>Creates a reference.</summary>
    /// <param name="name">The C name.</param>
    /// <param name="kind">What it turns into.</param>
    public TypeReference(string name, ReferenceKind kind)
    {
        Name = name;
        Kind = kind;
    }

    /// <summary>Gets the C name.</summary>
    public string Name { get; }

    /// <summary>Gets what it turns into.</summary>
    public ReferenceKind Kind { get; }
}
