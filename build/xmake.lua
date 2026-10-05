-- Native libraries of the Jade.Native.* packages (docs/adr/0010, docs/adr/0031), and the layout
-- libraries that only the tests load (docs/adr/0036).
--
-- scripts/build-native.cs fetches the sources at the commits pinned in build/versions.json and
-- configures this project with their locations; running xmake by hand needs the same options.

set_xmakever("3.1.1")
set_project("jade-natives")

set_allowedplats("linux")
set_allowedarchs("linux|x86_64")
set_allowedmodes("release")
set_defaultmode("release")

-- Packages are built from the sources given below, never downloaded prebuilt, and installed
-- inside the build directory so that nothing outside it depends on a previous build.
set_policy("package.precompiled", false)
set_policy("package.install_locally", true)
-- The generator is fixed rather than left to xmake's detection or to CMAKE_GENERATOR.
set_policy("package.cmake_generator.ninja", true)

for _, name in ipairs({"dawn", "sdl", "miniaudio"}) do
    option(name .. "_source")
        set_showmenu(true)
        set_description("The source directory of " .. name .. " at its pinned commit.")
    option_end()
end

-- xmake reuses an installed package as long as its configs are unchanged, so a package config
-- carries a key that changes with the pinned commit and with the package definition.
for _, name in ipairs({"dawn", "sdl"}) do
    option(name .. "_key")
        set_showmenu(true)
        set_description("The build key of " .. name .. ": a change rebuilds it.")
    option_end()
end

includes("dawn", "sdl", "miniaudio", "layout")
