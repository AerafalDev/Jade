-- Writes the stage fragment of an upstream package to <installdir>/jade/. scripts/build-native.cs
-- merges the fragments of every bundled package into artifacts/native/<rid>/:
--
--   jade/upstream.json   one entry of metadata/versions.json
--   jade/include/        headers the binding generator reads, copied to include/
--   jade/licenses/       license texts, copied to metadata/licenses/
--   jade/metadata/       other generator inputs (optional), copied to metadata/
--   jade/exports.txt     the public C API, read by the jade.bundle rule (rules/bundle.lua)
--
-- Called from a package's on_install, with the source tree as the current directory.
--
-- opt.commit    full commit hash of the pinned version
-- opt.exports   symbol names or `*` patterns to export, without the Mach-O underscore
-- opt.headers   patterns relative to <installdir>/include; `|` excludes, as in add_files
-- opt.licenses  {path in the source tree = staged file name}
-- opt.metadata  {path in the source tree = staged file name}, optional
-- opt.defines   defines that shape the public API; whoever parses the headers must use the same
function main(package, opt)
    import("core.base.json")

    assert(opt.commit and #opt.commit == 40, "package(" .. package:name() .. "): full commit hash required")
    assert(opt.exports and #opt.exports > 0, "package(" .. package:name() .. "): no exports")

    local stagedir = package:installdir("jade")
    io.writefile(path.join(stagedir, "exports.txt"), table.concat(opt.exports, "\n") .. "\n")

    local includedir = package:installdir("include")
    local count = 0
    for _, pattern in ipairs(opt.headers) do
        for _, file in ipairs(os.files(path.join(includedir, pattern))) do
            os.cp(file, path.join(stagedir, "include", path.relative(file, includedir)))
            count = count + 1
        end
    end
    assert(count > 0, "package(" .. package:name() .. "): no header matched")

    for source, name in pairs(opt.licenses) do
        os.cp(source, path.join(stagedir, "licenses", package:name(), name))
    end

    for source, name in pairs(opt.metadata or {}) do
        os.cp(source, path.join(stagedir, "metadata", name))
    end

    -- Only $(version) is used in our URLs, so a plain substitution resolves them.
    local url = package:urls()[1]:gsub("%$%(version%)", package:version_str())
    json.savefile(path.join(stagedir, "upstream.json"), {
        name = package:name(),
        version = package:version_str(),
        commit = opt.commit,
        url = url,
        sha256 = package:sourcehash(),
        license = package:license(),
        defines = json.mark_as_array(table.copy(opt.defines or {}))
    }, {pretty = true})
end
