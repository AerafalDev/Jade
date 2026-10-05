-- miniaudio and the Jade shim, built as one shared library.

target("miniaudio")
    set_kind("shared")
    -- The same optimization and assertion settings as the CMake Release builds of Dawn and SDL3.
    set_optimize("fastest")
    add_defines("NDEBUG")
    -- Only the declarations marked MA_API in config.h are exported.
    set_symbols("hidden")
    set_warnings("all", "error")
    add_forceincludes(path.join(os.scriptdir(), "config.h"))
    add_includedirs(get_config("miniaudio_source"))
    add_files(path.join(get_config("miniaudio_source"), "miniaudio.c"), "jade_miniaudio.c")

    if is_plat("linux") then
        add_syslinks("m", "dl", "pthread")
        -- xmake links shared libraries with the C++ driver, which would make this C library
        -- depend on libstdc++.
        add_shflags("-Wl,--as-needed", {force = true})
    end

    on_install(function (target)
        os.mkdir(target:installdir())
        os.cp(target:targetfile(), target:installdir())
    end)
target_end()
