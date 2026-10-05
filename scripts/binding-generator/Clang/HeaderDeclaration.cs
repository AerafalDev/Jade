namespace Jade.BindingGenerator.Clang;

/// <summary>A named top-level declaration of a library's headers.</summary>
/// <param name="Kind">The kind of declaration.</param>
/// <param name="Name">The C name, such as <c>SDL_CreateWindow</c>.</param>
internal sealed record HeaderDeclaration(HeaderDeclarationKind Kind, string Name)
{
    /// <summary>Orders declarations by name, then by kind, both independently of the parse order.</summary>
    public static IComparer<HeaderDeclaration> ByName { get; } = Comparer<HeaderDeclaration>.Create(static (left, right) =>
    {
        var byName = string.CompareOrdinal(left.Name, right.Name);

        return byName != 0 ? byName : left.Kind.CompareTo(right.Kind);
    });
}
