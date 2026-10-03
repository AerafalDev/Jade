/// <summary>Everything one library binds, as produced by a reader and consumed by <see cref="CSharpEmitter"/>.</summary>
internal sealed class LibraryModel
{
    /// <summary>Gets the library name, which is also the output folder and the last namespace segment (for example <c>Sdl3</c>).</summary>
    public required string Name { get; init; }

    /// <summary>Gets the C# namespace.</summary>
    public required string Namespace { get; init; }

    /// <summary>Gets the static class holding the functions and constants.</summary>
    public required string FunctionsClass { get; init; }

    /// <summary>Gets the function groups in output order, each with the header or section it comes from.</summary>
    public required IReadOnlyList<KeyValuePair<string, string>> Groups { get; init; }

    /// <summary>Gets the functions, in source order.</summary>
    public required IReadOnlyList<FunctionModel> Functions { get; init; }

    /// <summary>Gets the enums.</summary>
    public required IReadOnlyList<EnumModel> Enums { get; init; }

    /// <summary>Gets the structs and unions.</summary>
    public required IReadOnlyList<StructModel> Structs { get; init; }

    /// <summary>Gets the handles.</summary>
    public required IReadOnlyList<HandleModel> Handles { get; init; }

    /// <summary>Gets the constants, in source order.</summary>
    public required IReadOnlyList<ConstantModel> Constants { get; init; }

    /// <summary>Gets the documentation of the callback typedefs that <see cref="TypeRef.Alias"/> names, by C name.</summary>
    public required IReadOnlyDictionary<string, Documentation> Callbacks { get; init; }
}
