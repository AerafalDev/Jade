using System.Collections.Frozen;
using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>The C header front-end settings of a library configuration: what to parse, and the annotations the headers cannot express (ADR 0033).</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ClangConfiguration
{
    /// <summary>Gets the headers to parse, as written in an <c>#include</c> directive.</summary>
    public required IReadOnlyList<string> Headers { get; init; }

    /// <summary>Gets the include directories, relative to the dependency's sources.</summary>
    public required IReadOnlyList<string> IncludeDirectories { get; init; }

    /// <summary>
    /// Gets the files included before the headers, relative to the repository root. The native
    /// build force-includes the same files, so both see the same configuration macros.
    /// </summary>
    public IReadOnlyList<string> ForcedIncludes { get; init; } = [];

    /// <summary>
    /// Gets the macros defined for the parse only, as <c>NAME</c> or <c>NAME=value</c>: they
    /// select what the headers declare for the application, never how the library is built.
    /// </summary>
    public IReadOnlyList<string> Defines { get; init; } = [];

    /// <summary>
    /// Gets the headers of the repository's own C shim, relative to the repository root, included
    /// after <see cref="Headers"/>; their declarations belong to the library (ADR 0006).
    /// </summary>
    public IReadOnlyList<string> Shims { get; init; } = [];

    /// <summary>
    /// Gets the prefixes that .NET names drop, such as <c>SDL_</c>, the longest that matches first;
    /// they are matched without regard to case, so <c>ma_</c> also covers <c>MA_</c> (ADR 0027).
    /// </summary>
    public required IReadOnlyList<string> Prefixes { get; init; }

    /// <summary>
    /// Gets the words that the .NET names of members and parameters drop when they come first, such
    /// as the <c>p</c> and <c>pp</c> of miniaudio's Hungarian notation for pointers (ADR 0034).
    /// </summary>
    public IReadOnlyList<string> MemberPrefixes { get; init; } = [];

    /// <summary>
    /// Gets the headers whose declarations are left out, with the reason, as written in an
    /// <c>#include</c> directive; the types that the other headers use are kept.
    /// </summary>
    public IReadOnlyDictionary<string, string> ExcludeHeaders { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the structures and typedefs whose layout the bindings do not reproduce, by C name, with
    /// the reason: they become handles, used through pointers only (ADR 0006).
    /// </summary>
    public IReadOnlyDictionary<string, string> Opaque { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Gets the functions that allocate and free an opaque type, as patterns where <c>{name}</c>
    /// stands for the C name of the type; when set, every opaque type must have both.
    /// </summary>
    public OpaqueAllocators? OpaqueAllocators { get; init; }

    /// <summary>
    /// Gets the C types mapped by name, whatever they alias on each target (ADR 0027, ADR 0033): to a
    /// builtin type such as <c>uintptr_t</c> or a pointer such as <c>void*</c>, for pointer-sized
    /// typedefs; to <c>void</c> for a type only ever pointed to whose size differs between targets,
    /// such as <c>wchar_t</c>; or to a .NET type by its full name, such as
    /// <c>System.Numerics.Vector3</c>, for a structure with the same layout.
    /// </summary>
    public IReadOnlyDictionary<string, string> Types { get; init; } = FrozenDictionary<string, string>.Empty;

    /// <summary>Gets the integer typedefs that hold booleans, such as <c>ma_bool32</c> (ADR 0027).</summary>
    public IReadOnlyList<string> Booleans { get; init; } = [];

    /// <summary>
    /// Gets the enums by C name: C enums whose values are flags, and integer typedefs whose values
    /// are macros, such as <c>SDL_WindowFlags</c> and the <c>SDL_WINDOW_*</c> macros (ADR 0027).
    /// </summary>
    public IReadOnlyDictionary<string, EnumConfiguration> Enums { get; init; } = FrozenDictionary<string, EnumConfiguration>.Empty;

    /// <summary>
    /// Gets the regular expressions that select the object-like macros bound as constants; each is
    /// matched against the whole macro name.
    /// </summary>
    public IReadOnlyList<string> Constants { get; init; } = [];
}
