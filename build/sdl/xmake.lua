-- SDL3, built through its CMake build: a shared library, or a static archive on iOS and in the
-- browser.

-- SDL's CMake build silently drops a feature whose development files or SDK headers are missing,
-- so the same definition would give different libraries on different machines. The build fails
-- instead when one of these defines is missing from the generated SDL_build_config.h. Each list
-- holds the features of its platform that depend on such a detection; the others are always
-- built. The *_DYNAMIC defines mean that the library is loaded at runtime rather than linked, so
-- that a machine without it can still load SDL3.
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
    windows = {
        "SDL_VIDEO_RENDER_D3D",
        "SDL_VIDEO_RENDER_D3D11",
        "SDL_VIDEO_RENDER_D3D12",
        "SDL_GPU_D3D12",
        "HAVE_DXGI1_6_H",
        "HAVE_SHELLSCALINGAPI_H",
        "HAVE_ROAPI_H",
        "HAVE_TPCSHRD_H",
        "SDL_JOYSTICK_DINPUT",
        "SDL_JOYSTICK_XINPUT",
        "SDL_JOYSTICK_WGI",
        "SDL_JOYSTICK_GAMEINPUT",
        "SDL_HAPTIC_DINPUT",
        "SDL_SENSOR_WINDOWS",
        "SDL_CAMERA_DRIVER_MEDIAFOUNDATION",
    },
    macosx = {
        "SDL_VIDEO_DRIVER_COCOA",
        "SDL_VIDEO_METAL",
        "SDL_VIDEO_RENDER_METAL",
        "SDL_VIDEO_VULKAN",
        "SDL_GPU_METAL",
        "SDL_JOYSTICK_MFI",
        "SDL_CAMERA_DRIVER_COREMEDIA",
    },
    iphoneos = {
        "SDL_VIDEO_DRIVER_UIKIT",
        "SDL_VIDEO_METAL",
        "SDL_VIDEO_RENDER_METAL",
        "SDL_VIDEO_VULKAN",
        "SDL_GPU_METAL",
        "SDL_JOYSTICK_MFI",
        "SDL_SENSOR_COREMOTION",
        "SDL_CAMERA_DRIVER_COREMEDIA",
    },
    android = {
        "SDL_VIDEO_DRIVER_ANDROID",
        "SDL_VIDEO_OPENGL_EGL",
        "SDL_VIDEO_VULKAN",
        "SDL_GPU_VULKAN",
        "SDL_JOYSTICK_ANDROID",
        "SDL_HAPTIC_ANDROID",
        "SDL_SENSOR_ANDROID",
        "SDL_CAMERA_DRIVER_ANDROID",
    },
    wasm = {
        "SDL_VIDEO_DRIVER_EMSCRIPTEN",
        "SDL_JOYSTICK_EMSCRIPTEN",
        "SDL_CAMERA_DRIVER_EMSCRIPTEN",
    },
}

package("sdl")
    set_sourcedir(get_config("sdl_source"))
    add_configs("key", {description = "The build key given by scripts/build-native.cs.", default = "", type = "string"})

    on_install(function (package)
        import("package.tools.cmake")

        local static = package:is_plat("iphoneos", "wasm")
        local configs = {
            "-DSDL_SHARED=" .. (static and "OFF" or "ON"),
            "-DSDL_STATIC=" .. (static and "ON" or "OFF"),
            "-DSDL_TEST_LIBRARY=OFF",
            "-DSDL_TESTS=OFF",
            "-DSDL_EXAMPLES=OFF",
            "-DSDL_INSTALL_DOCS=OFF",
            -- Jade's audio goes through miniaudio and SDL's audio subsystem is never initialized
            -- (docs/adr/0005).
            "-DSDL_AUDIO=OFF",
        }
        if package:is_plat("macosx", "iphoneos") then
            table.insert(configs, "-DSDL_FRAMEWORK=OFF")
            table.insert(configs, "-DCMAKE_OSX_DEPLOYMENT_TARGET=" .. get_config("target_minver"))
        end
        if package:is_plat("iphoneos") and get_config("appledev") == "simulator" then
            -- xmake only selects the simulator SDK for x86_64; arm64 is both a device and a
            -- simulator architecture.
            table.insert(configs, "-DCMAKE_OSX_SYSROOT=iphonesimulator")
        end
        if package:is_plat("android") then
            -- SDL's Java sources ship with the Android sample, not as a jar built here.
            table.insert(configs, "-DSDL_ANDROID_JAR=OFF")
        end
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
            raise("SDL3 was configured without %s: a development package or SDK component is missing (see CONTRIBUTING.md).", table.concat(missing, ", "))
        end
    end)
package_end()

add_requires("sdl", {system = false, configs = jade_package_configs("sdl")})

target("SDL3")
    set_kind("phony")
    add_packages("sdl")

    on_install(function (target)
        local libdir = path.join(target:pkg("sdl"):installdir(), target:is_plat("windows") and "bin" or "lib")
        local files = {
            linux = "libSDL3.so",
            android = "libSDL3.so",
            windows = "SDL3.dll",
            macosx = "libSDL3.dylib",
            iphoneos = "libSDL3.a",
        }
        os.mkdir(target:installdir())
        if target:is_plat("wasm") then
            -- The archive is named after the module the managed side imports (docs/adr/0025).
            os.cp(path.join(libdir, "libSDL3.a"), path.join(target:installdir(), "SDL3.a"))
        else
            -- A shared library is installed as a chain of symbolic links; the package ships the
            -- file itself under the name the managed side loads.
            os.cp(path.join(libdir, files[target:plat()]), target:installdir(), {symlink = false})
        end
    end)
target_end()
