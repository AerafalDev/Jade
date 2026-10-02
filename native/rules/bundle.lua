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
--                   carry a leading underscore). Written but not verified yet (103, 104).
--   Windows         /WHOLEARCHIVE, and a .def file. A .def file has no wildcards, so it has to be
--                   generated before linking from the archives' external symbols matched against
--                   the list (dumpbin /symbols or llvm-nm). Not implemented yet (103).
-- A static jade_native (browser-wasm, maybe iOS) exports nothing by itself; it needs the upstream
-- archives merged into it instead (104, 105).
rule("jade.bundle")
    on_config(function (target)
        if target:kind() ~= "shared" then
            raise("jade.bundle: only a shared jade_native is implemented yet (static: tasks 104 and 105)")
        end

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
            for line in io.lines(exportsfile) do
                if #line > 0 then
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
                table.insert(archives, libfile)
            end
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
            target:add("shflags", "-Wl,-exported_symbols_list," .. list, {force = true})
        else
            raise("jade.bundle: export control is not implemented for %s yet (task 103)", target:plat())
        end
    end)

-- Writes <targetdir>/<target>.manifest.json after each build: the library, the target's own
-- public headers and the install directory of each bundled package. scripts/build-native.cs reads
-- it to stage artifacts/native/<rid>/.
rule("jade.manifest")
    after_build(function (target)
        import("core.base.json")

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

        json.savefile(path.join(target:targetdir(), target:name() .. ".manifest.json"), {
            library = path.absolute(target:targetfile()),
            headers = json.mark_as_array(headers),
            packages = json.mark_as_array(packages)
        }, {pretty = true})
    end)
