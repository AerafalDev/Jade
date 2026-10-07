using System.Runtime.InteropServices;

namespace Jade.Wgpu.Tests;

/// <summary>
/// A window created with SDL3 for the surface smoke test, and the surface source of its native
/// handles.
/// </summary>
/// <remarks>
/// The raw layer of <c>Jade.Sdl</c> is internal to its own tests and its idiomatic layer does not
/// exist yet (roadmap task 13), so the few SDL3 functions the test needs are loaded here, as the
/// layout tests load their library: the test project copies <c>libSDL3</c> with the other natives.
/// </remarks>
internal sealed unsafe class TestWindow : IDisposable
{
    private const string LibraryName = "SDL3";

    private const uint InitVideo = 0x20;

    private readonly delegate* unmanaged[Cdecl]<nint, void> _destroyWindow;

    private readonly delegate* unmanaged[Cdecl]<void> _quit;

    private readonly delegate* unmanaged[Cdecl]<nint, void> _destroyMetalView;

    private readonly delegate* unmanaged[Cdecl]<void> _pumpEvents;

    private readonly nint _window;

    private readonly nint _metalView;

    private TestWindow(nint library, nint window, uint width, uint height)
    {
        _destroyWindow = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "SDL_DestroyWindow");
        _quit = (delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(library, "SDL_Quit");
        _pumpEvents = (delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(library, "SDL_PumpEvents");
        _window = window;
        Width = width;
        Height = height;

        var properties = ((delegate* unmanaged[Cdecl]<nint, uint>)NativeLibrary.GetExport(library, "SDL_GetWindowProperties"))(window);
        var getPointer = (delegate* unmanaged[Cdecl]<uint, byte*, nint, nint>)NativeLibrary.GetExport(library, "SDL_GetPointerProperty");
        var getNumber = (delegate* unmanaged[Cdecl]<uint, byte*, long, long>)NativeLibrary.GetExport(library, "SDL_GetNumberProperty");
        var driver = Marshal.PtrToStringUTF8(((delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(library, "SDL_GetCurrentVideoDriver"))());

        // The names of the properties are SDL_PROP_WINDOW_* in SDL3/SDL_video.h; a UTF-8 literal is
        // NUL-terminated in memory.
        fixed (byte* waylandDisplay = "SDL.window.wayland.display"u8)
        fixed (byte* waylandSurface = "SDL.window.wayland.surface"u8)
        fixed (byte* x11Display = "SDL.window.x11.display"u8)
        fixed (byte* x11Window = "SDL.window.x11.window"u8)
        fixed (byte* win32Instance = "SDL.window.win32.instance"u8)
        fixed (byte* win32Window = "SDL.window.win32.hwnd"u8)
        {
            Source = driver switch
            {
                "wayland" => new SurfaceSource(SurfaceSourceKind.Wayland, getPointer(properties, waylandDisplay, 0), getPointer(properties, waylandSurface, 0), 0),
                "x11" => new SurfaceSource(SurfaceSourceKind.Xlib, getPointer(properties, x11Display, 0), 0, (ulong)getNumber(properties, x11Window, 0)),
                "windows" => new SurfaceSource(SurfaceSourceKind.Windows, getPointer(properties, win32Instance, 0), getPointer(properties, win32Window, 0), 0),
                "cocoa" => CreateMetalSource(library, window, out _metalView, out _destroyMetalView),
                _ => throw new AssertInconclusiveException($"The surface smoke test does not support the '{driver}' video driver."),
            };
        }
    }

    internal enum SurfaceSourceKind
    {
        Wayland,
        Xlib,
        Windows,
        Metal,
    }

    public uint Width { get; }

    public uint Height { get; }

    public SurfaceSource Source { get; }

    public static TestWindow Create(uint width, uint height)
    {
        if (OperatingSystem.IsLinux() && Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is null && Environment.GetEnvironmentVariable("DISPLAY") is null)
        {
            Assert.Inconclusive("The host has no display server.");
        }

        if (!NativeLibrary.TryLoad(LibraryName, typeof(TestWindow).Assembly, DllImportSearchPath.AssemblyDirectory, out var library))
        {
            Assert.Inconclusive($"The {LibraryName} library is not built for this host: run 'dotnet run scripts/build-native.cs'.");
        }

        var init = (delegate* unmanaged[Cdecl]<uint, byte>)NativeLibrary.GetExport(library, "SDL_Init");
        var createWindow = (delegate* unmanaged[Cdecl]<byte*, int, int, ulong, nint>)NativeLibrary.GetExport(library, "SDL_CreateWindow");

        if (init(InitVideo) == 0)
        {
            Assert.Inconclusive($"SDL3 cannot initialize video: {GetError(library)}");
        }

        nint window;

        fixed (byte* title = "Jade.Wgpu.Tests"u8)
        {
            window = createWindow(title, (int)width, (int)height, 0);
        }

        if (window == 0)
        {
            var error = GetError(library);

            ((delegate* unmanaged[Cdecl]<void>)NativeLibrary.GetExport(library, "SDL_Quit"))();
            Assert.Inconclusive($"SDL3 cannot create a window: {error}");
        }

        return new TestWindow(library, window, width, height);
    }

    public void PumpEvents()
    {
        _pumpEvents();
    }

    public void Dispose()
    {
        if (_metalView != 0)
        {
            _destroyMetalView(_metalView);
        }

        _destroyWindow(_window);
        _quit();
    }

    private static SurfaceSource CreateMetalSource(nint library, nint window, out nint view, out delegate* unmanaged[Cdecl]<nint, void> destroyView)
    {
        view = ((delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(library, "SDL_Metal_CreateView"))(window);
        destroyView = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(library, "SDL_Metal_DestroyView");

        return new SurfaceSource(SurfaceSourceKind.Metal, ((delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(library, "SDL_Metal_GetLayer"))(view), 0, 0);
    }

    private static string? GetError(nint library)
    {
        return Marshal.PtrToStringUTF8(((delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(library, "SDL_GetError"))());
    }

    /// <summary>The native handles a surface is created from.</summary>
    /// <param name="Kind">The window system.</param>
    /// <param name="First">The display, the module instance or the Metal layer.</param>
    /// <param name="Second">The Wayland surface or the window handle of Windows.</param>
    /// <param name="Window">The X11 window.</param>
    internal readonly record struct SurfaceSource(SurfaceSourceKind Kind, nint First, nint Second, ulong Window);
}
