-- SDL3 as a static library for jade_native. Only Linux is supported so far; tasks 103, 104 and
-- 105 add the other platforms with their system libraries and frameworks.
local commits = {
    ["3.4.16"] = "fa2c02bb6e21974a89ea9824bc53c9932abe5f9c"
}

package("sdl3")
    set_homepage("https://www.libsdl.org/")
    set_description("Simple DirectMedia Layer: windows, input and platform services.")
    set_license("Zlib")

    -- The release archive rather than GitHub's generated tarball: it is signed upstream and its
    -- bytes do not depend on GitHub's archiver.
    add_urls("https://github.com/libsdl-org/SDL/releases/download/release-$(version)/SDL3-$(version).tar.gz")
    add_versions("3.4.16", "7322236cd12090c3eb40b9728be4d49c76f66ad17d04369584d4ecad5cf77c68")

    -- The build hash ignores recipe scripts, so editing one would silently reuse the old install.
    add_configs("recipe", {description = "Hash of this recipe and of the staging module.", type = "string", readonly = true,
        default = hash.sha256(path.join(os.scriptdir(), "xmake.lua")) .. hash.sha256(path.join(os.scriptdir(), "../../../modules/stage.lua"))})

    on_load(function (package)
        if package:is_plat("linux") then
            package:add("syslinks", "m", "dl", "pthread", "rt")
        end
    end)

    on_install("linux", function (package)
        local configs = {
            "-DCMAKE_BUILD_TYPE=" .. (package:is_debug() and "Debug" or "Release"),
            "-DSDL_SHARED=OFF",
            "-DSDL_STATIC=ON",
            "-DSDL_TEST_LIBRARY=OFF",
            "-DSDL_TESTS=OFF",
            "-DSDL_EXAMPLES=OFF",
            -- Covered by other Jade libraries: miniaudio for audio, Dawn for the GPU and 2D rendering.
            "-DSDL_AUDIO=OFF",
            "-DSDL_GPU=OFF",
            "-DSDL_RENDER=OFF",
            -- Already the default: optional system libraries (X11, Wayland, udev, D-Bus, ...) are
            -- loaded at runtime, so jade_native never links against them.
            "-DSDL_DEPS_SHARED=ON"
        }
        import("package.tools.cmake").install(package, configs)

        -- SDL's own shared library exports exactly the names in this version script. A `SDL_*`
        -- pattern would not do: the static build leaves internal SDL_* functions visible.
        -- JNI_OnLoad is only defined on Android.
        local exports = {}
        for name in io.readfile("src/dynapi/SDL_dynapi.sym"):gmatch("\n%s+([%w_]+);") do
            if name ~= "JNI_OnLoad" or package:is_plat("android") then
                table.insert(exports, name)
            end
        end

        import("stage", {rootdir = path.join(package:scriptdir(), "../../../modules")})(package, {
            commit = commits[package:version_str()],
            exports = exports,
            -- The SDL3_test library is not built, and the OpenGL and EGL headers are Khronos
            -- definitions rather than SDL API.
            headers = {"SDL3/*.h|SDL_test*.h|SDL_opengl*.h|SDL_egl.h"},
            licenses = {
                ["LICENSE.txt"] = "LICENSE.txt",
                ["src/hidapi/LICENSE-bsd.txt"] = "hidapi-LICENSE-bsd.txt",
                ["src/video/yuv2rgb/LICENSE"] = "yuv2rgb-LICENSE.txt"
            }
        })
    end)

    on_test(function (package)
        assert(package:has_cfuncs("SDL_GetVersion", {includes = "SDL3/SDL.h"}))
    end)
