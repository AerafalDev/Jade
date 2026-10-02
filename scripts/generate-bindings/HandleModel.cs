/// <summary>An opaque C struct only used through pointers, exposed as a pointer-sized handle struct (ADR-0006).</summary>
internal sealed class HandleModel
{
    /// <summary>Gets the C struct name; <c>NativeName*</c> maps to the handle.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# struct name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }
}
