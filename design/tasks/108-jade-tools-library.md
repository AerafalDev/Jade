# 108: jade_tools: move Dear ImGui out of jade_native

- Depends on: 310
- ADRs: 0002, 0003, 0004, 0013, 0014, 0019

## Goal

Every desktop RID has two native libraries. `jade_native` no longer contains Dear ImGui.
`jade_tools` contains Dear ImGui, its dear_bindings C API and its SDL3, WebGPU and null backends,
and reaches SDL3 and Dawn through `jade_native` (ADR-0019). Both are packed, and
`Jade.Tools`/`Jade.Tools.Native` exist next to `Jade`/`Jade.Native`.

## Context

- ADR-0019 states the split and its hard constraint: one SDL and one Dawn per process. `jade_tools`
  must not embed its own copy of anything `jade_native` bundles.
- 310 bundled ImGui into `jade_native` (`native/packages/i/imgui/xmake.lua`, the `bundled` list in
  `native/xmake.lua`, the export check against dear_bindings' JSON, the smoke check's ImGui frame,
  and the `List exports` step of `native.yml`). Read its Outcome first.
- The `jade.bundle` rule (`native/rules/bundle.lua`) does whole-archive linking and export control
  per platform. Its exact export lists, version scripts, `.def` files and `-exported_symbols_list`
  apply to `jade_tools` as they do to `jade_native`.
- `src/Jade.Native` packs every RID staged under `artifacts/native/<rid>/lib/` (106). With two
  libraries, staging and packing must separate them.

## Scope

- xmake: a second target, `jade_tools`, bundling the `imgui` package (removed from `jade_native`'s
  `bundled` list). It links against `jade_native`:
  - Linux and Android: `NEEDED libjade_native.so` with an `$ORIGIN` rpath;
  - macOS: `@loader_path` install names;
  - Windows: the `jade_native.lib` import library.
  Both libraries come out of one `scripts/build-native.cs` run.
- Load order: show, on every desktop RID, that a .NET process P/Invoking into `jade_tools` first
  still resolves `jade_native`, both with `dotnet run` from the build output (`runtimes/<rid>/native/`)
  and as a published NativeAOT app. If one platform cannot, find the mechanism (for example a
  `NativeLibrary` resolver in the managed assembly) or stop and report (ADR-0019's fallback is the
  user's decision).
- Shared state check: in the smoke check, create the SDL window through `jade_native`, initialize
  ImGui's SDL3 backend through `jade_tools` on that window (dummy video driver), and render a frame.
  Confirm with the process's loaded modules that only one SDL and one Dawn exist.
- Staging: `artifacts/native/<rid>/` gets a layout that separates the two libraries, their headers,
  licenses and `versions.json` entries. Update `scripts/fetch-native.cs` and `native.yml` (artifacts,
  export listing per library, smoke check).
- Packaging: `src/Jade.Tools.Native` (same pattern as `src/Jade.Native`: required RIDs, size guard)
  and `src/Jade.Tools` (an empty managed library for now, packable, depending on `Jade` and
  `Jade.Tools.Native` at the same exact version). Extend `scripts/test-package.cs` and
  `tests/Jade.PackageTests` so a consumer of `Jade.Tools` renders an ImGui frame on the null
  backends, JIT and NativeAOT.
- Generator: if cheap, let a library config name its import library and target assembly. Otherwise
  leave it to 311 and say so.
- Update CLAUDE.md (layout, native rules), `design/architecture.md` (layers, packages, pipelines),
  README (Status, Packages) and CHANGELOG.

## Out of scope

- ImGui bindings (311), extensions (313 and later), mobile and browser specifics (104, 105).

## Acceptance criteria

- [ ] `jade_native` exports no ImGui symbol, and its size drops accordingly (report per RID).
- [ ] `jade_tools` exports exactly the dear_bindings functions 310 exported, and links to
      `jade_native` on every desktop RID (`ldd`, `otool -L`, `dumpbin /dependents`, or what the CI
      runners have).
- [ ] The shared-state smoke check passes on every desktop RID in `native.yml`.
- [ ] `test-package.cs` passes with `Jade.Tools` on linux-x64, JIT and NativeAOT.
- [ ] Build 0 warnings, tests green, style checks and `actionlint` clean.

## Verification

Local linux-x64 in the glibc 2.28 container, then `native.yml` on the six desktop RIDs once the
orchestrator pushes the branch. Report what each RID proved.

## Pitfalls

- Windows looks for a DLL's dependencies with the default search order, not in the directory .NET
  loaded `jade_tools` from. A `jade_native` already loaded by the process is reused, so load order
  may matter. Test both orders.
- Static CRT (`/MT`) on Windows gives each DLL its own heap. Memory allocated by `jade_native`
  (SDL, Dawn) and freed by `jade_tools`, or the reverse, would corrupt it. Check that ImGui's
  backends never free what SDL or Dawn allocated, or the other way round. If they do, stop and
  report.
- Changing the bundled list rebuilds packages; expect a cold CI build.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
