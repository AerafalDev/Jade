-- The layout libraries of the C# layout tests (docs/adr/0036): one per interop library, compiled
-- from the source the binding generator writes here, with the headers the generator parsed.
--
-- They are test-only and never shipped: they are not default targets, so `xmake build` and
-- `xmake install` leave them out, and scripts/build-native.cs builds and installs the "layout"
-- group on its own, into artifacts/native/test/<rid>/.

local function layout_library(name)
    target("jade_" .. name .. "_layout")
        set_kind("shared")
        set_default(false)
        set_group("layout")
        -- The C standard the binding generator parses the headers with.
        set_languages("c17")
        set_warnings("all", "error")
        set_symbols("hidden")
        add_files(name .. ".g.c")

        if is_plat("linux") then
            -- xmake links shared libraries with the C++ driver, and a layout library calls nothing:
            -- it must not depend on libstdc++.
            add_shflags("-Wl,--as-needed", {force = true})
        end

        on_install(function (target)
            os.mkdir(target:installdir())
            os.cp(target:targetfile(), target:installdir())
        end)
end

-- Dawn generates webgpu.h during its build; the package installs it. Its library is not linked:
-- the extra config replaces the package's links.
layout_library("wgpu")
    add_packages("dawn", {links = {}})
target_end()

-- The headers of the pinned sources, which the binding generator parses.
layout_library("sdl")
    add_includedirs(path.join(get_config("sdl_source"), "include"))
target_end()

layout_library("miniaudio")
    add_includedirs(get_config("miniaudio_source"))
target_end()
