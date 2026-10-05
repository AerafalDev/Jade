namespace Jade.BindingGenerator.Clang;

/// <summary>The value of a macro, as clang evaluates it for one target (ADR 0027).</summary>
/// <param name="Kind">The kind of value.</param>
/// <param name="Size">The size of the value's C type in bytes; for a string, the size of its characters.</param>
/// <param name="Bits">An integer, as the bits of a 64-bit two's complement value.</param>
/// <param name="Number">A floating-point number.</param>
/// <param name="Text">A string, without its terminator.</param>
/// <param name="Header">The header that defines the macro.</param>
internal sealed record MacroValue(MacroValueKind Kind, long Size, ulong Bits, double Number, string? Text, string Header);
