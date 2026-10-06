-- Native libraries of the Jade.Native.* packages (docs/adr/0010, docs/adr/0031), and the layout
-- libraries that only the tests load (docs/adr/0036).
--
-- scripts/build-native.cs fetches the sources at the commits pinned in build/versions.json and
-- configures this project with their locations; running xmake by hand needs the same options.

set_xmakever("3.1.1")
set_project("jade-natives")

-- The runtime identifiers of docs/adr/0012, as xmake names them
-- (scripts/build-native/Build/NativeTarget.cs).
set_allowedplats("linux", "windows", "macosx", "iphoneos", "android", "wasm")
set_allowedarchs(
    "linux|x86_64", "linux|arm64",
    "windows|x64", "windows|arm64",
    "macosx|x86_64", "macosx|arm64",
    "iphoneos|arm64", "iphoneos|x86_64",
    "android|arm64-v8a", "android|x86_64",
    "wasm|wasm32")
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

option("win32_winnt")
    set_showmenu(true)
    set_description("The _WIN32_WINNT value of the minimum Windows version (docs/adr/0024).")
option_end()

-- The libraries are linked statically into the application on iOS and in the browser
-- (docs/adr/0011, docs/adr/0025), and loaded as shared libraries everywhere else.
function jade_library_kind()
    return is_plat("iphoneos", "wasm") and "static" or "shared"
end

-- xmake applies --runtimes to the targets but not to the packages, which get it here with their
-- build key: the static CRT on Windows, so that no Visual C++ redistributable is needed, and the
-- NDK's static libc++ on Android (docs/adr/0038).
function jade_package_configs(name)
    return {key = get_config(name .. "_key"), runtimes = get_config("runtimes")}
end

includes("dawn", "sdl", "miniaudio", "layout")
