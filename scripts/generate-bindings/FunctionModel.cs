/// <summary>An exported C function.</summary>
internal sealed class FunctionModel
{
    /// <summary>Gets the exported symbol.</summary>
    public required string NativeName { get; init; }

    /// <summary>Gets the C# method name in the library's static class.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the return type.</summary>
    public required TypeRef Return { get; init; }

    /// <summary>Gets the parameters in C order.</summary>
    public required IReadOnlyList<ParameterModel> Parameters { get; init; }

    /// <summary>Gets the group, which names the partial class file (for example <c>Video</c> for <c>Sdl.Video.g.cs</c>).</summary>
    public required string Group { get; init; }

    /// <summary>Gets the upstream documentation.</summary>
    public required Documentation Documentation { get; init; }

    /// <summary>Gets the platforms (<c>OperatingSystem.IsOSPlatform</c> names) where the function must not be called.</summary>
    public IReadOnlyList<string> UnsupportedPlatforms { get; init; } = [];
}
