/// <summary>An opaque C struct only used through pointers, exposed as a pointer-sized handle struct (ADR-0006).</summary>
internal sealed class HandleModel
{
    /// <summary>Gets the C struct name; <c>NativeName*</c> maps to the handle.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# struct name.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the name the native functions use for the type, before any rename: instance methods drop it from the function
    /// name (<c>SDL_LockMutex</c> on <c>SdlMutex</c> becomes <c>Lock</c>).
    /// </summary>
    public required string Stem { get; init; }

    /// <summary>Gets whether the handle belongs to another API (<see cref="LibraryConfig.ForeignHandles"/>), which gives it no instance methods.</summary>
    public bool IsForeign { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }
}
