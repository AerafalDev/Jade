namespace Jade.BindingGenerator.Model;

/// <summary>The intermediate representation of a library's C API (ADR 0026).</summary>
/// <remarks>
/// Every collection of declarations is ordered by C name, with an ordinal comparison, so that the
/// output never depends on the order of the inputs.
/// </remarks>
internal sealed class ApiModel
{
    /// <summary>The declarations of every kind, by C name.</summary>
    private readonly Dictionary<string, Declaration> _declarations = [with(StringComparer.Ordinal)];

    /// <summary>Initializes a new instance of the <see cref="ApiModel"/> class.</summary>
    /// <param name="constants">The constants.</param>
    /// <param name="typedefs">The typedefs the API declares itself.</param>
    /// <param name="enums">The enums and sets of flags.</param>
    /// <param name="handles">The opaque handles.</param>
    /// <param name="structures">The structures.</param>
    /// <param name="functionPointers">The function pointer types.</param>
    /// <param name="functions">The exported functions.</param>
    /// <param name="skipped">The declarations of the inputs that the model leaves out.</param>
    /// <exception cref="InvalidDataException">Two declarations have the same C name.</exception>
    public ApiModel(
        IEnumerable<ConstantDeclaration> constants,
        IEnumerable<TypedefDeclaration> typedefs,
        IEnumerable<EnumDeclaration> enums,
        IEnumerable<HandleDeclaration> handles,
        IEnumerable<StructureDeclaration> structures,
        IEnumerable<FunctionPointerDeclaration> functionPointers,
        IEnumerable<FunctionDeclaration> functions,
        IEnumerable<SkippedDeclaration> skipped)
    {
        Constants = Sort(constants);
        Typedefs = Sort(typedefs);
        Enums = Sort(enums);
        Handles = Sort(handles);
        Structures = Sort(structures);
        FunctionPointers = Sort(functionPointers);
        Functions = Sort(functions);
        Skipped = [.. skipped.OrderBy(static declaration => declaration.Name, StringComparer.Ordinal)];

        IEnumerable<Declaration> all = [.. Constants, .. Typedefs, .. Enums, .. Handles, .. Structures, .. FunctionPointers, .. Functions];

        foreach (var declaration in all)
        {
            if (!_declarations.TryAdd(declaration.CName, declaration))
            {
                throw new InvalidDataException($"'{declaration.CName}' is declared twice.");
            }
        }
    }

    /// <summary>Gets the constants.</summary>
    public IReadOnlyList<ConstantDeclaration> Constants { get; }

    /// <summary>Gets the typedefs the API declares itself.</summary>
    public IReadOnlyList<TypedefDeclaration> Typedefs { get; }

    /// <summary>Gets the enums and sets of flags.</summary>
    public IReadOnlyList<EnumDeclaration> Enums { get; }

    /// <summary>Gets the opaque handles.</summary>
    public IReadOnlyList<HandleDeclaration> Handles { get; }

    /// <summary>Gets the structures.</summary>
    public IReadOnlyList<StructureDeclaration> Structures { get; }

    /// <summary>Gets the function pointer types.</summary>
    public IReadOnlyList<FunctionPointerDeclaration> FunctionPointers { get; }

    /// <summary>Gets the exported functions.</summary>
    public IReadOnlyList<FunctionDeclaration> Functions { get; }

    /// <summary>Gets the declarations of the inputs that the model leaves out, ordered by name.</summary>
    public IReadOnlyList<SkippedDeclaration> Skipped { get; }

    /// <summary>Gets a declaration by C name.</summary>
    /// <typeparam name="T">The kind of declaration expected.</typeparam>
    /// <param name="cName">The C name.</param>
    /// <returns>The declaration.</returns>
    /// <exception cref="InvalidDataException">No declaration of that kind has this name.</exception>
    public T Get<T>(string cName)
        where T : Declaration
    {
        return _declarations.TryGetValue(cName, out var declaration) && declaration is T typed
            ? typed
            : throw new InvalidDataException($"No {typeof(T).Name} is named '{cName}'.");
    }

    /// <summary>Gets a declaration by C name, whatever its kind.</summary>
    /// <param name="cName">The C name.</param>
    /// <returns>The declaration, or <see langword="null"/> when none has this name.</returns>
    public Declaration? Find(string cName)
    {
        return _declarations.GetValueOrDefault(cName);
    }

    /// <summary>Orders declarations by C name.</summary>
    /// <typeparam name="T">The kind of declaration.</typeparam>
    /// <param name="declarations">The declarations.</param>
    /// <returns>The declarations, ordered by C name with an ordinal comparison.</returns>
    private static List<T> Sort<T>(IEnumerable<T> declarations)
        where T : Declaration
    {
        return [.. declarations.OrderBy(static declaration => declaration.CName, StringComparer.Ordinal)];
    }
}
