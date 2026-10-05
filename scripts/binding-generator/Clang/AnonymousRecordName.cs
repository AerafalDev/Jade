using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Clang;

/// <summary>The name that an anonymous structure or union gets from the member that holds it (ADR 0027).</summary>
/// <param name="CName">The name standing for its C name: <c>{parent}_{member}</c>, as in <c>SDL_GamepadBinding_input</c>.</param>
/// <param name="Words">The words of the parent's name followed by those of the member, which give <c>{Parent}{Member}</c>.</param>
/// <param name="Header">The header that declares the parent.</param>
/// <param name="Position">Where it lies in its parent, which the layout tests reach it through (ADR 0036).</param>
internal sealed record AnonymousRecordName(string CName, IReadOnlyList<string> Words, string Header, RecordPosition Position)
{
    /// <summary>Gets the name of an anonymous record that is the element of an array member, whose own name goes to the array (ADR 0033).</summary>
    /// <returns>The name with an <c>element</c> suffix, at the first element of the array.</returns>
    public AnonymousRecordName ForElement()
    {
        return new AnonymousRecordName($"{CName}_element", [.. Words, "element"], Header, Position with { Designator = $"{Position.Designator}[0]" });
    }
}
