using System.Runtime.InteropServices;

/// <summary>Raw imports of one function per bundled library, enough to prove the exports resolve.</summary>
internal static unsafe class NativeMethods
{
    /// <summary><c>SDL_GetVersion</c>: the linked SDL version as <c>major * 1000000 + minor * 1000 + micro</c>.</summary>
    /// <returns>The encoded version.</returns>
    [DllImport("jade_native")]
    public static extern int SDL_GetVersion();

    /// <summary><c>SDL_GetRevision</c>: the source revision SDL was built from.</summary>
    /// <returns>A static UTF-8 string.</returns>
    [DllImport("jade_native")]
    public static extern byte* SDL_GetRevision();

    /// <summary><c>SDL_GetNumVideoDrivers</c>: the number of video drivers compiled in.</summary>
    /// <returns>The driver count.</returns>
    [DllImport("jade_native")]
    public static extern int SDL_GetNumVideoDrivers();

    /// <summary><c>SDL_GetVideoDriver</c>: the name of a compiled-in video driver.</summary>
    /// <param name="index">Index below <see cref="SDL_GetNumVideoDrivers"/>.</param>
    /// <returns>A static UTF-8 string.</returns>
    [DllImport("jade_native")]
    public static extern byte* SDL_GetVideoDriver(int index);

    /// <summary><c>ma_version_string</c>: the miniaudio version.</summary>
    /// <returns>A static UTF-8 string such as <c>0.11.25</c>.</returns>
    [DllImport("jade_native")]
    public static extern byte* ma_version_string();

    /// <summary><c>jade_native_abi_version</c>: the ABI version compiled into the library.</summary>
    /// <returns>JADE_NATIVE_ABI_VERSION from jade_native.h.</returns>
    [DllImport("jade_native")]
    public static extern uint jade_native_abi_version();
}
