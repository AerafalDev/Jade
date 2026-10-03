-- jade_native: one library per RID that links every bundled upstream statically and exports only
-- their public C APIs plus the jade_* shims (ADR-0003). Build it with scripts/build-native.cs,
-- which maps the RID to this project's platform, architecture and toolchain (ADR-0004).
set_project("jade_native")
set_xmakever("3.1.1")

add_rules("mode.debug", "mode.release")

-- Every package comes from the local repository in this directory (packages/<l>/<name>/). The
-- official xmake-repo is never cloned, and no package is taken from the system.
set_policy("network.mode", "private")
add_repositories("jade-native .", {rootdir = os.scriptdir()})

includes("rules/bundle.lua")

-- Upstreams linked into jade_native. Bundling a library is one line here plus its package
-- definition, whose stage fragment lists the symbols to export (modules/stage.lua).
local bundled = {
    "sdl3",
    "miniaudio",
    "dawn"
}

for _, name in ipairs(bundled) do
    add_requires(name, {system = false, configs = {debug = is_mode("debug")}})
end

target("jade_native")
    set_kind("shared")
    set_languages("c11")
    -- Shims export through JADE_API only. Release builds keep their symbols too: the library is
    -- linked unstripped, then utils.symbols.extract moves the symbol table (and the shims' debug
    -- information; the packages are built without any) to a separate file and strips the library.
    -- On Windows the linker writes the PDB itself.
    set_symbols("debug", "hidden")
    set_warnings("allextra", "error")
    add_rules("jade.bundle", "jade.manifest", "utils.symbols.extract")

    add_files("shims/*.c")
    add_headerfiles("shims/jade_native.h", {prefixdir = "jade"})
    add_defines("JADE_NATIVE_BUILD")

    for _, name in ipairs(bundled) do
        add_packages(name)
        add_values("jade.bundle", name)
    end
