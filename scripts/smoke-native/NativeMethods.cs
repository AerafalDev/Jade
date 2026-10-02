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

    /// <summary><c>wgpuCreateInstance</c>: creates a WebGPU instance.</summary>
    /// <param name="descriptor">A <c>WGPUInstanceDescriptor</c>, or null for the defaults.</param>
    /// <returns>The <c>WGPUInstance</c>, or null.</returns>
    [DllImport("jade_native")]
    public static extern nint wgpuCreateInstance(void* descriptor);

    /// <summary><c>wgpuInstanceRequestAdapter</c>: requests an adapter, answered through the callback.</summary>
    /// <param name="instance">The <c>WGPUInstance</c>.</param>
    /// <param name="options">The adapter options.</param>
    /// <param name="callbackInfo">The callback and how it is delivered.</param>
    /// <returns>The <c>WGPUFuture</c>, a struct holding a single <c>uint64_t</c>, which every ABI returns like the integer itself.</returns>
    [DllImport("jade_native")]
    public static extern ulong wgpuInstanceRequestAdapter(nint instance, WGPURequestAdapterOptions* options, WGPURequestAdapterCallbackInfo callbackInfo);

    /// <summary><c>wgpuInstanceProcessEvents</c>: runs the callbacks that are ready, for the <c>AllowProcessEvents</c> mode.</summary>
    /// <param name="instance">The <c>WGPUInstance</c>.</param>
    [DllImport("jade_native")]
    public static extern void wgpuInstanceProcessEvents(nint instance);

    /// <summary><c>wgpuInstanceRelease</c>: releases a reference to an instance.</summary>
    /// <param name="instance">The <c>WGPUInstance</c>.</param>
    [DllImport("jade_native")]
    public static extern void wgpuInstanceRelease(nint instance);

    /// <summary><c>wgpuAdapterGetInfo</c>: describes an adapter.</summary>
    /// <param name="adapter">The <c>WGPUAdapter</c>.</param>
    /// <param name="info">Filled on success; free it with <see cref="wgpuAdapterInfoFreeMembers"/>.</param>
    /// <returns><c>WGPUStatus_Success</c> (1) or <c>WGPUStatus_Error</c> (2).</returns>
    [DllImport("jade_native")]
    public static extern uint wgpuAdapterGetInfo(nint adapter, WGPUAdapterInfo* info);

    /// <summary><c>wgpuAdapterInfoFreeMembers</c>: frees the strings of an adapter description.</summary>
    /// <param name="adapterInfo">A description filled by <see cref="wgpuAdapterGetInfo"/>.</param>
    [DllImport("jade_native")]
    public static extern void wgpuAdapterInfoFreeMembers(WGPUAdapterInfo adapterInfo);

    /// <summary><c>wgpuAdapterRelease</c>: releases a reference to an adapter.</summary>
    /// <param name="adapter">The <c>WGPUAdapter</c>.</param>
    [DllImport("jade_native")]
    public static extern void wgpuAdapterRelease(nint adapter);
}
