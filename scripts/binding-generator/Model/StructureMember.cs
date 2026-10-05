namespace Jade.BindingGenerator.Model;

/// <summary>A member of a structure, in C layout order.</summary>
/// <remarks>Members have the availability of their structure: a structure is identical on every platform it exists on (ADR 0026).</remarks>
internal sealed record StructureMember
{
    /// <summary>Gets the C name of the member.</summary>
    public required string CName { get; init; }

    /// <summary>Gets the words of the name, which the projection turns into a .NET name.</summary>
    public required IReadOnlyList<string> Words { get; init; }

    /// <summary>Gets the type of the member.</summary>
    public required TypeReference Type { get; init; }

    /// <summary>Gets what the member is for.</summary>
    public MemberRole Role { get; init; }

    /// <summary>Gets the value the member has in the structure's default initializer.</summary>
    public required ValueExpression Default { get; init; }

    /// <summary>Gets the length of the array a pointer member points to, or <see langword="null"/> for a single element.</summary>
    public ArrayLength? Length { get; init; }

    /// <summary>Gets whether the member may be null or left unset.</summary>
    public bool IsOptional { get; init; }
}
