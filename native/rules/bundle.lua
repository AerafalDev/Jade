-- Export control for jade_native (ADR-0003): every bundled upstream archive is linked in full, and
-- only its public C API, plus the jade_* shims, leaves the shared library.
--
-- A target bundles a package with
--   add_packages("<package>")
--   add_values("jade.bundle", "<package>")
-- (native/xmake.lua does both from its `bundled` list). The package's stage fragment lists its
-- public API in <installdir>/jade/exports.txt: exact names or `*` patterns (modules/stage.lua).
--
-- A linker pulls from a static archive only the members something references, so the public API
-- would mostly be dropped: each archive is linked whole. The export list then hides everything
-- else, including whatever an upstream build left with default visibility:
--   Linux, Android  --whole-archive, and a version script (the list global, the rest local).
--                   --gc-sections then drops what no export reaches. Measured on Dawn alone
--                   (stripped): whole archive 10.0 MB, only the members the exports need
--                   (--undefined per name) 9.4 MB, either one with --gc-sections 8.7 MB.
--   macOS, iOS      -force_load, and -exported_symbols_list (ld64 accepts wildcards; Mach-O names
--                   carry a leading underscore). -dead_strip plays the part of --gc-sections.
--   Windows         /WHOLEARCHIVE, and a .def file. A .def file has no wildcards, so each `*`
--                   pattern is expanded against the defined globals of its own package's archives
--                   (xmake's binutils.readsyms reads MSVC archives), with DATA for variables. The
--                   jade_* shims export themselves through JADE_API (__declspec(dllexport)).
--                   /OPT:REF and /OPT:ICF are spelled out: /DEBUG, which writes the PDB, turns them
--                   off by default.
-- A static jade_native (browser-wasm, maybe iOS) exports nothing by itself; it needs the upstream
-- archives merged into it instead (104, 105).
rule("jade.bundle")
    on_config(function (target)
        if target:kind() ~= "shared" then
            raise("jade.bundle: only a shared jade_native is implemented yet (static: tasks 104 and 105)")
        end

        -- One entry per bundled package: the names it exports and its archives.
        local bundles = {}
        local exports = {"jade_*"}
        local archives = {}
        for _, name in ipairs(table.wrap(target:values("jade.bundle"))) do
            local pkg = target:pkg(name)
            if not pkg then
                raise("jade.bundle: package %s is bundled but not added to %s", name, target:name())
            end
            local exportsfile = path.join(pkg:installdir(), "jade", "exports.txt")
            if not os.isfile(exportsfile) then
                raise("jade.bundle: package %s has no %s", name, exportsfile)
            end
            local bundle = {name = name, exports = {}, archives = {}}
            for line in io.lines(exportsfile) do
                if #line > 0 then
                    table.insert(bundle.exports, line)
                    table.insert(exports, line)
                end
            end
            local libfiles = table.wrap(pkg:libraryfiles())
            if #libfiles == 0 then
                raise("jade.bundle: package %s has no library file", name)
            end
            for _, libfile in ipairs(libfiles) do
                if not libfile:endswith(".a") and not libfile:endswith(".lib") then
                    raise("jade.bundle: %s is not a static archive", libfile)
                end
                table.insert(bundle.archives, libfile)
                table.insert(archives, libfile)
            end
            table.insert(bundles, bundle)
        end

        -- The content goes into the file name: when the list changes, the flags change and xmake
        -- relinks.
        local function write_list(extension, lines)
            local content = table.concat(lines, "\n") .. "\n"
            local file = path.join(target:autogendir(), "rules", "jade.bundle",
                "exports-" .. hash.uuid(content):sub(1, 8):lower() .. extension)
            if not os.isfile(file) then
                io.writefile(file, content)
            end
            return file
        end

        if target:is_plat("linux", "android") then
            local lines = {"{", "  global:"}
            for _, symbol in ipairs(exports) do
                table.insert(lines, "    " .. symbol .. ";")
            end
            table.join2(lines, {"  local:", "    *;", "};"})
            local script = write_list(".map", lines)
            for _, archive in ipairs(archives) do
                target:add("shflags", "-Wl,--whole-archive," .. archive .. ",--no-whole-archive", {force = true})
            end
            -- --no-undefined: an unresolved symbol fails the link instead of the first dlopen.
            -- --as-needed: xmake links shared libraries with the C++ driver, whose implicit
            -- runtime libraries must not become dependencies when nothing uses them.
            target:add("shflags", "-Wl,--version-script=" .. script, "-Wl,--no-undefined", "-Wl,--as-needed", "-Wl,--gc-sections", {force = true})
            if target:is_plat("linux") then
                -- The C++ runtime of the C++ upstreams goes inside (ADR-0003), hidden by the version
                -- script like the rest. Explicit flags, because xmake's stdc++_static runtime adds
                -- -static-libstdc++ only to targets with C++ sources, and never -static-libgcc.
                -- Android's libc++ is set up by the NDK toolchain instead (task 104).
                target:add("shflags", "-static-libstdc++", "-static-libgcc", {force = true})
            end
        elseif target:is_plat("macosx", "iphoneos") then
            local lines = {}
            for _, symbol in ipairs(exports) do
                table.insert(lines, "_" .. symbol)
            end
            local list = write_list(".txt", lines)
            for _, archive in ipairs(archives) do
                target:add("shflags", "-Wl,-force_load," .. archive, {force = true})
            end
            target:add("shflags", "-Wl,-exported_symbols_list," .. list, "-Wl,-dead_strip", {force = true})
        elseif target:is_plat("windows") then
            import("core.base.binutils")

            local lines = {"EXPORTS"}
            for _, bundle in ipairs(bundles) do
                local patterns = {}
                for _, entry in ipairs(bundle.exports) do
                    if entry:find("*", 1, true) then
                        table.insert(patterns, {entry = entry, pattern = "^" .. entry:gsub("%*", ".*") .. "$", matched = false})
                    else
                        table.insert(lines, "    " .. entry)
                    end
                end
                if #patterns > 0 then
                    -- nm-style types: upper case is a defined global, U an undefined one.
                    local seen = {}
                    for _, archive in ipairs(bundle.archives) do
                        for _, object in ipairs(binutils.readsyms(archive)) do
                            for _, symbol in ipairs(object.symbols or {}) do
                                local kind = symbol.type
                                if kind and kind:match("^[A-TV-Z]$") and not seen[symbol.name] then
                                    for _, item in ipairs(patterns) do
                                        if symbol.name:match(item.pattern) then
                                            seen[symbol.name] = true
                                            item.matched = true
                                            table.insert(lines, "    " .. symbol.name .. ((kind == "T") and "" or " DATA"))
                                            break
                                        end
                                    end
                                end
                            end
                        end
                    end
                    for _, item in ipairs(patterns) do
                        if not item.matched then
                            raise("jade.bundle: no global symbol of package %s matches the export %s", bundle.name, item.entry)
                        end
                    end
                end
            end
            local deffile = write_list(".def", lines)
            for _, archive in ipairs(archives) do
                target:add("shflags", "/WHOLEARCHIVE:" .. archive, {force = true})
            end
            target:add("shflags", "/DEF:" .. deffile, {force = true})
            if is_mode("release") then
                target:add("shflags", "/OPT:REF", "/OPT:ICF", {force = true})
            end
        else
            raise("jade.bundle: export control is not implemented for %s", target:plat())
        end
    end)

-- Writes <targetdir>/<target>.manifest.json after each build: the library, its separate debug
-- symbols when the build has some, the target's own public headers, the install directory of each
-- bundled package and the toolchain that built it. scripts/build-native.cs reads it to stage
-- artifacts/native/<rid>/ and records the toolchain in versions.json.
rule("jade.manifest")
    after_build(function (target)
        import("core.base.json")
        import("lib.detect.find_tool")

        local headers = {}
        local rootdir = path.join(os.tmpdir(), "jade-headers")
        local srcheaders, dstheaders = target:headerfiles(rootdir)
        for i, srcheader in ipairs(table.wrap(srcheaders)) do
            table.insert(headers, {
                source = path.absolute(srcheader),
                destination = path.unix(path.relative(dstheaders[i], rootdir))
            })
        end

        local packages = {}
        for _, name in ipairs(table.wrap(target:values("jade.bundle"))) do
            table.insert(packages, {name = name, installdir = target:pkg(name):installdir()})
        end

        -- A release build has a symbol file next to the library: the PDB on Windows, the dSYM bundle
        -- on Apple, the debug file the utils.symbols.extract rule splits off elsewhere.
        local symbols = target:symbolfile()
        symbols = os.exists(symbols) and path.absolute(symbols) or nil

        -- Every value is a display string; the packages are built by the same toolchain.
        local envs = {}
        for _, toolchain_inst in ipairs(target:toolchains()) do
            envs = os.joinenvs(envs, toolchain_inst:runenvs() or {})
        end
        local toolchain = {xmake = xmake.version():shortstr()}
        local function describe(name, program)
            local tool = try { function () return find_tool(name, {program = program, version = true, envs = envs}) end }
            return tool and tool.version and (name .. " " .. tool.version) or name
        end
        for _, kind in ipairs({"cc", "cxx", "sh"}) do
            local program, toolname = target:tool(kind)
            if program then
                toolchain[kind] = describe(toolname or path.basename(program), program)
            end
        end
        toolchain.cmake = describe("cmake")
        toolchain.ninja = describe("ninja")
        for _, toolchain_inst in ipairs(target:toolchains()) do
            if toolchain_inst:name() == "msvc" then
                toolchain.vs = toolchain_inst:config("vs")
                toolchain.vs_toolset = toolchain_inst:config("vs_toolset")
                toolchain.vs_sdkver = toolchain_inst:config("vs_sdkver")
            elseif toolchain_inst:name() == "xcode" then
                toolchain.xcode_sdkver = toolchain_inst:config("xcode_sdkver")
                toolchain.target_minver = toolchain_inst:config("target_minver")
            end
        end
        if target:is_plat("linux") then
            toolchain.libc = try { function () return os.iorunv("getconf", {"GNU_LIBC_VERSION"}):trim() end }
        end

        json.savefile(path.join(target:targetdir(), target:name() .. ".manifest.json"), {
            library = path.absolute(target:targetfile()),
            symbols = symbols,
            headers = json.mark_as_array(headers),
            packages = json.mark_as_array(packages),
            toolchain = toolchain
        }, {pretty = true})
    end)
