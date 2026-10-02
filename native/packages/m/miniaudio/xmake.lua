-- miniaudio as a static library for jade_native, so its implementation is compiled exactly once.
-- Only Linux is supported so far; tasks 103, 104 and 105 add the other platforms.
local commits = {
    ["0.11.25"] = "9634bedb5b5a2ca38c1ee7108a9358a4e233f14d"
}

-- Compile-time configuration. It decides which APIs and struct members exist, so the shims and the
-- binding generator see the same list (exported as package defines and staged in versions.json).
-- Empty: every backend, decoder and the engine are kept, and backends stay loaded at runtime.
local defines = {}

package("miniaudio")
    set_homepage("https://miniaud.io")
    set_description("Audio playback, capture, decoding and mixing in a single C source file.")
    -- Dual-licensed by upstream: public domain (Unlicense) or MIT No Attribution.
    set_license("Unlicense OR MIT-0")

    add_urls("https://github.com/mackron/miniaudio/archive/refs/tags/$(version).tar.gz")
    add_versions("0.11.25", "b900edcffe979816e2560a0580b9b1216d674b4f17fbadeca8f777a7f8ab0274")

    -- The build hash ignores recipe scripts, so editing one would silently reuse the old install.
    add_configs("recipe", {description = "Hash of this recipe and of the staging module.", type = "string", readonly = true,
        default = hash.sha256(path.join(os.scriptdir(), "xmake.lua")) .. hash.sha256(path.join(os.scriptdir(), "../../../modules/stage.lua"))})

    on_load(function (package)
        if #defines > 0 then
            package:add("defines", table.unpack(defines))
        end
        if package:is_plat("linux") then
            package:add("syslinks", "m", "dl", "pthread")
        end
    end)

    on_install("linux", function (package)
        -- No mode.release rule: it compiles static targets with hidden visibility, which would hide
        -- every MA_API function (a plain `extern`) from jade_native's exports.
        local lines = {
            'target("miniaudio")',
            '    set_kind("static")',
            '    add_files("miniaudio.c")',
            '    add_headerfiles("miniaudio.h", {prefixdir = "miniaudio"})',
            '    if is_mode("debug") then',
            '        set_symbols("debug")',
            '        set_optimize("none")',
            '    else',
            '        set_optimize("fastest")',
            '        add_defines("NDEBUG")',
            '    end'
        }
        for _, define in ipairs(defines) do
            table.insert(lines, ('    add_defines("%s")'):format(define))
        end
        io.writefile("xmake.lua", table.concat(lines, "\n") .. "\n")
        import("package.tools.xmake").install(package)

        import("stage", {rootdir = path.join(package:scriptdir(), "../../../modules")})(package, {
            commit = commits[package:version_str()],
            -- Internal functions are static (MA_PRIVATE), so every global symbol is public API.
            exports = {"ma_*"},
            headers = {"miniaudio/miniaudio.h"},
            licenses = {["LICENSE"] = "LICENSE.txt"},
            defines = defines
        })
    end)

    on_test(function (package)
        assert(package:has_cfuncs("ma_version_string", {includes = "miniaudio/miniaudio.h"}))
    end)
