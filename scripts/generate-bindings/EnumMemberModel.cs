/// <summary>An enum constant, or one macro of a macro enum (<see cref="LibraryConfig.MacroEnums"/>).</summary>
internal sealed class EnumMemberModel
{
    /// <summary>Gets the C name.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the value's bits, sign-extended to 64 bits for negative values.</summary>
    public required ulong Value { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }

    /// <summary>Gets the only platforms (<c>OperatingSystem.IsOSPlatform</c> names) where the member exists, or none when it exists everywhere.</summary>
    public IReadOnlyList<string> SupportedPlatforms { get; init; } = [];

    /// <summary>Gets the platforms (<c>OperatingSystem.IsOSPlatform</c> names) where the member does not exist.</summary>
    public IReadOnlyList<string> UnsupportedPlatforms { get; init; } = [];
}
