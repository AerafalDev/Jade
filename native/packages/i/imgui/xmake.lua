-- Dear ImGui, docking branch, as one static archive for jade_native (ADR-0014): ImGui itself, the C
-- API that dear_bindings generated for the same tag, and the SDL3, WebGPU and null backends with
-- their dear_bindings wrappers. Linux, Windows and macOS for now; tasks 104 and 105 add the mobile
-- platforms and the browser.
local commits = {
    ["1.92.9b-docking"] = "b48d1afbe8ee8b238e2961dc363a949dd7304e23"
}

-- The dear_bindings release generated from each ImGui tag (its release names carry the ImGui
-- version; there is no "latest"). Generated files fall under ImGui's license, as dear_bindings'
-- docs/Readme.md says, and the archive's LICENSE.txt is ImGui's.
local dear_bindings = {
    ["1.92.9b-docking"] = {version = "0.24", commit = "ec09a883592857d038e82ec618b079cd260a0919", license = "MIT"}
}

-- Compile-time configuration. Both shape the C API (dcimgui.h and dcimgui_impl_wgpu.h test them), so
-- the shims and the binding generator see the same list (exported as package defines and staged in
-- versions.json).
local defines = {"IMGUI_DISABLE_OBSOLETE_FUNCTIONS", "IMGUI_IMPL_WEBGPU_BACKEND_DAWN"}

-- Backends compiled in: backends/imgui_impl_<name>.cpp and its wrapper backends/dcimgui_impl_<name>.cpp.
local backends = {"sdl3", "wgpu", "null"}

