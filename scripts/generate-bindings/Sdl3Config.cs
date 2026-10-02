/// <summary>The SDL3 bindings: <c>Jade.Interop.Sdl3</c>, functions in <c>Sdl</c>.</summary>
internal static class Sdl3Config
{
    /// <summary>Builds the config.</summary>
    /// <returns>The SDL3 config.</returns>
    public static LibraryConfig Create() => new()
    {
        Name = "Sdl3",
        Upstream = "sdl3",
        IncludeDirectory = "SDL3",
        FunctionsClass = "Sdl",
        Prefixes = ["SDL_"],

        // Task 201 binds a vertical slice; task 203 extends this list to every public header.
        Headers =
        [
            new("SDL3/SDL_init.h", "Init"),
            new("SDL3/SDL_version.h", "Version"),
            new("SDL3/SDL_error.h", "Error"),
            new("SDL3/SDL_video.h", "Video", @"^SDL_(?!GL_|EGL_)\w*Window"),
            new("SDL3/SDL_events.h", "Events", @"^SDL_(PumpEvents|PollEvent|WaitEvent|WaitEventTimeout|PushEvent|GetWindowFromEvent)$"),
        ],

        // SDL uses C99 bool, one byte on every target. Raw signatures keep it a byte: a 1-byte wrapper struct would
        // make the ABI depend on how each platform passes small structs, which is not verified here.
        Bool = PrimitiveType.Byte,

        Exclusions = new Dictionary<string, string>
        {
            ["SDL_SetError"] = "variadic (printf-style); P/Invoke cannot call variadic functions portably",
            ["SDL_SetErrorV"] = "takes a va_list, whose type differs on every ABI",
            ["SDL_WINDOW_SURFACE_VSYNC_DISABLED"] = "an SDL_SetWindowSurfaceVSync value that shares the SDL_WINDOW_ prefix, not a window flag",
            ["SDL_WINDOW_SURFACE_VSYNC_ADAPTIVE"] = "an SDL_SetWindowSurfaceVSync value that shares the SDL_WINDOW_ prefix, not a window flag",
        },

        Handles = ["SDL_Window"],
        OpaqueStructs = ["SDL_DisplayModeData"],

        FlagMacros = new Dictionary<string, string>
        {
            ["SDL_InitFlags"] = "SDL_INIT_",
            ["SDL_SurfaceFlags"] = "SDL_SURFACE_",
            ["SDL_WindowFlags"] = "SDL_WINDOW_",
        },

        Words = new Dictionary<string, string>
        {
            ["OPENGL"] = "OpenGL",
            ["TOPLEFT"] = "TopLeft",
            ["TOPRIGHT"] = "TopRight",
            ["BOTTOMLEFT"] = "BottomLeft",
            ["BOTTOMRIGHT"] = "BottomRight",
        },

        // Directions come from each function's documentation ("filled in with", "may be NULL", ...). Nullable
        // pointers keep their raw overload for passing NULL.
        Parameters = new Dictionary<string, ParameterRule>
        {
            ["SDL_GetWindows.count"] = ParameterRule.Out,
            ["SDL_GetWindowICCProfile.size"] = ParameterRule.Out,
            ["SDL_SetWindowFullscreenMode.mode"] = ParameterRule.In,
            ["SDL_GetWindowPosition.x"] = ParameterRule.Out,
            ["SDL_GetWindowPosition.y"] = ParameterRule.Out,
            ["SDL_GetWindowSize.w"] = ParameterRule.Out,
            ["SDL_GetWindowSize.h"] = ParameterRule.Out,
            ["SDL_GetWindowSafeArea.rect"] = ParameterRule.Out,
            ["SDL_GetWindowAspectRatio.min_aspect"] = ParameterRule.Out,
            ["SDL_GetWindowAspectRatio.max_aspect"] = ParameterRule.Out,
            ["SDL_GetWindowBordersSize.top"] = ParameterRule.Out,
            ["SDL_GetWindowBordersSize.left"] = ParameterRule.Out,
            ["SDL_GetWindowBordersSize.bottom"] = ParameterRule.Out,
            ["SDL_GetWindowBordersSize.right"] = ParameterRule.Out,
            ["SDL_GetWindowSizeInPixels.w"] = ParameterRule.Out,
            ["SDL_GetWindowSizeInPixels.h"] = ParameterRule.Out,
            ["SDL_GetWindowMinimumSize.w"] = ParameterRule.Out,
            ["SDL_GetWindowMinimumSize.h"] = ParameterRule.Out,
            ["SDL_GetWindowMaximumSize.w"] = ParameterRule.Out,
            ["SDL_GetWindowMaximumSize.h"] = ParameterRule.Out,
            ["SDL_GetWindowSurfaceVSync.vsync"] = ParameterRule.Out,
            ["SDL_UpdateWindowSurfaceRects.rects"] = ParameterRule.Span("numrects"),
            ["SDL_SetWindowMouseRect.rect"] = ParameterRule.In,
            ["SDL_PollEvent.event"] = ParameterRule.Out,
            ["SDL_WaitEvent.event"] = ParameterRule.Out,
            ["SDL_WaitEventTimeout.event"] = ParameterRule.Out,
            ["SDL_PushEvent.event"] = ParameterRule.Ref,
            ["SDL_GetWindowFromEvent.event"] = ParameterRule.In,
        },

        // The slice only polls quit, display and window events; 203 binds every member. SDL_Event keeps its
        // native 128-byte size through its padding member.
        UnionMembers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["SDL_Event"] = ["type", "common", "display", "window", "quit", "padding"],
        },
    };
}
