using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Model;

/// <summary>A named declaration of a C API, with the platforms it is available on (ADR 0026).</summary>
internal abstract record Declaration
{
    /// <summary>Gets the C name of the declaration.</summary>
    public required string CName { get; init; }

    /// <summary>
    /// Gets the words of the name without the library prefix, which the projection turns into a
    /// .NET name (ADR 0027).
    /// </summary>
    public required IReadOnlyList<string> Words { get; init; }

    /// <summary>Gets the platform families the declaration is available on.</summary>
    public required Platforms Availability { get; init; }
}
