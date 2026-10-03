using System.Runtime.InteropServices;

/// <summary>Raw imports of a few functions per bundled library, enough to prove the exports resolve and to run ImGui on its null backends.</summary>
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

    /// <summary><c>ImGui_GetVersion</c>: the ImGui version compiled in.</summary>
    /// <returns>A static UTF-8 string such as <c>1.92.9b</c>.</returns>
    [DllImport("jade_native")]
    public static extern byte* ImGui_GetVersion();

    /// <summary><c>DearBindings_GetVersion</c>: the dear_bindings version that generated the C API.</summary>
    /// <returns>A static UTF-8 string such as <c>0.24</c>.</returns>
    [DllImport("jade_native")]
    public static extern byte* DearBindings_GetVersion();

    /// <summary><c>ImGui_CreateContext</c>: creates an ImGui context and makes it current.</summary>
    /// <param name="sharedFontAtlas">An <c>ImFontAtlas</c> to share, or null for a new one.</param>
    /// <returns>The <c>ImGuiContext</c>.</returns>
    [DllImport("jade_native")]
    public static extern nint ImGui_CreateContext(void* sharedFontAtlas);

    /// <summary><c>ImGui_DestroyContext</c>: destroys an ImGui context.</summary>
    /// <param name="context">The <c>ImGuiContext</c>, or null for the current one.</param>
    [DllImport("jade_native")]
    public static extern void ImGui_DestroyContext(nint context);

    /// <summary><c>ImGui_GetIO</c>: the current context's configuration and inputs.</summary>
    /// <returns>The context's <c>ImGuiIO</c>.</returns>
    [DllImport("jade_native")]
    public static extern ImGuiIO* ImGui_GetIO();

    /// <summary><c>ImGui_NewFrame</c>: starts a frame.</summary>
    [DllImport("jade_native")]
    public static extern void ImGui_NewFrame();

    /// <summary><c>ImGui_Begin</c>: begins a window.</summary>
    /// <param name="name">The null-terminated UTF-8 window name.</param>
    /// <param name="open">A C <c>bool</c> cleared when the window is closed, or null for no close button.</param>
    /// <param name="flags">The <c>ImGuiWindowFlags</c>.</param>
    /// <returns>A C <c>bool</c>: nonzero when the window's contents are visible.</returns>
    [DllImport("jade_native")]
    public static extern byte ImGui_Begin(byte* name, byte* open, int flags);

    /// <summary><c>ImGui_TextUnformatted</c>: draws text without formatting it.</summary>
    /// <param name="text">The null-terminated UTF-8 text.</param>
    [DllImport("jade_native")]
    public static extern void ImGui_TextUnformatted(byte* text);

    /// <summary><c>ImGui_End</c>: ends the window that <see cref="ImGui_Begin"/> began.</summary>
    [DllImport("jade_native")]
    public static extern void ImGui_End();

    /// <summary><c>ImGui_Render</c>: ends the frame and builds its draw data.</summary>
    [DllImport("jade_native")]
    public static extern void ImGui_Render();

    /// <summary><c>ImGui_GetDrawData</c>: the draw data of the last <see cref="ImGui_Render"/>.</summary>
    /// <returns>The <c>ImDrawData</c>.</returns>
    [DllImport("jade_native")]
    public static extern ImDrawData* ImGui_GetDrawData();

    /// <summary><c>cImGui_ImplNull_Init</c>: sets up the null platform and renderer backends.</summary>
    /// <returns>A C <c>bool</c>: nonzero on success.</returns>
    [DllImport("jade_native")]
    public static extern byte cImGui_ImplNull_Init();

    /// <summary><c>cImGui_ImplNull_NewFrame</c>: starts a frame on the null backends (a 1920 x 1080 display).</summary>
    [DllImport("jade_native")]
    public static extern void cImGui_ImplNull_NewFrame();

    /// <summary><c>cImGui_ImplNullRender_RenderDrawData</c>: "renders" draw data, which only marks its textures as uploaded.</summary>
    /// <param name="drawData">The <c>ImDrawData</c>.</param>
    [DllImport("jade_native")]
    public static extern void cImGui_ImplNullRender_RenderDrawData(ImDrawData* drawData);

    /// <summary><c>cImGui_ImplNull_Shutdown</c>: shuts the null backends down.</summary>
    [DllImport("jade_native")]
    public static extern void cImGui_ImplNull_Shutdown();
}
