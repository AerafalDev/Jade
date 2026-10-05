-- SDL3, built as a shared library through its CMake build.

-- SDL's CMake build silently drops a feature whose development files are missing, so the same
-- definition would give different libraries on different machines. The build fails instead when
-- one of these defines is missing from the generated SDL_build_config.h. The *_DYNAMIC defines
-- mean that the library is loaded at runtime rather than linked, so that a machine without it
-- can still load SDL3.
local required_features = {
    linux = {
        "SDL_VIDEO_DRIVER_X11",
        "SDL_VIDEO_DRIVER_X11_DYNAMIC",
        "SDL_VIDEO_DRIVER_X11_XCURSOR",
        "SDL_VIDEO_DRIVER_X11_XDBE",
        "SDL_VIDEO_DRIVER_X11_XFIXES",
        "SDL_VIDEO_DRIVER_X11_XINPUT2",
        "SDL_VIDEO_DRIVER_X11_XRANDR",
        "SDL_VIDEO_DRIVER_X11_XSCRNSAVER",
        "SDL_VIDEO_DRIVER_X11_XSHAPE",
        "SDL_VIDEO_DRIVER_X11_XSYNC",
        "SDL_VIDEO_DRIVER_X11_XTEST",
        "SDL_VIDEO_DRIVER_WAYLAND",
        "SDL_VIDEO_DRIVER_WAYLAND_DYNAMIC",
        "SDL_VIDEO_DRIVER_WAYLAND_DYNAMIC_LIBDECOR",
        "SDL_VIDEO_DRIVER_KMSDRM",
        "SDL_VIDEO_DRIVER_KMSDRM_DYNAMIC",
        "SDL_VIDEO_OPENGL_EGL",
        "SDL_VIDEO_OPENGL_GLX",
        "SDL_VIDEO_VULKAN",
        "HAVE_DBUS_DBUS_H",
        "HAVE_IBUS_IBUS_H",
        "HAVE_FCITX",
        "SDL_UDEV_DYNAMIC",
        "SDL_LIBUSB_DYNAMIC",
        "HAVE_LIBURING_H",
        "SDL_FRIBIDI_DYNAMIC",
        "SDL_LIBTHAI_DYNAMIC",
    },
}

package("sdl")
    set_sourcedir(get_config("sdl_source"))
    add_configs("key", {description = "The build key given by scripts/build-native.cs.", default = "", type = "string"})

    on_install("linux", function (package)
        import("package.tools.cmake")

        local configs = {
            "-DSDL_SHARED=ON",
            "-DSDL_STATIC=OFF",
            "-DSDL_TEST_LIBRARY=OFF",
            "-DSDL_TESTS=OFF",
            "-DSDL_EXAMPLES=OFF",
            "-DSDL_INSTALL_DOCS=OFF",
            -- Jade's audio goes through miniaudio and SDL's audio subsystem is never initialized
            -- (docs/adr/0005).
            "-DSDL_AUDIO=OFF",
        }
        cmake.install(package, configs)

        local header = path.join(package:builddir(), "include-config-release", "build_config", "SDL_build_config.h")
        local defined = {}
        for name in io.readfile(header):gmatch("\n#define ([%w_]+)") do
            defined[name] = true
        end
        local missing = {}
        for _, name in ipairs(required_features[package:plat()]) do
            if not defined[name] then
                table.insert(missing, name)
            end
        end
        if #missing > 0 then
            raise("SDL3 was configured without %s: a development package is missing (see CONTRIBUTING.md).", table.concat(missing, ", "))
        end
    end)
package_end()

add_requires("sdl", {system = false, configs = {key = get_config("sdl_key")}})

target("SDL3")
    set_kind("phony")
    add_packages("sdl")

    on_install(function (target)
        -- The library is installed as a chain of symbolic links; the package ships the file itself
        -- under the name the managed side loads.
        os.mkdir(target:installdir())
        os.cp(path.join(target:pkg("sdl"):installdir(), "lib", "libSDL3.so"), target:installdir(), {symlink = false})
    end)
target_end()
