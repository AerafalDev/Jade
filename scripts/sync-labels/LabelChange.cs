/// <summary>What a sync does to one label, from its current state on GitHub to its declaration.</summary>
internal sealed class LabelChange
{
    /// <summary>Creates a change.</summary>
    /// <param name="kind">What happens to the label.</param>
    /// <param name="currentName">The label's name on GitHub, or the declared name when it is created.</param>
    /// <param name="target">The declaration, or <see langword="null"/> for an undeclared label.</param>
    /// <param name="differences">What differs from the declaration: <c>name</c>, <c>color</c>, <c>description</c>.</param>
    public LabelChange(LabelChangeKind kind, string currentName, Label? target, IReadOnlyList<string> differences)
    {
        Kind = kind;
        CurrentName = currentName;
        Target = target;
        Differences = differences;
    }

    /// <summary>Gets what happens to the label.</summary>
    public LabelChangeKind Kind { get; }

    /// <summary>Gets the label's name on GitHub, or the declared name when it is created.</summary>
    public string CurrentName { get; }

    /// <summary>Gets the declaration, or <see langword="null"/> for an undeclared label.</summary>
    public Label? Target { get; }

    /// <summary>Gets what differs from the declaration: <c>name</c>, <c>color</c>, <c>description</c>.</summary>
    public IReadOnlyList<string> Differences { get; }
}
