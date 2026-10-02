/// <summary>A config decision for a type whose layout differs between targets, with the reason recorded next to it.</summary>
internal sealed class LayoutDecision
{
    /// <summary>Creates a decision.</summary>
    /// <param name="kind">How the type is exposed.</param>
    /// <param name="reason">Why, for reviewers of the config.</param>
    public LayoutDecision(LayoutDecisionKind kind, string reason)
    {
        Kind = kind;
        Reason = reason;
    }

    /// <summary>Gets how the type is exposed.</summary>
    public LayoutDecisionKind Kind { get; }

    /// <summary>Gets why.</summary>
    public string Reason { get; }
}
