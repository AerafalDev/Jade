/// <summary>Every library the generator binds from C headers.</summary>
internal static class Libraries
{
    /// <summary>Gets the configs, in generation order.</summary>
    public static IReadOnlyList<LibraryConfig> All { get; } = [Sdl3Config.Create()];
}
