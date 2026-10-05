namespace Jade.BindingGenerator.Model;

/// <summary>What a structure member is for, beyond its type.</summary>
/// <remarks>
/// The chain and userdata members are part of the C layout but carry no value of the API; the
/// projection treats them apart, for instance when it classifies a structure (ADR 0029).
/// </remarks>
internal enum MemberRole
{
    /// <summary>A member that carries a value of the API.</summary>
    Value,

    /// <summary>The <c>nextInChain</c> pointer of a chain root.</summary>
    NextInChain,

    /// <summary>The chain header that starts an extension, which links it and gives its structure type.</summary>
    ChainHeader,

    /// <summary>A pointer that the library passes back to a callback untouched.</summary>
    Userdata,
}
