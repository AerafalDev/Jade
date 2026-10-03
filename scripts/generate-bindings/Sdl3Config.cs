/// <summary>The SDL3 bindings: <c>Jade.Interop.Sdl3</c>, functions and constants in <c>Sdl</c>.</summary>
internal static class Sdl3Config
{
    // SDL has no non-variadic sibling for these two: formatting belongs to the caller, as in .NET.
    private const string Variadic = "variadic (printf-style); P/Invoke cannot call variadic functions portably";
    private const string VaList = "takes a va_list, whose type differs on every ABI";

    private const string PrintfFormat = "a printf length modifier of the target's C library, which differs per target";
    private const string Gdk = "only declared for Microsoft GDK builds (SDL_PLATFORM_GDK), which no ADR-0007 target is";
    private const string CallerLocation = "the location of the C code expanding it (__FILE__, __LINE__, __func__)";

    /// <summary>Builds the config.</summary>
    /// <returns>The SDL3 config.</returns>
    public static LibraryConfig Create() => new()
    {
        Name = "Sdl3",
        Upstream = "sdl3",
        IncludeDirectory = "SDL3",
        FunctionsClass = "Sdl",
        Prefixes = ["SDL_"],
        ExportPrefixes = ["SDL_"],

        Headers =
        [
            new("SDL3/SDL_assert.h", "Assert"),
            new("SDL3/SDL_asyncio.h", "AsyncIO"),
            new("SDL3/SDL_atomic.h", "Atomic"),
            new("SDL3/SDL_blendmode.h", "BlendMode"),
            new("SDL3/SDL_camera.h", "Camera"),
            new("SDL3/SDL_clipboard.h", "Clipboard"),
            new("SDL3/SDL_cpuinfo.h", "CpuInfo"),
            new("SDL3/SDL_dialog.h", "Dialog"),
            new("SDL3/SDL_error.h", "Error"),
            new("SDL3/SDL_events.h", "Events"),
            new("SDL3/SDL_filesystem.h", "Filesystem"),
            new("SDL3/SDL_gamepad.h", "Gamepad"),
            new("SDL3/SDL_guid.h", "Guid"),
            new("SDL3/SDL_haptic.h", "Haptic"),
            new("SDL3/SDL_hidapi.h", "HidApi"),
            new("SDL3/SDL_hints.h", "Hints"),
            new("SDL3/SDL_init.h", "Init"),
            new("SDL3/SDL_iostream.h", "IOStream"),
            new("SDL3/SDL_joystick.h", "Joystick"),
            new("SDL3/SDL_keyboard.h", "Keyboard"),
            new("SDL3/SDL_keycode.h", "Keycode"),
            new("SDL3/SDL_loadso.h", "LoadSo"),
            new("SDL3/SDL_locale.h", "Locale"),
            new("SDL3/SDL_log.h", "Log"),
            new("SDL3/SDL_messagebox.h", "MessageBox"),
            new("SDL3/SDL_metal.h", "Metal"),
            new("SDL3/SDL_misc.h", "Misc"),
            new("SDL3/SDL_mouse.h", "Mouse"),
            new("SDL3/SDL_mutex.h", "Mutex"),
            new("SDL3/SDL_pen.h", "Pen"),
            new("SDL3/SDL_pixels.h", "Pixels"),
            new("SDL3/SDL_platform.h", "Platform"),
            new("SDL3/SDL_power.h", "Power"),
            new("SDL3/SDL_process.h", "Process"),
            new("SDL3/SDL_properties.h", "Properties"),
            new("SDL3/SDL_rect.h", "Rect"),
            new("SDL3/SDL_scancode.h", "Scancode"),
            new("SDL3/SDL_sensor.h", "Sensor"),
            new("SDL3/SDL_stdinc.h", "Stdinc"),
            new("SDL3/SDL_storage.h", "Storage"),
            new("SDL3/SDL_surface.h", "Surface"),
            new("SDL3/SDL_system.h", "System"),
            new("SDL3/SDL_thread.h", "Thread"),
            new("SDL3/SDL_time.h", "Time"),
            new("SDL3/SDL_timer.h", "Timer"),
            new("SDL3/SDL_touch.h", "Touch"),
            new("SDL3/SDL_tray.h", "Tray"),
            new("SDL3/SDL_version.h", "Version"),
            new("SDL3/SDL_video.h", "Video"),
            new("SDL3/SDL_vulkan.h", "Vulkan"),
        ],

        ExcludedHeaders = new Dictionary<string, string>
        {
            ["SDL3/SDL.h"] = "the umbrella header, which only includes the others",
            ["SDL3/SDL_audio.h"] = "audio subsystem built out of jade_native (101: SDL_AUDIO=OFF, miniaudio covers audio), so these functions fail at runtime",
            ["SDL3/SDL_gpu.h"] = "GPU subsystem built out of jade_native (101: SDL_GPU=OFF, Dawn covers the GPU), so these functions fail at runtime",
            ["SDL3/SDL_render.h"] = "2D renderer built out of jade_native (101: SDL_RENDER=OFF, Dawn covers rendering), so these functions fail at runtime",
            ["SDL3/SDL_main.h"] = "platform main and app lifecycle glue, out of scope until the 104 ADRs",
            ["SDL3/SDL_main_impl.h"] = "defines the C main() of SDL_main.h; platform main glue (104)",
            ["SDL3/SDL_begin_code.h"] = "compiler and export macros of SDL's own headers",
            ["SDL3/SDL_close_code.h"] = "compiler and export macros of SDL's own headers",
            ["SDL3/SDL_platform_defines.h"] = "SDL_PLATFORM_* detection macros, only meaningful to a C compiler",
            ["SDL3/SDL_intrin.h"] = "compiler intrinsics detection macros",
            ["SDL3/SDL_dlopennote.h"] = "macros that embed dlopen notes into ELF binaries",
            ["SDL3/SDL_endian.h"] = "byte-order macros and inline swaps, nothing exported; BinaryPrimitives covers them",
            ["SDL3/SDL_bits.h"] = "inline bit functions, nothing exported; BitOperations covers them",
            ["SDL3/SDL_copying.h"] = "the license text",
            ["SDL3/SDL_oldnames.h"] = "SDL2 names, only defined for C code built with SDL_ENABLE_OLD_NAMES",
            ["SDL3/SDL_revision.h"] = "only SDL_REVISION, the headers' revision; SDL_GetRevision returns the library's",
        },

        InferParameters = true,

        Exclusions = new Dictionary<string, string>
        {
            ["__debugbreak"] = "the MSVC intrinsic that SDL_assert.h declares for SDL_TriggerBreakpoint, not SDL API",
            ["SDL_ASSERT_FILE"] = CallerLocation,
            ["SDL_ASSERT_LEVEL"] = "depends on how the including C code is compiled (NDEBUG, compiler)",
            ["SDL_DUMMY_ENUM"] = "only checks the size of C enums at compile time",
            ["SDL_DYNAPI_entry"] = "the entry point of SDL's dynamic API loader, declared in no public header",
            ["SDL_FILE"] = CallerLocation,
            ["SDL_FUNCTION"] = CallerLocation,
            ["SDL_LINE"] = CallerLocation,
            ["SDL_NULL_WHILE_LOOP_CONDITION"] = "a C idiom for do-while(0) macros",
            ["SDL_PRILL_PREFIX"] = PrintfFormat,
            ["SDL_PRILLd"] = PrintfFormat,
            ["SDL_PRILLu"] = PrintfFormat,
            ["SDL_PRILLx"] = PrintfFormat,
            ["SDL_PRILLX"] = PrintfFormat,
            ["SDL_PRIs32"] = PrintfFormat,
            ["SDL_PRIs64"] = PrintfFormat,
            ["SDL_PRIu32"] = PrintfFormat,
            ["SDL_PRIu64"] = PrintfFormat,
            ["SDL_PRIx32"] = PrintfFormat,
            ["SDL_PRIX32"] = PrintfFormat,
            ["SDL_PRIx64"] = PrintfFormat,
            ["SDL_PRIX64"] = PrintfFormat,
            ["SDL_SIZE_MAX"] = "the maximum of size_t, whose width depends on the target: nuint.MaxValue",
            ["SDL_GDKResumeGPU"] = Gdk,
            ["SDL_GDKSuspendGPU"] = Gdk,
            ["SDL_GetGDKDefaultUser"] = Gdk,
            ["SDL_GetGDKTaskQueue"] = Gdk,
            ["SDL_ICONV_E2BIG"] = "(size_t)-2, whose width depends on the target; compare with nuint.MaxValue - 1",
            ["SDL_ICONV_EILSEQ"] = "(size_t)-3, whose width depends on the target; compare with nuint.MaxValue - 2",
            ["SDL_ICONV_EINVAL"] = "(size_t)-4, whose width depends on the target; compare with nuint.MaxValue - 3",
            ["SDL_ICONV_ERROR"] = "(size_t)-1, whose width depends on the target; compare with nuint.MaxValue",
            ["SDL_asprintf"] = Variadic,
            ["SDL_IOprintf"] = Variadic,
            ["SDL_IOvprintf"] = VaList,
            ["SDL_Log"] = Variadic,
            ["SDL_LogCritical"] = Variadic,
            ["SDL_LogDebug"] = Variadic,
            ["SDL_LogError"] = Variadic,
            ["SDL_LogInfo"] = Variadic,
            ["SDL_LogMessage"] = Variadic,
            ["SDL_LogMessageV"] = VaList,
            ["SDL_LogTrace"] = Variadic,
            ["SDL_LogVerbose"] = Variadic,
            ["SDL_LogWarn"] = Variadic,
            ["SDL_SetError"] = Variadic,
            ["SDL_SetErrorV"] = VaList,
            ["SDL_snprintf"] = Variadic,
            ["SDL_sscanf"] = Variadic,
            ["SDL_swprintf"] = Variadic,
            ["SDL_vasprintf"] = VaList,
            ["SDL_vsnprintf"] = VaList,
            ["SDL_vsscanf"] = VaList,
            ["SDL_vswprintf"] = VaList,
        },

        Handles =
        [
            "SDL_AsyncIO", "SDL_AsyncIOQueue", "SDL_Camera", "SDL_Condition", "SDL_Cursor", "SDL_Environment", "SDL_Gamepad",
            "SDL_GLContextState", "SDL_Haptic", "SDL_hid_device", "SDL_iconv_data_t", "SDL_IOStream", "SDL_Joystick", "SDL_Mutex",
            "SDL_Process", "SDL_RWLock", "SDL_Semaphore", "SDL_Sensor", "SDL_SharedObject", "SDL_Storage", "SDL_Thread", "SDL_Tray",
            "SDL_TrayEntry", "SDL_TrayMenu", "SDL_Window",
        ],

        MethodStems = new Dictionary<string, string>
        {
            ["SDL_hid_device"] = "Hid",
            ["SDL_IOStream"] = "IO",
            ["SDL_SharedObject"] = "Object",
        },

        // Vulkan objects that SDL only passes through: no SDL function belongs to them.
        ForeignHandles = ["VkInstance_T", "VkPhysicalDevice_T"],

        OpaqueStructs = ["SDL_DisplayModeData", "VkAllocationCallbacks", "tagMSG", "_XEvent"],

        IdTypedefs =
        [
            "SDL_AudioDeviceID", "SDL_CameraID", "SDL_DisplayID", "SDL_FingerID", "SDL_HapticEffectID", "SDL_HapticID", "SDL_JoystickID",
            "SDL_KeyboardID", "SDL_MouseID", "SDL_PenID", "SDL_PropertiesID", "SDL_SensorID", "SDL_ThreadID", "SDL_TimerID", "SDL_TouchID",
            "SDL_WindowID",
        ],

        // A non-dispatchable Vulkan handle is a pointer on 64-bit targets and a uint64_t on 32-bit ones (wasm32): always
        // 64 bits, passed like a 64-bit integer by every 64-bit ABI.
        TypedefMappings = new Dictionary<string, PrimitiveType>
        {
            ["VkSurfaceKHR"] = PrimitiveType.UInt64,
        },

        MacroEnums = new Dictionary<string, MacroEnum>
        {
            ["SDL_BlendMode"] = MacroEnum.Values("SDL_BLENDMODE_"),
            ["SDL_GLContextFlag"] = MacroEnum.Flags("SDL_GL_CONTEXT_", "_FLAG$"),
            ["SDL_GLContextReleaseFlag"] = MacroEnum.Values("SDL_GL_CONTEXT_RELEASE_BEHAVIOR_"),
            ["SDL_GLContextResetNotification"] = MacroEnum.Values("SDL_GL_CONTEXT_RESET_", "^SDL_GL_CONTEXT_RESET_(?!ISOLATION_FLAG$)"),
            ["SDL_GLProfile"] = MacroEnum.Values("SDL_GL_CONTEXT_PROFILE_"),
            ["SDL_GlobFlags"] = MacroEnum.Flags("SDL_GLOB_"),
            ["SDL_HapticDirectionType"] = MacroEnum.Values("SDL_HAPTIC_", "^SDL_HAPTIC_(POLAR|CARTESIAN|SPHERICAL|STEERING_AXIS)$"),
            ["SDL_HapticEffectType"] = MacroEnum.Values("SDL_HAPTIC_", "^SDL_HAPTIC_(CONSTANT|SINE|SQUARE|TRIANGLE|SAWTOOTHUP|SAWTOOTHDOWN|RAMP|SPRING|DAMPER|INERTIA|FRICTION|LEFTRIGHT|RESERVED[0-9]|CUSTOM)$"),
            ["SDL_InitFlags"] = MacroEnum.Flags("SDL_INIT_"),
            ["SDL_Keycode"] = MacroEnum.Values("SDLK_"),
            ["SDL_Keymod"] = MacroEnum.Flags("SDL_KMOD_"),
            ["SDL_MessageBoxButtonFlags"] = MacroEnum.Flags("SDL_MESSAGEBOX_BUTTON_"),
            ["SDL_MessageBoxFlags"] = MacroEnum.Flags("SDL_MESSAGEBOX_", "^SDL_MESSAGEBOX_(?!BUTTON_)"),
            ["SDL_MouseButtonFlags"] = MacroEnum.Flags("SDL_BUTTON_", "MASK$"),
            ["SDL_PenInputFlags"] = MacroEnum.Flags("SDL_PEN_INPUT_"),
            ["SDL_SurfaceFlags"] = MacroEnum.Flags("SDL_SURFACE_"),
            ["SDL_TrayEntryFlags"] = MacroEnum.Flags("SDL_TRAYENTRY_"),

            // SDL_SetWindowSurfaceVSync values share the prefix; they stay constants.
            ["SDL_WindowFlags"] = MacroEnum.Flags("SDL_WINDOW_", "^SDL_WINDOW_(?!SURFACE_VSYNC_)"),
        },

        // Declared on every target but documented for one platform; functions only some targets declare get their
        // platforms from the headers instead (PlatformMerge).
        SupportedPlatforms = new Dictionary<string, IReadOnlyList<string>>
        {
            ["SDL_Metal_CreateView"] = ["ios", "macos"],
            ["SDL_Metal_DestroyView"] = ["ios", "macos"],
            ["SDL_Metal_GetLayer"] = ["ios", "macos"],
            ["SDL_SetWindowFillDocument"] = ["browser"],
            ["SDL_SetX11EventHook"] = ["linux"],
        },

        Renames = new Dictionary<string, string>
        {
            // Types named like a type of the SDK's implicit global usings (System, System.IO, System.Threading, ...) would
            // be ambiguous in user code that imports both namespaces: they keep an Sdl prefix.
            ["SDL_DateTime"] = "SdlDateTime",
            ["SDL_Environment"] = "SdlEnvironment",
            ["SDL_GUID"] = "SdlGuid",
            ["SDL_Mutex"] = "SdlMutex",
            ["SDL_Semaphore"] = "SdlSemaphore",
            ["SDL_Thread"] = "SdlThread",
            ["SDL_ThreadPriority"] = "SdlThreadPriority",
            ["SDL_ThreadState"] = "SdlThreadState",

            // Struct tags named after the typedef users know.
            ["SDL_GLContextState"] = "GLContext",
            ["SDL_iconv_data_t"] = "IconvState",
            ["tagMSG"] = "Msg",
            ["VkInstance_T"] = "VkInstance",
            ["VkPhysicalDevice_T"] = "VkPhysicalDevice",

            // The mask of each button, named after the button.
            ["SDL_MouseButtonFlags.SDL_BUTTON_LMASK"] = "Left",
            ["SDL_MouseButtonFlags.SDL_BUTTON_MMASK"] = "Middle",
            ["SDL_MouseButtonFlags.SDL_BUTTON_RMASK"] = "Right",
            ["SDL_MouseButtonFlags.SDL_BUTTON_X1MASK"] = "X1",
            ["SDL_MouseButtonFlags.SDL_BUTTON_X2MASK"] = "X2",
        },

        Words = new Dictionary<string, string>
        {
            ["BOTTOMLEFT"] = "BottomLeft",
            ["BOTTOMRIGHT"] = "BottomRight",
            ["IO"] = "IO",
            ["IOSTREAM"] = "IOStream",
            ["MOUSEID"] = "MouseID",
            ["OPENGL"] = "OpenGL",
            ["TOPLEFT"] = "TopLeft",
            ["TOPRIGHT"] = "TopRight",
            ["TOUCHID"] = "TouchID",
            ["VSYNC"] = "VSync",
            ["WINDOWPOS"] = "WindowPos",
        },

        Parameters = new Dictionary<string, ParameterRule>
        {
            // A friendly overload only lends memory for the duration of the call, so a pointer SDL returns or keeps stays
            // raw (checked in SDL's sources).
            ["SDL_GetStringProperty.default_value"] = ParameterRule.Raw,
            ["SDL_SetScancodeName.name"] = ParameterRule.Raw,
            ["SDL_ShowOpenFileDialog.filters"] = ParameterRule.Raw,
            ["SDL_ShowSaveFileDialog.filters"] = ParameterRule.Raw,
            ["SDL_strcasestr.haystack"] = ParameterRule.Raw,
            ["SDL_strchr.str"] = ParameterRule.Raw,
            ["SDL_strnstr.haystack"] = ParameterRule.Raw,
            ["SDL_strpbrk.str"] = ParameterRule.Raw,
            ["SDL_strrchr.str"] = ParameterRule.Raw,
            ["SDL_strstr.haystack"] = ParameterRule.Raw,

            // Directions the parameter docs do not state plainly enough to infer (ParameterInference).
            ["SDL_GetAtomicInt.a"] = ParameterRule.Ref,
            ["SDL_GetAtomicU32.a"] = ParameterRule.Ref,
            ["SDL_GetMasksForPixelFormat.bpp"] = ParameterRule.Out,
            ["SDL_GetRectAndLineIntersection.X1"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersection.X2"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersection.Y1"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersection.Y2"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersectionFloat.X1"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersectionFloat.X2"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersectionFloat.Y1"] = ParameterRule.Ref,
            ["SDL_GetRectAndLineIntersectionFloat.Y2"] = ParameterRule.Ref,
            ["SDL_GetScancodeFromKey.modstate"] = ParameterRule.Out,
            ["SDL_GetTextInputArea.cursor"] = ParameterRule.Out,
            ["SDL_GetTLS.id"] = ParameterRule.Ref,
            ["SDL_GetWindowICCProfile.size"] = ParameterRule.Out,
            ["SDL_GL_GetSwapInterval.interval"] = ParameterRule.Out,
            ["SDL_LockSpinlock.lock"] = ParameterRule.Ref,
            ["SDL_PushEvent.event"] = ParameterRule.Ref,
            ["SDL_rand_bits_r.state"] = ParameterRule.Ref,
            ["SDL_rand_r.state"] = ParameterRule.Ref,
            ["SDL_randf_r.state"] = ParameterRule.Ref,
            ["SDL_SetInitialized.state"] = ParameterRule.Ref,
            ["SDL_SetTLS.id"] = ParameterRule.Ref,
            ["SDL_ShouldInit.state"] = ParameterRule.Ref,
            ["SDL_ShouldQuit.state"] = ParameterRule.Ref,
            ["SDL_ShowMessageBox.buttonid"] = ParameterRule.Out,
            ["SDL_TryLockSpinlock.lock"] = ParameterRule.Ref,
            ["SDL_UnlockSpinlock.lock"] = ParameterRule.Ref,
        },
    };
}
