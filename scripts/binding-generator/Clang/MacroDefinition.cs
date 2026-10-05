namespace Jade.BindingGenerator.Clang;

/// <summary>An object-like macro that a library's header defines.</summary>
/// <param name="Name">The name of the macro.</param>
/// <param name="Header">The header that defines it.</param>
internal sealed record MacroDefinition(string Name, string Header);
