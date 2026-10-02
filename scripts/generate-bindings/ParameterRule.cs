/// <summary>A config annotation that proves a friendly overload correct for one parameter (ADR-0006).</summary>
internal sealed class ParameterRule
{
    private ParameterRule(ParameterKind kind, string? count = null)
    {
        Kind = kind;
        Count = count;
    }

    /// <summary>Gets a rule that keeps only the raw pointer, for example to stop the automatic UTF-8 overload.</summary>
    public static ParameterRule Raw { get; } = new(ParameterKind.None);

    /// <summary>Gets the rule for a <c>const T*</c> to a single value: <c>in T</c>.</summary>
    public static ParameterRule In { get; } = new(ParameterKind.In);

    /// <summary>Gets the rule for a <c>T*</c> the callee only writes: <c>out T</c>.</summary>
    public static ParameterRule Out { get; } = new(ParameterKind.Out);

    /// <summary>Gets the rule for a <c>T*</c> the callee reads and writes: <c>ref T</c>.</summary>
    public static ParameterRule Ref { get; } = new(ParameterKind.Ref);

    /// <summary>Gets how the parameter is exposed.</summary>
    public ParameterKind Kind { get; }

    /// <summary>Gets the C name of the count parameter, for <see cref="ParameterKind.Span"/>.</summary>
    public string? Count { get; }

    /// <summary>Creates the rule for a pointer to <paramref name="count"/> elements: <c>Span&lt;T&gt;</c>, or <c>ReadOnlySpan&lt;T&gt;</c> when <c>const</c>.</summary>
    /// <param name="count">The C name of the parameter holding the element count.</param>
    /// <returns>The rule.</returns>
    public static ParameterRule Span(string count) => new(ParameterKind.Span, count);
}
