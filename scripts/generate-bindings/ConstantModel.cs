/// <summary>An object-like macro that evaluates to an integer, floating-point or string constant.</summary>
internal sealed class ConstantModel
{
    /// <summary>Gets the macro name.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# name in the library's static class.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the group of the header defining the macro, which names the partial class file.</summary>
    public required string Group { get; init; }

    /// <summary>
    /// Gets the type: a <see cref="TypeKind.Primitive"/> for numbers, a <see cref="TypeKind.Named"/> enum, or a
    /// <see cref="TypeKind.FixedArray"/> of <see cref="PrimitiveType.Char"/> for a string.
    /// </summary>
    public required TypeRef Type { get; init; }

    /// <summary>Gets the bits of an integer or enum value, sign-extended to 64 bits for negative values.</summary>
    public ulong Integer { get; init; }

    /// <summary>Gets a floating-point value.</summary>
    public double Float { get; init; }

    /// <summary>Gets a string value, without its terminator.</summary>
    public string? Text { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }

    /// <summary>Gets the only platforms (<c>OperatingSystem.IsOSPlatform</c> names) whose headers define the macro, or none when all do.</summary>
    public IReadOnlyList<string> SupportedPlatforms { get; init; } = [];

    /// <summary>Gets whether the value is a string.</summary>
    public bool IsString => Type.Kind == TypeKind.FixedArray;

    /// <summary>Describes the constant, for comparing a macro read on different targets.</summary>
    /// <returns>A string that is equal for equal constants.</returns>
    public string Describe() => $"{Type.Describe()} {Name} = {Integer} {Float} {Text} in {Group}";
}
