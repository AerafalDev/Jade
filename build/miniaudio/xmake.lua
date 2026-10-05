-- miniaudio and the Jade shim, built as one library: shared, or a static archive on iOS and in
-- the browser.

target("miniaudio")
    set_kind(jade_library_kind())
    -- The same optimization and assertion settings as the CMake Release builds of Dawn and SDL3.
    set_optimize("fastest")
    add_defines("NDEBUG")
    -- Only the declarations marked MA_API in config.h are exported.
    set_symbols("hidden")
    set_warnings("all", "error")
    add_forceincludes(path.join(os.scriptdir(), "config.h"))
    add_includedirs(get_config("miniaudio_source"))
    add_files("jade_miniaudio.c")

    if is_plat("iphoneos") then
        -- miniaudio's iOS backend uses AVAudioSession (miniaudio.h, section 2.2).
        add_files(path.join(get_config("miniaudio_source"), "miniaudio.c"), {sourcekind = "mm"})
    else
        add_files(path.join(get_config("miniaudio_source"), "miniaudio.c"))
    end

    if is_plat("linux", "android") then
        -- Bionic has the pthread functions in libc.
        add_syslinks("m", "dl", is_plat("linux") and "pthread" or nil)
        -- xmake links shared libraries with the C++ driver, which would make this C library
        -- depend on the C++ runtime.
        add_shflags("-Wl,--as-needed", {force = true})
    elseif is_plat("windows") then
        add_defines("_WIN32_WINNT=" .. get_config("win32_winnt"), "WINVER=" .. get_config("win32_winnt"))
    elseif is_plat("macosx") then
        -- MA_NO_RUNTIME_LINKING (config.h) links the audio frameworks instead of loading them.
        add_frameworks("CoreFoundation", "CoreAudio", "AudioToolbox")
    end

    on_install(function (target)
        os.mkdir(target:installdir())
        if target:is_plat("wasm") then
            -- The archive is named after the module the managed side imports (docs/adr/0025).
            os.cp(target:targetfile(), path.join(target:installdir(), "miniaudio.a"))
        else
            os.cp(target:targetfile(), target:installdir())
        end
    end)
target_end()
