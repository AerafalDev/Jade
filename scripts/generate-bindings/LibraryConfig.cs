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

    /// <summary>Gets the static class holding the functions.</summary>
    public required string FunctionsClass { get; init; }

    /// <summary>Gets the prefixes stripped from C names, longest first.</summary>
    public required IReadOnlyList<string> Prefixes { get; init; }

    /// <summary>Gets the headers whose functions are bound. Types are bound when a bound function or struct uses them.</summary>
    public required IReadOnlyList<HeaderConfig> Headers { get; init; }

    /// <summary>Gets the targets to parse. Every target must yield the same declarations.</summary>
    public IReadOnlyList<Target> Targets { get; init; } = global::Targets.All;

    /// <summary>Gets the managed type of C <c>bool</c>/<c>_Bool</c>, which must have its size on every target.</summary>
    public PrimitiveType Bool { get; init; } = PrimitiveType.Byte;

    /// <summary>Gets whether <c>const char*</c> parameters get <c>ReadOnlySpan&lt;byte&gt;</c> UTF-8 overloads unless a rule says otherwise.</summary>
    public bool Utf8Strings { get; init; } = true;

    /// <summary>Gets functions and flag macros that are not bound, by C name, with the reason.</summary>
    public IReadOnlyDictionary<string, string> Exclusions { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the opaque C structs exposed as handles. <c>T*</c> becomes the handle, and functions taking it first become instance methods.</summary>
    public IReadOnlyList<string> Handles { get; init; } = [];

    /// <summary>Gets opaque C structs that are not handles, exposed as empty structs used through pointers.</summary>
    public IReadOnlyList<string> OpaqueStructs { get; init; } = [];

    /// <summary>Gets integer typedefs whose values are <c>#define</c>s, with the macro prefix. They become <c>[Flags]</c> enums of the macros with that prefix in the typedef's header.</summary>
    public IReadOnlyDictionary<string, string> FlagMacros { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets C enums whose values combine as bits.</summary>
    public IReadOnlyList<string> FlagEnums { get; init; } = [];

    /// <summary>Gets C# names that differ from the naming rules, by C name (<c>Function</c>, <c>Type</c>, <c>Type.member</c> or <c>Function.parameter</c>).</summary>
    public IReadOnlyDictionary<string, string> Renames { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets the casing of all-caps words in enum members and fields, for example <c>OPENGL</c> to <c>OpenGL</c>.</summary>
    public IReadOnlyDictionary<string, string> Words { get; init; } = new Dictionary<string, string>();

    /// <summary>Gets parameter annotations, by <c>Function.parameter</c> C names.</summary>
    public IReadOnlyDictionary<string, ParameterRule> Parameters { get; init; } = new Dictionary<string, ParameterRule>();

    /// <summary>Gets unions bound with a subset of their members, by C name. The union keeps its native size.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> UnionMembers { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Gets how each type whose layout differs between targets is exposed, by C name.</summary>
    public IReadOnlyDictionary<string, LayoutDecision> LayoutDecisions { get; init; } = new Dictionary<string, LayoutDecision>();

    /// <summary>Gets functions unavailable on some platforms, by C name, as <c>OperatingSystem.IsOSPlatform</c> names (<c>browser</c>, <c>ios</c>, ...).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> UnsupportedPlatforms { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
}