package("imgui")
    set_homepage("https://github.com/ocornut/imgui")
    set_description("Dear ImGui, docking branch, with the dear_bindings C API and the SDL3, WebGPU and null backends.")
    set_license("MIT")

    add_urls("https://github.com/ocornut/imgui/archive/refs/tags/v$(version).tar.gz")
    add_versions("1.92.9b-docking", "90ded916bd57db2e0e171b6b098940a47c6f5042725dcdc67fb19940ca8bfdcc")
    add_resources("1.92.9b-docking", "dear_bindings",
        "https://github.com/dearimgui/dear_bindings/releases/download/DearBindings_v0.24_ImGui_v1.92.9b-docking/DearBindings_v0.24_ImGui_v1.92.9b-docking.zip",
        "ee7a51d4220b5ba28672253dc95e287d9e00ae9f9a5321725d8c3b978789694b")

    -- The build hash ignores recipe scripts, so editing one would silently reuse the old install.
    add_configs("recipe", {description = "Hash of this recipe and of the staging module.", type = "string", readonly = true,
        default = hash.sha256(path.join(os.scriptdir(), "xmake.lua")) .. hash.sha256(path.join(os.scriptdir(), "../../../modules/stage.lua"))})

    on_load(function (package)
        -- Required exactly as native/xmake.lua requires them, so that the backends compile against the
        -- SDL3 and Dawn builds jade_native links rather than a second build of each. Only their
        -- headers are used here.
        for _, dep in ipairs({"sdl3", "dawn"}) do
            package:add("deps", dep, {system = false, configs = {debug = package:is_debug()}})
        end
        package:add("defines", table.unpack(defines))
        if package:is_plat("linux") then
            package:add("syslinks", "m")
        elseif package:is_plat("windows") then
            -- ImGui's default clipboard, IME and shell functions (imgui.cpp also names them with
            -- #pragma comment).
            package:add("syslinks", "user32", "imm32", "shell32")
        elseif package:is_plat("macosx") then
            -- The WebGPU backend's surface helper puts a CAMetalLayer into an NSWindow.
            package:add("frameworks", "Cocoa", "QuartzCore")
        end
    end)

    on_install("linux", "windows", "macosx", function (package)
        import("core.base.binutils")
        import("core.base.json")

        os.cp(package:resourcedir("dear_bindings"), "dear_bindings")

        local sources = {"imgui.cpp", "imgui_demo.cpp", "imgui_draw.cpp", "imgui_tables.cpp", "imgui_widgets.cpp", "dear_bindings/dcimgui.cpp"}
        local headers = {"imconfig.h", "dear_bindings/dcimgui.h"}
        for _, backend in ipairs(backends) do
            table.insert(sources, ("backends/imgui_impl_%s.cpp"):format(backend))
            table.insert(sources, ("dear_bindings/backends/dcimgui_impl_%s.cpp"):format(backend))
            table.insert(headers, ("dear_bindings/backends/dcimgui_impl_%s.h"):format(backend))
        end

        -- No mode.release rule: it compiles static targets with hidden visibility, which would keep
        -- the wrappers' functions out of jade_native's exports. The wrappers warn -Wunused-function
        -- (unused conversion helpers), so warnings keep the compiler's defaults.
        local lines = {
            'target("imgui")',
            '    set_kind("static")',
            '    set_languages("c++17")',
            '    add_includedirs(".", "backends", "dear_bindings", "dear_bindings/backends")',
            ('    add_includedirs(%q, %q)'):format(package:dep("sdl3"):installdir("include"), package:dep("dawn"):installdir("include")),
            '    if is_mode("debug") then',
            '        set_symbols("debug")',
            '        set_optimize("none")',
            '    else',
            '        set_optimize("fastest")',
            -- IM_ASSERT stays ImGui's assert(): it aborts in debug builds and compiles out here, like
            -- miniaudio's asserts. Recoverable API misuse still goes through ImGui's error recovery,
            -- which logs it and shows a tooltip without asserting (io.ConfigErrorRecovery*).
            '        add_defines("NDEBUG")',
            '    end'
        }
        for _, source in ipairs(sources) do
            if source == "backends/imgui_impl_wgpu.cpp" and package:is_plat("macosx") then
                -- Its surface helper is Objective-C on macOS, behind a .cpp extension.
                table.insert(lines, ('    add_files("%s", {cxxflags = "-x objective-c++"})'):format(source))
            else
                table.insert(lines, ('    add_files("%s")'):format(source))
            end
        end
        for _, header in ipairs(headers) do
            table.insert(lines, ('    add_headerfiles("%s", {prefixdir = "imgui"})'):format(header))
        end
        for _, define in ipairs(defines) do
            table.insert(lines, ('    add_defines("%s")'):format(define))
        end
        io.writefile("xmake.lua", table.concat(lines, "\n") .. "\n")
        import("package.tools.xmake").install(package)

        -- The exports are the functions of the C API's metadata whose preprocessor conditionals hold
        -- for this build. Each macro those conditionals test is listed with whether this build defines
        -- it; any other macro or form fails the install rather than being guessed.
        local macros = {
            IMGUI_DISABLE_DEBUG_TOOLS = false,
            IMGUI_HAS_IMSTR = false,
            IMGUI_IMPL_WEBGPU_BACKEND_WGPU = false,
            __EMSCRIPTEN__ = package:is_plat("wasm") == true
        }
        for _, define in ipairs(defines) do
            macros[define] = true
        end
        local function holds(conditional)
            local condition, expression = conditional.condition, conditional.expression
            local name = expression
            if condition == "if" or condition == "ifnot" then
                name = expression:match("^defined%(([%w_]+)%)$")
            end
            local defined = name and macros[name]
            if defined == nil or not (condition == "ifdef" or condition == "ifndef" or condition == "if" or condition == "ifnot") then
                raise("imgui: unknown conditional `%s %s` in the dear_bindings metadata", condition, expression)
            end
            return (condition == "ifdef" or condition == "if") == defined
        end

        local metadata = {["dear_bindings/dcimgui.json"] = "dcimgui.json"}
        for _, backend in ipairs(backends) do
            metadata[("dear_bindings/backends/dcimgui_impl_%s.json"):format(backend)] = ("dcimgui_impl_%s.json"):format(backend)
        end
        local exports = {}
        local listed = {}
        for _, source in ipairs(table.orderkeys(metadata)) do
            for _, func in ipairs(json.loadfile(source).functions) do
                local enabled = true
                for _, conditional in ipairs(func.conditionals or {}) do
                    if not holds(conditional) then
                        enabled = false
                    end
                end
                if enabled then
                    table.insert(exports, func.name)
                    listed[func.name] = true
                end
            end
        end

        -- The list must be exactly the functions the wrappers define, the only unmangled ones (ImGui and
        -- its backends are C++). A wrong entry in the macros above would otherwise hide a function, or
        -- list one the linker cannot find (which only Linux tolerates). The object files are read rather
        -- than the archive: xmake 3.1.1's archive reader skips the members whose names are long.
        local objects = os.files(path.join(package:builddir(), ".objs", "**" .. (package:is_plat("windows") and ".obj" or ".o")))
        assert(#objects == #sources, ("imgui: %d object files for %d sources"):format(#objects, #sources))
        local underscore = package:is_plat("macosx", "iphoneos")
        local defined = {}
        for _, object in ipairs(objects) do
            for _, entry in ipairs(binutils.readsyms(object)) do
                for _, symbol in ipairs(entry.symbols or {}) do
                    local name = symbol.name
                    if underscore and name:startswith("_") then
                        name = name:sub(2)
                    end
                    if symbol.type == "T" and name:match("^%a[%w_]*$") then
                        defined[name] = true
                    end
                end
            end
        end
        local errors = {}
        for _, name in ipairs(table.orderkeys(defined)) do
            if not listed[name] then
                table.insert(errors, name .. " is defined but not in the metadata")
            end
        end
        for _, name in ipairs(exports) do
            if not defined[name] then
                table.insert(errors, name .. " is in the metadata but not defined")
            end
        end
        if #errors > 0 then
            raise("imgui: the export list does not match the archive:\n  " .. table.concat(errors, "\n  "))
        end

        local version = package:version_str()
        import("stage", {rootdir = path.join(package:scriptdir(), "../../../modules")})(package, {
            commit = commits[version],
            exports = exports,
            headers = {"imgui/*.h"},
            licenses = {["LICENSE.txt"] = "LICENSE.txt"},
            metadata = metadata,
            defines = defines,
            resources = {dear_bindings = dear_bindings[version]}
        })
    end)

    on_test(function (package)
        assert(package:has_cxxfuncs("ImGui_GetVersion", {includes = "imgui/dcimgui.h"}))
    end)
