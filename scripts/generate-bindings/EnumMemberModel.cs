/// <summary>An enum constant, or one flag macro of a flags typedef.</summary>
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
}
