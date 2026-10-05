namespace Jade.BindingGenerator.Model;

/// <summary>A structure, with the chain and ownership semantics the generator needs beyond its C layout.</summary>
internal sealed record StructureDeclaration : Declaration
{
    /// <summary>Gets the members, in C layout order, the chain and userdata members included.</summary>
    public required IReadOnlyList<StructureMember> Members { get; init; }

    /// <summary>Gets whether the structure is a C union, whose members all start at offset 0.</summary>
    public bool IsUnion { get; init; }

    /// <summary>
    /// Gets how C code names the type: <c>struct SDL_Rect</c> for a tag, which a typedef may not
    /// repeat, or the typedef of an anonymous record, such as <c>ma_vec3f</c>; <see langword="null"/>
    /// for an anonymous record held by another, which only <see cref="Position"/> reaches.
    /// </summary>
    public string? CTypeName { get; init; }

    /// <summary>Gets where an anonymous record lies in the record that holds it, or <see langword="null"/> for a named one.</summary>
    public RecordPosition? Position { get; init; }

    /// <summary>
    /// Gets the full name of the .NET type that the configuration maps the structure to, such as
    /// <c>System.Numerics.Vector3</c>, or <see langword="null"/> for a structure the bindings
    /// declare. A mapped structure is never emitted: its members only serve the layout tests,
    /// which check that the .NET type has its layout (ADR 0033, ADR 0036).
    /// </summary>
    public string? DotNetType { get; init; }

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
