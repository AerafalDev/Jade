-- Dawn, the WebGPU implementation (ADR-0001), as one static archive for jade_native, built from
-- source with Dawn's own CMake. Backends: Vulkan on Linux, D3D12 on Windows, Metal on macOS, and
-- Null everywhere. Task 104 adds the mobile platforms.
local commits = {
    -- The release notes cite the parent commit 9af2744f; the tagged commit only rolls V8, unused here.
    ["20260930.214659"] = "6fa6adb71bdcbf7bb17fe21fb1702bf43e89078f"
}

-- Dawn's DEPS entries this configuration needs, with the CMake variable that points Dawn at each.
-- DEPS pins every entry to a commit. The rest of DEPS serves options turned off below (tests,
-- samples, GLFW, protobuf, OpenGL, DXC, ...).
--   vulkan  only needed with the Vulkan backend (third_party/CMakeLists.txt)
--   linked  code from it ends up in the archive (Ninja's header dependencies, 102), so its license
--           is staged: always, or only with the Vulkan backend
local dependencies = {
    ["third_party/abseil-cpp"] = {var = "DAWN_ABSEIL_DIR", linked = "always"},
    -- jinja2 and markupsafe run Dawn's code generator; nothing from them is linked.
    ["third_party/jinja2"] = {var = "DAWN_JINJA2_DIR"},
    ["third_party/markupsafe"] = {var = "DAWN_MARKUPSAFE_DIR"},
    -- Configured on every platform, but only Tint's SPIR-V reader and writer include it.
    ["third_party/spirv-headers/src"] = {var = "DAWN_SPIRV_HEADERS_DIR", linked = "vulkan"},
    -- Configured but never built: only the SPIR-V reader and SPIR-V validation link it, both off.
    ["third_party/spirv-tools/src"] = {var = "DAWN_SPIRV_TOOLS_DIR", vulkan = true},
    ["third_party/vulkan-headers/src"] = {var = "DAWN_VULKAN_HEADERS_DIR", vulkan = true, linked = "vulkan"},
    ["third_party/vulkan-utility-libraries/src"] = {var = "DAWN_VULKAN_UTILITY_LIBRARIES_DIR", vulkan = true, linked = "vulkan"}
}

-- Clones the DEPS entries named on the command line with Dawn's tools/fetch_dawn_dependencies.py,
-- then writes each entry's pinned commit and checked-out HEAD to a JSON file.
-- The script assumes a Dawn checkout, where every entry's directory already exists; and it ignores
-- git failures, so a failed clone would only show up later as a broken build. Hence the mkdir and
-- the HEAD check.
-- Arguments: <Dawn tools dir> <root holding DEPS> <output JSON> <entry>...
local fetch_script = [[
import argparse, json, pathlib, subprocess, sys
tools, root, output, names = sys.argv[1], pathlib.Path(sys.argv[2]), sys.argv[3], sys.argv[4:]
sys.path.insert(0, tools)
import fetch_dawn_dependencies as fetch
for name in names:
    (root / name).mkdir(parents=True, exist_ok=True)
fetch.process_dir(argparse.Namespace(git="git", shallow=True), root, names)
scope = {}
exec((root / "DEPS").read_text(), {"Var": fetch.Var, "Str": str}, scope)
result = []
for name in names:
    url, commit = scope["deps"][name]["url"].format(**scope.get("vars", {})).rsplit("@", 1)
    head = subprocess.run(["git", "-C", str(root / name), "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
    result.append({"name": name, "url": url, "commit": commit, "head": head})
pathlib.Path(output).write_text(json.dumps(result))
]]

package("dawn")
    set_homepage("https://dawn.googlesource.com/dawn")
    set_description("WebGPU implementation with the webgpu.h C API.")
    set_license("BSD-3-Clause")

    -- dawn.googlesource.com is the official source and has no release archives. Its GitHub mirror
    -- carries the same commits and tags.
    add_urls("https://github.com/google/dawn/archive/refs/tags/v$(version).tar.gz")
    add_versions("20260930.214659", "4a5619284239213e9f98725a04f383868c5cf6efe37e86330aa9826ca37cef03")

    -- The build hash ignores recipe scripts, so editing one would silently reuse the old install.
    add_configs("recipe", {description = "Hash of this recipe and of the staging module.", type = "string", readonly = true,
        default = hash.sha256(path.join(os.scriptdir(), "xmake.lua")) .. hash.sha256(path.join(os.scriptdir(), "../../../modules/stage.lua"))})

    -- What the monolithic archive needs from the system (conditional_private_platform_depends in
    -- src/dawn/native/CMakeLists.txt; Abseil adds CoreFoundation on Apple and pulls its Windows
    -- libraries in with #pragma comment). Direct3D, DXGI and the shader compilers are loaded at
    -- runtime.
    on_load(function (package)
        if package:is_plat("linux") then
            package:add("syslinks", "dl", "pthread", "m")
        elseif package:is_plat("windows") then
            package:add("syslinks", "user32", "onecore_apiset", "dxguid")
        elseif package:is_plat("macosx") then
            package:add("frameworks", "Cocoa", "IOKit", "Foundation", "IOSurface", "QuartzCore", "Metal", "CoreFoundation")
        end
    end)

    on_install("linux", "windows", "macosx", function (package)
        import("core.base.json")
        import("core.package.package", {alias = "core_package"})
        import("lib.detect.find_tool")

        local commit = commits[package:version_str()]
        local python = assert(find_tool("python3") or find_tool("python"), "dawn: Python 3 is required by Dawn's build")

        -- xmake deletes the source tree once installed, so the dependencies live in the package cache
        -- (XMAKE_PKG_CACHEDIR or ~/.xmake/cache/packages), one directory per Dawn commit. A reinstall
        -- after a recipe edit, or another configuration, fetches nothing.
        local depsdir = path.join(core_package.cachedir({rootonly = true}), "dawn-deps", commit)
        os.mkdir(depsdir)
        os.cp("DEPS", depsdir)
        local vulkan = package:is_plat("linux")
        local names = {}
        for _, name in ipairs(table.orderkeys(dependencies)) do
            if vulkan or not dependencies[name].vulkan then
                table.insert(names, name)
            end
        end
        local result = path.join(depsdir, "fetch.json")
        os.vrunv(python.program, table.join({"-c", fetch_script, path.absolute("tools"), depsdir, result}, names))
        for _, entry in ipairs(json.loadfile(result)) do
            if entry.head ~= entry.commit then
                -- Removed so the next build clones it again instead of reusing a broken checkout.
                os.tryrm(path.join(depsdir, entry.name))
                raise("dawn: fetching %s@%s from %s failed (HEAD is '%s')", entry.name, entry.commit, entry.url, entry.head)
            end
        end

        -- Every option is spelled out, defaults included, so a default changing upstream cannot
        -- silently change what jade_native contains.
        local configs = {
            "-DCMAKE_BUILD_TYPE=" .. (package:is_debug() and "Debug" or "Release"),
            "-DDAWN_BUILD_MONOLITHIC_LIBRARY=STATIC",
            "-DDAWN_FETCH_DEPENDENCIES=OFF",
            "-DDAWN_ENABLE_INSTALL=OFF",
            "-DDAWN_BUILD_SAMPLES=OFF",
            "-DDAWN_BUILD_TESTS=OFF",
            "-DDAWN_BUILD_BENCHMARKS=OFF",
            "-DDAWN_BUILD_FUZZERS=OFF",
            "-DDAWN_BUILD_NODE_BINDINGS=OFF",
            "-DDAWN_BUILD_PROTOBUF=OFF",
            "-DDAWN_USE_GLFW=OFF",
            "-DDAWN_ENABLE_SWIFTSHADER=OFF",
            -- Backends: one native API per platform. Null needs no GPU, so adapter and device tests
            -- also run on headless CI. D3D11 is not needed as a fallback: D3D12 runs on every
            -- Windows 10 and 11 that .NET 10 supports.
            "-DDAWN_ENABLE_VULKAN=" .. (vulkan and "ON" or "OFF"),
            "-DDAWN_ENABLE_D3D12=" .. (package:is_plat("windows") and "ON" or "OFF"),
            "-DDAWN_ENABLE_D3D11=OFF",
            "-DDAWN_ENABLE_METAL=" .. (package:is_plat("macosx") and "ON" or "OFF"),
            "-DDAWN_ENABLE_NULL=ON",
            "-DDAWN_ENABLE_DESKTOP_GL=OFF",
            "-DDAWN_ENABLE_OPENGLES=OFF",
            -- Surfaces from X11 and Wayland windows (what SDL hands over). Dawn needs X11/Xlib.h and
            -- X11/Xlib-xcb.h at build time (libx11-dev and libx11-xcb-dev on Debian and Ubuntu) and
            -- loads the libraries at runtime.
            "-DDAWN_USE_X11=" .. (package:is_plat("linux") and "ON" or "OFF"),
            "-DDAWN_USE_WAYLAND=" .. (package:is_plat("linux") and "ON" or "OFF"),
            -- Windows: surfaces come from HWNDs, which need nothing extra; Windows UI only adds the
            -- UWP CoreWindow and SwapChainPanel sources. D3D12 compiles shaders with FXC
            -- (d3dcompiler_47.dll, loaded at runtime from the library's directory, then from the
            -- system's), so neither DXC nor the Agility SDK is built.
            "-DDAWN_USE_WINDOWS_UI=OFF",
            "-DDAWN_USE_BUILT_DXC=OFF",
            "-DDAWN_USE_AGILITY_SDK=OFF",
            "-DDAWN_FORCE_SYSTEM_COMPONENT_LOAD=OFF",
            -- macOS: Dawn links the AppKit and IOKit frameworks for Metal surfaces only when told so.
            "-DDAWN_TARGET_MACOS=" .. (package:is_plat("macosx") and "ON" or "OFF"),
            -- Shader input is WGSL only: no SPIR-V shader modules, so no SPIR-V reader and no
            -- validation of SPIR-V input (the only users of SPIRV-Tools).
            "-DDAWN_ENABLE_SPIRV_VALIDATION=OFF",
            "-DTINT_BUILD_SPV_READER=OFF",
            "-DTINT_BUILD_WGSL_READER=ON",
            -- Tint writers: the backend's shading language (SPIR-V for Vulkan, HLSL for D3D12, MSL
            -- for Metal) and NULL for the Null backend.
            "-DTINT_BUILD_SPV_WRITER=" .. (vulkan and "ON" or "OFF"),
            "-DTINT_BUILD_HLSL_WRITER=" .. (package:is_plat("windows") and "ON" or "OFF"),
            "-DTINT_BUILD_MSL_WRITER=" .. (package:is_plat("macosx") and "ON" or "OFF"),
            "-DTINT_BUILD_NULL_WRITER=ON",
            "-DTINT_BUILD_WGSL_WRITER=OFF",
            "-DTINT_BUILD_GLSL_WRITER=OFF",
            "-DTINT_BUILD_GLSL_VALIDATOR=OFF",
            "-DTINT_BUILD_IR_BINARY=OFF",
            "-DTINT_BUILD_CMD_TOOLS=OFF",
            "-DTINT_BUILD_TESTS=OFF",
            "-DTINT_BUILD_BENCHMARKS=OFF",
            "-DTINT_BUILD_FUZZERS=OFF"
        }
        for _, name in ipairs(names) do
            table.insert(configs, "-D" .. dependencies[name].var .. "=" .. path.join(depsdir, name))
        end
        if package:is_plat("macosx") and get_config("target_minver") then
            table.insert(configs, "-DCMAKE_OSX_DEPLOYMENT_TARGET=" .. get_config("target_minver"))
        end
        -- Abseil picks the MSVC runtime itself and defaults to the DLL one (its CMakeLists.txt),
        -- which would clash with the static CRT of everything else (ADR-0003).
        if package:is_plat("windows") then
            table.insert(configs, "-DABSL_MSVC_STATIC_RUNTIME=" .. (package:has_runtime("MT", "MTd") and "ON" or "OFF"))
        end

        -- __FILE__ in Dawn's and Abseil's assertions and logs would embed the builder's cache paths
        -- (102): they become relative to the source trees. MSVC has no documented equivalent.
        local cxflags = {}
        if not package:is_plat("windows") then
            cxflags = {"-ffile-prefix-map=" .. os.curdir() .. "=dawn", "-ffile-prefix-map=" .. depsdir .. "=dawn"}
        end

        -- Only the monolithic archive: the default target also builds the non-bundled libraries,
        -- and Dawn's install step needs it. The two files used here are copied by hand instead.
        import("package.tools.cmake").build(package, configs, {target = "webgpu_dawn", cxflags = cxflags})

        local builddir = package:builddir()
        local archive = package:is_plat("windows") and "webgpu_dawn.lib" or "libwebgpu_dawn.a"
        os.cp(path.join(builddir, "src", "dawn", "native", archive), package:installdir("lib"))
        -- webgpu/webgpu.h only includes dawn/webgpu.h, which is generated from dawn.json.
        os.cp("include/webgpu/webgpu.h", path.join(package:installdir("include"), "webgpu", "webgpu.h"))
        os.cp(path.join(builddir, "gen", "include", "dawn", "webgpu.h"), path.join(package:installdir("include"), "dawn", "webgpu.h"))

        -- Dawn has no export list of its own. Its shared build exports what the header declares with
        -- WGPU_EXPORT, so the list comes from there. emscripten_webgpu_get_device is declared there too
        -- but only exists in Emscripten builds.
        local exports = {}
        for name in io.readfile(path.join(package:installdir("include"), "dawn", "webgpu.h")):gmatch("\nWGPU_EXPORT [^\n(]-[%s%*](wgpu[%w_]+)%(") do
            table.insert(exports, name)
        end

        -- Dawn's own code, and the third-party code compiled or inlined into the archive (Ninja's
        -- header dependencies of a build with these options). SPIRV-Tools, jinja2 and markupsafe are
        -- not among them.
        local licenses = {
            ["LICENSE"] = "LICENSE.txt",
            -- Chromium's license, which src/dawn/common/LinkedList.h (copied from Chromium) refers to.
            ["tools/nocompile/LICENSE"] = "chromium-LICENSE.txt",
            ["third_party/renderdoc/LICENSE.md"] = "renderdoc-LICENSE.md"
        }
        for _, name in ipairs(names) do
            local linked = dependencies[name].linked
            if linked == "always" or (linked == "vulkan" and vulkan) then
                local project = name:match("third_party/([^/]+)")
                -- The Khronos repositories keep the full license texts in LICENSES/.
                for _, file in ipairs(table.join(os.files(path.join(depsdir, name, "LICENSE*")), os.files(path.join(depsdir, name, "LICENSES", "*")))) do
                    licenses[file] = project .. "-" .. path.filename(file)
                end
            end
        end

        import("stage", {rootdir = path.join(package:scriptdir(), "../../../modules")})(package, {
            commit = commit,
            exports = exports,
            headers = {"webgpu/webgpu.h", "dawn/webgpu.h"},
            licenses = licenses,
            metadata = {["src/dawn/dawn.json"] = "dawn.json"}
        })
    end)

    on_test(function (package)
        assert(package:has_cfuncs("wgpuCreateInstance", {includes = "webgpu/webgpu.h"}))
    end)
