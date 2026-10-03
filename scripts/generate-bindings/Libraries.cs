/// <summary>Every library the generator binds, from C headers or an API description.</summary>
internal static class Libraries
{
    /// <summary>Gets the configs, in generation order.</summary>
    public static IReadOnlyList<LibraryConfig> All { get; } = [Sdl3Config.Create(), WebGpuConfig.Create()];
}
