-- Dawn, built as its monolithic library (webgpu_dawn) through its CMake build, and Emdawnwebgpu
-- from the same sources for the browser (docs/adr/0004, docs/adr/0025).

package("dawn")
    set_sourcedir(get_config("dawn_source"))
    add_configs("key", {description = "The build key given by scripts/build-native.cs.", default = "", type = "string"})

    on_install(function (package)
        import("package.tools.cmake")

        -- The third-party sources come from Dawn's DEPS file, fetched by scripts/build-native.cs
        -- at the pinned commits; Dawn's own fetch script clones test and tool dependencies too
        -- and ignores git failures.
        local configs = {
            "-DDAWN_FETCH_DEPENDENCIES=OFF",
            "-DBUILD_SHARED_LIBS=OFF",
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

        if package:is_plat("wasm") then
            -- Emdawnwebgpu implements webgpu.h on the browser's WebGPU: only its C++ part and
            -- the JavaScript it links with are built, not Dawn itself. Its CMake build has no
            -- install rules for them.
            table.insert(configs, "-DDAWN_ENABLE_INSTALL=OFF")
            cmake.build(package, configs, {target = "emdawnwebgpu_c"})
            local builddir = package:builddir()
            local generated = path.join(builddir, "gen", "src", "emdawnwebgpu")
            local sources = path.join(package:sourcedir(), "third_party", "emdawnwebgpu", "pkg", "webgpu", "src")
            os.cp(path.join(builddir, "src", "emdawnwebgpu", "libemdawnwebgpu_c.a"), package:installdir("lib"))
            for _, file in ipairs({
                path.join(generated, "library_webgpu_enum_tables.js"),
                path.join(generated, "library_webgpu_generated_sig_info.js"),
                path.join(generated, "library_webgpu_generated_struct_info.js"),
                path.join(sources, "library_webgpu.js"),
                path.join(sources, "webgpu-externs.js"),
            }) do
                os.cp(file, package:installdir("js"))
            end
            -- The layout library of the tests compiles against the header Emdawnwebgpu generates.
            os.cp(path.join(generated, "include"), package:installdir())
            return
        end

        table.insert(configs, "-DDAWN_ENABLE_INSTALL=ON")
        -- iOS applications link Dawn statically, from an xcframework (docs/adr/0011).
        table.insert(configs, "-DDAWN_BUILD_MONOLITHIC_LIBRARY=" .. (package:is_plat("iphoneos") and "STATIC" or "SHARED"))
        if package:is_plat("windows") then
            -- D3D12 compiles shaders with the DXC that Dawn builds and ships (docs/adr/0030).
            table.insert(configs, "-DDAWN_USE_BUILT_DXC=ON")
            -- Abseil replaces CMAKE_MSVC_RUNTIME_LIBRARY with the DLL runtime unless told
            -- otherwise, which would mix it with the static CRT of everything else (docs/adr/0038).
            table.insert(configs, "-DABSL_MSVC_STATIC_RUNTIME=ON")
            -- DXC adds /Zi to its Release build unless this is set, and its parallel cl.exe then
            -- fail on the one PDB directory xmake gives the build (C1041), even with /FS. Dawn and
            -- DXC predate CMP0141, so CMake adds no flag for it.
            table.insert(configs, "-DCMAKE_MSVC_DEBUG_INFORMATION_FORMAT=Embedded")
        end
        if package:is_plat("macosx", "iphoneos") then
            table.insert(configs, "-DCMAKE_OSX_DEPLOYMENT_TARGET=" .. get_config("target_minver"))
        end
        if package:is_plat("iphoneos") and get_config("appledev") == "simulator" then
            -- xmake only selects the simulator SDK for x86_64; arm64 is both a device and a
            -- simulator architecture.
            table.insert(configs, "-DCMAKE_OSX_SYSROOT=iphonesimulator")
        end
        cmake.install(package, configs)

        if package:is_plat("windows") then
            -- Dawn's install rules leave DXC in the build directory.
            os.cp(path.join(package:builddir(), "dxcompiler.dll"), package:installdir("bin"))
        end
    end)
package_end()

add_requires("dawn", {system = false, configs = jade_package_configs("dawn")})

local function dawn_files(target, package)
    local installdir = package:installdir()
    if target:is_plat("windows") then
        return {path.join(installdir, "bin", "webgpu_dawn.dll"), path.join(installdir, "bin", "dxcompiler.dll")}
    elseif target:is_plat("macosx") then
        return {path.join(installdir, "lib", "libwebgpu_dawn.dylib")}
    elseif target:is_plat("iphoneos") then
        return {path.join(installdir, "lib", "libwebgpu_dawn.a")}
    else
        return {path.join(installdir, "lib", "libwebgpu_dawn.so")}
    end
end

target("webgpu_dawn")
    set_kind("phony")
    add_packages("dawn")

    on_install(function (target)
        os.mkdir(target:installdir())
        local package = target:pkg("dawn")
        if target:is_plat("wasm") then
            -- The archive is named after the module the managed side imports (docs/adr/0025),
            -- and the link of the application needs Emdawnwebgpu's JavaScript next to it.
            os.cp(path.join(package:installdir(), "lib", "libemdawnwebgpu_c.a"), path.join(target:installdir(), "webgpu_dawn.a"))
            os.cp(path.join(package:installdir(), "js", "*.js"), target:installdir())
        else
            for _, file in ipairs(dawn_files(target, package)) do
                os.cp(file, target:installdir())
            end
        end
    end)
target_end()
