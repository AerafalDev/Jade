-- Dawn, built as its monolithic shared library (webgpu_dawn) through its CMake build.

package("dawn")
    set_sourcedir(get_config("dawn_source"))
    add_configs("key", {description = "The build key given by scripts/build-native.cs.", default = "", type = "string"})

    on_install("linux", function (package)
        import("package.tools.cmake")

        -- The third-party sources come from Dawn's DEPS file, fetched by scripts/build-native.cs
        -- at the pinned commits; Dawn's own fetch script clones test and tool dependencies too
        -- and ignores git failures.
        local configs = {
            "-DDAWN_FETCH_DEPENDENCIES=OFF",
            "-DDAWN_BUILD_MONOLITHIC_LIBRARY=SHARED",
            "-DBUILD_SHARED_LIBS=OFF",
            "-DDAWN_ENABLE_INSTALL=ON",
            "-DDAWN_BUILD_SAMPLES=OFF",
            "-DDAWN_BUILD_TESTS=OFF",
            "-DDAWN_BUILD_BENCHMARKS=OFF",
            "-DDAWN_BUILD_PROTOBUF=OFF",
            "-DDAWN_USE_GLFW=OFF",
            -- The C++20 module interface only serves C++ consumers, and Dawn's support check
            -- accepts GCC 13, whose modules CMake cannot scan.
            "-DDAWN_SUPPORTS_CXX_MODULES=OFF",
            "-DTINT_BUILD_CMD_TOOLS=OFF",
            "-DTINT_BUILD_TESTS=OFF",
            "-DTINT_BUILD_BENCHMARKS=OFF",
            "-DTINT_BUILD_FUZZERS=OFF",
        }
        cmake.install(package, configs)
    end)
package_end()

add_requires("dawn", {system = false, configs = {key = get_config("dawn_key")}})

target("webgpu_dawn")
    set_kind("phony")
    add_packages("dawn")

    on_install(function (target)
        os.mkdir(target:installdir())
        os.cp(path.join(target:pkg("dawn"):installdir(), "lib", "libwebgpu_dawn.so"), target:installdir())
    end)
target_end()
