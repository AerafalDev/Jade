/// <summary>
/// Everything the generator needs to know about one C library beyond its headers. Each library has one
/// <c>*Config.cs</c> file that builds an instance; every exclusion and decision carries its reason there.
/// </summary>
internal sealed class LibraryConfig
{
    /// <summary>Gets the library name: output folder <c>Generated/&lt;Name&gt;/</c> and namespace <c>Jade.Interop.&lt;Name&gt;</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the upstream's name in <c>metadata/versions.json</c>, whose <c>defines</c> are passed to the parser.</summary>
    public required string Upstream { get; init; }

    /// <summary>Gets the folder under the staged <c>include/</c> that holds the library's headers. Declarations elsewhere are never bound.</summary>
    public required string IncludeDirectory { get; init; }

    /// <summary>Gets the static class holding the functions and constants.</summary>
    public required string FunctionsClass { get; init; }

    /// <summary>Gets the prefixes stripped from C names, longest first.</summary>
    public required IReadOnlyList<string> Prefixes { get; init; }

    /// <summary>Gets the prefixes of the library's symbols in jade_native's export table, for the export cross-check.</summary>
    public required IReadOnlyList<string> ExportPrefixes { get; init; }

    /// <summary>
    /// Gets the headers whose functions, enums and object-like macros are bound. Structs and other types are bound
    /// when a bound declaration uses them, wherever they are declared in the library. Unused with <see cref="ApiDescription"/>.
    /// </summary>
    public IReadOnlyList<HeaderConfig> Headers { get; init; } = [];

    /// <summary>
    /// Gets the file under the staged <c>metadata/</c> folder that describes the API, read by <see cref="DawnJsonReader"/>
    /// instead of parsing the headers (ADR-0005), or <see langword="null"/> for a library read from its headers.
    /// </summary>
    public string? ApiDescription { get; init; }

    /// <summary>
    /// Gets the header, relative to the staged <c>include/</c> folder, that declares what <see cref="ApiDescription"/>
    /// describes. It is only parsed to cross-check the model and to measure struct layouts per target (<see cref="DawnHeaderCheck"/>).
    /// </summary>
    public string? CrossCheckHeader { get; init; }

    /// <summary>
    /// Gets the headers that are parsed but not bound, with the reason. Every header of <see cref="IncludeDirectory"/>
    /// is either bound or excluded; functions declared in an excluded header count as excluded for the export cross-check.
    /// </summary>
    public IReadOnlyDictionary<string, string> ExcludedHeaders { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the targets to parse. Every target must yield the same declarations, except functions only declared on some.</summary>
    public IReadOnlyList<Target> Targets { get; init; } = global::Targets.All;

    /// <summary>Gets the managed type of C <c>bool</c>/<c>_Bool</c>, which must have its size on every target (ADR-0012).</summary>
    public PrimitiveType Bool { get; init; } = PrimitiveType.Bool;

    /// <summary>Gets whether <c>const char*</c> parameters get <c>ReadOnlySpan&lt;byte&gt;</c> UTF-8 overloads unless a rule says otherwise.</summary>
    public bool Utf8Strings { get; init; } = true;

    /// <summary>Gets whether parameter rules are inferred from upstream parameter docs where <see cref="Parameters"/> has none (<see cref="ParameterInference"/>).</summary>
    public bool InferParameters { get; init; }

    /// <summary>Gets what replaces the leading <c>_</c> of a name that would start with a digit, such as <c>SDL_SCANCODE_1</c>.</summary>
    public string LeadingDigitPrefix { get; init; } = "Digit";

    /// <summary>
    /// Gets the functions, macros and enums that are not bound, by C name, with the reason. An entry may also name an
    /// exported function no target declares.
    /// </summary>
    public IReadOnlyDictionary<string, string> Exclusions { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the opaque C structs exposed as handles. <c>T*</c> becomes the handle, and functions taking it first become instance methods.</summary>
    public IReadOnlyList<string> Handles { get; init; } = [];

    /// <summary>
    /// Gets the word native function names use for a handle when it differs from the handle's own name, by C name, so
    /// instance methods drop it (<c>SDL_ReadIO</c> on <c>SDL_IOStream</c> becomes <c>Read</c> with the stem <c>IO</c>).
    /// </summary>
    public IReadOnlyDictionary<string, string> MethodStems { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets opaque structs of another API exposed as handles without instance methods, such as Vulkan's.</summary>
    public IReadOnlyList<string> ForeignHandles { get; init; } = [];

    /// <summary>Gets opaque C structs that are not handles, exposed as empty structs used through pointers.</summary>
    public IReadOnlyList<string> OpaqueStructs { get; init; } = [];

    /// <summary>Gets integer typedefs that identify objects, exposed as enums without members so IDs of different kinds do not mix.</summary>
    public IReadOnlyList<string> IdTypedefs { get; init; } = [];

    /// <summary>Gets typedefs mapped to a scalar on every target instead of what they stand for, by C name.</summary>
    public IReadOnlyDictionary<string, PrimitiveType> TypedefMappings { get; init; } = new Dictionary<string, PrimitiveType>();

    /// <summary>Gets integer typedefs whose values are <c>#define</c>s, by C name.</summary>
    public IReadOnlyDictionary<string, MacroEnum> MacroEnums { get; init; } = new Dictionary<string, MacroEnum>();

    /// <summary>Gets C enums whose values combine as bits.</summary>
    public IReadOnlyList<string> FlagEnums { get; init; } = [];

    /// <summary>Gets C# names that differ from the naming rules, by C name (<c>Function</c>, <c>Type</c>, <c>Type.member</c> or <c>Function.parameter</c>).</summary>
    public IReadOnlyDictionary<string, string> Renames { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the casing of all-caps words in enum members, constants and fields, for example <c>OPENGL</c> to <c>OpenGL</c>.</summary>
    public IReadOnlyDictionary<string, string> Words { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets parameter annotations, by <c>Function.parameter</c> C names. They take precedence over inferred rules.</summary>
    public IReadOnlyDictionary<string, ParameterRule> Parameters { get; init; } = new Dictionary<string, ParameterRule>();

    /// <summary>Gets unions bound with a subset of their members, by C name. The union keeps its native size.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> UnionMembers { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Gets how each type whose layout differs between targets is exposed, by C name.</summary>
    public IReadOnlyDictionary<string, LayoutDecision> LayoutDecisions { get; init; } = new Dictionary<string, LayoutDecision>();

    /// <summary>
    /// Gets functions declared on every target that only work on some platforms, by C name, as
    /// <c>OperatingSystem.IsOSPlatform</c> names (<c>browser</c>, <c>ios</c>, ...). Functions declared on some targets only
    /// get their platforms from the headers instead.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SupportedPlatforms { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Gets functions unavailable on some platforms, by C name, as <c>OperatingSystem.IsOSPlatform</c> names.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> UnsupportedPlatforms { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>
    /// Gets remarks added to the documentation of declarations, by C name, for facts about jade_native's build that
    /// upstream cannot document. Only <see cref="DawnJsonReader"/> applies them so far.
    /// </summary>
    public IReadOnlyDictionary<string, string> Notes { get; init; } = new Dictionary<string, string>();
}
