/// <summary>The xmake configuration that builds jade_native for one .NET RID.</summary>
internal sealed class XmakeTarget
{
    /// <summary>Creates a mapping.</summary>
    /// <param name="platform">xmake platform (<c>-p</c>).</param>
    /// <param name="architecture">xmake architecture (<c>-a</c>).</param>
    /// <param name="toolchain">xmake toolchain (<c>--toolchain</c>), or <see langword="null"/> for the platform's default.</param>
    /// <param name="verified">Whether a build through this mapping has been checked on its native OS.</param>
    /// <param name="extraArguments">Further <c>xmake f</c> arguments.</param>
    public XmakeTarget(string platform, string architecture, string? toolchain, bool verified, params string[] extraArguments)
    {
        Platform = platform;
        Architecture = architecture;
        Toolchain = toolchain;
        Verified = verified;
        ExtraArguments = extraArguments;
    }

    /// <summary>Gets the xmake platform (<c>-p</c>).</summary>
    public string Platform { get; }

    /// <summary>Gets the xmake architecture (<c>-a</c>).</summary>
    public string Architecture { get; }

    /// <summary>Gets the xmake toolchain (<c>--toolchain</c>), or <see langword="null"/> for the platform's default.</summary>
    public string? Toolchain { get; }

    /// <summary>Gets whether a build through this mapping has been checked on its native OS.</summary>
    public bool Verified { get; }

    /// <summary>Gets further <c>xmake f</c> arguments.</summary>
    public string[] ExtraArguments { get; }
}
