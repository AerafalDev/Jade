namespace Jade.BindingGenerator.Model;

/// <summary>A structure, with the chain and ownership semantics the generator needs beyond its C layout.</summary>
internal sealed record StructureDeclaration : Declaration
{
    /// <summary>Gets the members, in C layout order, the chain and userdata members included.</summary>
    public required IReadOnlyList<StructureMember> Members { get; init; }

    /// <summary>
    /// Gets the C name of the macro that initializes the structure with its defaults, such as
    /// <c>WGPU_EXTENT_3D_INIT</c>, or <see langword="null"/> when the API defines none.
    /// </summary>
    public string? InitializerCName { get; init; }

    /// <summary>Gets whether the library fills the structure in, rather than the caller.</summary>
    public bool IsOutput { get; init; }

    /// <summary>Gets whether the structure carries a callback, its mode and its userdata.</summary>
    public bool IsCallbackInfo { get; init; }

    /// <summary>Gets the direction of the chain the structure is the root of, through its <c>nextInChain</c> member.</summary>
    public ChainDirection Extensible { get; init; }

    /// <summary>Gets the direction of the chains the structure extends, through its chain header.</summary>
    public ChainDirection Chained { get; init; }

    /// <summary>Gets the C names of the structures this extension can be chained to.</summary>
    public IReadOnlyList<string> ChainRoots { get; init; } = [];
}
