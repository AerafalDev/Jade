/// <summary>A public header of jade_native itself, in a <see cref="Manifest"/>.</summary>
internal sealed class ManifestHeader
{
    /// <summary>Gets the absolute path of the header in native/.</summary>
    public required string Source { get; init; }

    /// <summary>Gets the path to stage it at, relative to include/ (for example <c>jade/jade_native.h</c>).</summary>
    public required string Destination { get; init; }
}
