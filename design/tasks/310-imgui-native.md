# 310: Dear ImGui in jade_native

- Depends on: 103, 003
- ADRs: 0003, 0004, 0014

## Goal

`jade_native` contains Dear ImGui (docking), the dear_bindings C API and the SDL3, WebGPU and
null backends on the desktop RIDs in CI. It exports exactly the dear_bindings functions.

## Context

Promoted from draft I1 of the 301 survey (see its Outcome for the evidence).

ImGui `v1.92.9b-docking` tarball and `DearBindings_v0.24_ImGui_v1.92.9b-docking.zip`
(SHA-256 values in 301's Verification); re-check both before pinning, and take the dear_bindings
release built for the exact ImGui tag. Generated files fall under ImGui's MIT license (dear_bindings
README). Exports: the `name` of every function in `dcimgui.json` and in the three backend JSON
files (790 + 13 + 16 + 10 today), read in `on_install` like Dawn's list is read from its header.
`dcimgui.cpp` includes `dcimgui.h` inside `namespace cimgui`; follow the same pattern for any shim.

## Scope

Package `native/packages/i/imgui` (two resources), compile `imgui*.cpp` (demo included),
`dcimgui.cpp`, the three backends and their `dcimgui_impl_*.cpp`, with
`IMGUI_DISABLE_OBSOLETE_FUNCTIONS` and `IMGUI_IMPL_WEBGPU_BACKEND_DAWN`, against the `sdl3` and
`dawn` packages' headers. Stage `dcimgui.h`, the three backend headers, `imconfig.h` and the JSON
metadata. Record ImGui and dear_bindings versions in `versions.json`. Smoke check: create a context,
init the null backends, run one frame.

## Out of scope

Bindings, `dcimgui_internal`, extensions.

## Acceptance criteria

- [ ] Exports match the JSON lists on every desktop RID in CI, no C++ symbol exported, smoke check
      passes, licenses in `THIRD-PARTY-NOTICES.md`.
- [ ] Generated output, if any, stays deterministic, and the build and style checks pass.

## Verification

Run the commands from CLAUDE.md: `scripts/build-native.cs`, `scripts/smoke-native.cs`,
`scripts/generate-bindings.cs` (twice, compare hashes), build, tests and the style checks.
`native.yml` runs the other desktop RIDs once the orchestrator pushes the branch; the Outcome says
which RIDs were built locally.

## Pitfalls

On macOS `imgui_impl_wgpu.cpp` must build as Objective-C++ (Cocoa surface helper). The
dear_bindings wrappers warn `-Wunused-function`, so they must not get `jade_native`'s
warnings-as-errors. ImGui's `IM_ASSERT` aborts by default: decide whether `imconfig` routes it to a
handler. dear_bindings' release names contain the ImGui version; there is no "latest" alias.

## Outcome

- Summary: `jade_native` bundles Dear ImGui `v1.92.9b-docking`, built with
  `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`, the dear_bindings `v0.24` C API (`dcimgui.cpp`) and the SDL3,
  WebGPU (`IMGUI_IMPL_WEBGPU_BACKEND_DAWN`) and null backends with their dear_bindings wrappers. They
  come from the new package `native/packages/i/imgui`: the ImGui tarball plus the
  `DearBindings_v0.24_ImGui_v1.92.9b-docking.zip` resource, both SHA-256 values unchanged since 301.
  `jade_native` exports exactly the 791 functions of `dcimgui.json` and the three backend JSON files
  whose preprocessor conditionals hold for the build (753 + 13 + 15 + 10), and stages
  `include/imgui/` (`dcimgui.h`, the three backend headers, `imconfig.h`), `metadata/dcimgui*.json`,
  ImGui's license, and an `imgui` entry in `versions.json` with a new `resources` list that records
  dear_bindings. The smoke check runs ImGui frames on the null backends and compares every metadata
  function with the exports. `native.yml`'s export listing now counts the ImGui exports and fails on
  any name outside the bundled C APIs, such as a C++ symbol.
- Verification (commands and results), 2026-10-03, linux-x64 only (host CachyOS, clang; container
  clang 21.1.8 on glibc 2.28):
  - `dotnet scripts/build-native.cs`: miniaudio, sdl3, dawn and imgui installed once each (imgui's
    sdl3 and dawn dependencies resolve to the same builds), `libjade_native.so` 13,195 KiB.
  - `dotnet scripts/build-native.cs --rid linux-x64 --container`: new image (Dockerfile changed), every
    package rebuilt, 12,955 KiB, done in 403 s. On this library: highest glibc symbol `GLIBC_2.27`;
    `nm -D --defined-only` grouped as in `native.yml`: 38 `cImGui_`, 753 `dcimgui`, 1 `jade_`,
    1179 `ma_`, 1270 `SDL_`, 276 `wgpu`, no other name (so no C++ symbol); `strings` finds no
    `/home/` or `/root/` path.
  - `dotnet scripts/smoke-native.cs`: every check ok, among them `ImGui_GetVersion: 1.92.9b`,
    `DearBindings_GetVersion: 0.24`, `ImGui frames (null backends): rendered (1 draw list(s), 150
    vertices, 249 indices)` and `dear_bindings exports: 791 of 829`. No `imgui.ini` is left behind.
  - The new export listing of `native.yml`, run locally with `llvm-nm` on the artifacts of `main`'s
    native run 37093573668 (osx-arm64, osx-x64, linux-arm64) and on the linux-x64 above: no
    unexpected name; a mangled name fed to it is reported. `actionlint` passes.
  - `dotnet scripts/generate-bindings.cs` twice: the SHA-256 over every file under both `Generated/`
    folders is the same before, after the first run and after the second (`5bd471ce…`), 0 files
    changed.
  - `dotnet build -c Release`: 0 warnings, 0 errors. `SDL_VIDEO_DRIVER=dummy dotnet test -c Release`:
    101 of 101 passed. `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'`:
    exit 0. Script style check and `dotnet build` of `build-native` and `smoke-native`: no change, no
    warning.
  - Not verified here: win-x64, win-arm64, osx-x64, osx-arm64 and linux-arm64 (MSVC, Xcode, the
    Objective-C++ compile of `imgui_impl_wgpu.cpp`, the Windows and macOS link). `native.yml` builds
    and checks them once the branch is pushed. Nothing is drawn through SDL3 or WebGPU yet (312).
- Decisions taken (and ADRs added):
  - `IM_ASSERT` stays ImGui's `assert()`; `imconfig.h` is upstream's, staged unchanged. Release
    builds define `NDEBUG`, as miniaudio's do, so assertions compile out. Recoverable API misuse still
    goes through ImGui's error recovery: `IM_ASSERT_USER_ERROR` calls `ErrorLog` whatever `NDEBUG` is,
    which writes to ImGui's debug log and shows a tooltip, configured by `io.ConfigErrorRecovery*`.
    Debug builds abort, as upstream. A handler reporting to managed code, with a managed stack trace,
    can be added later as a `jade_imgui_*` shim without changing anything else.
  - The export list is read from the four JSON files in `on_install`, and their conditionals are
    evaluated against a table of the macros they test (`IMGUI_DISABLE_OBSOLETE_FUNCTIONS`,
    `IMGUI_IMPL_WEBGPU_BACKEND_DAWN`, `IMGUI_IMPL_WEBGPU_BACKEND_WGPU`, `IMGUI_HAS_IMSTR`,
    `IMGUI_DISABLE_DEBUG_TOOLS`, `__EMSCRIPTEN__`); another macro or form fails the install. The
    recipe then checks that the list equals the unmangled functions defined in the compiled objects,
    in both directions, so a wrong table entry fails the build instead of hiding a function. It
    reads the object files, not the archive: xmake 3.1.1's `binutils.readsyms` and `extractlib`
    returned only `imgui.cpp.o` and `dcimgui.cpp.o` of the 12 members, the two whose names fit
    without the GNU `//` long-name table.
  - The package version is the tag, `1.92.9b-docking`. dear_bindings is recorded as a resource of the
    imgui upstream: `stage.lua` takes `opt.resources`, `upstream.json` and `versions.json` gain a
    `resources` array (empty for the other upstreams), and `build-native` has `UpstreamResource`.
  - imgui depends on the sdl3 and dawn packages with the same `system` and `debug` settings as
    `native/xmake.lua`'s requires, so xmake reuses those installs; only their headers are used.
  - Default visibility (no `mode.release` rule) and compiler-default warnings, as for miniaudio; the
    wrappers' `-Wunused-function` warnings never meet `jade_native`'s warnings-as-errors.
  - `unzip` joins the Linux container: on Linux, xmake extracts zip archives only with unzip or 7z.
    README and CLAUDE.md list it among the native prerequisites.
  - `THIRD-PARTY-NOTICES.md`: Dear ImGui with Dear Bindings' generated files under its license, stb,
    ProggyClean and ProggyForever (both embedded in `imgui_draw.cpp`). The CC BY 4.0 kanji table of
    `imgui_draw.cpp` is not listed: it sits under `#ifndef IMGUI_DISABLE_OBSOLETE_FUNCTIONS` and is
    not compiled.
  - No ADR: nothing hard to reverse beyond what the brief and ADR-0014 cover.
- Deviations from the brief:
  - 791 exports, not 829 (790 + 13 + 16 + 10): 36 functions are obsolete API, absent with
    `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`; `ImStrv_FromCharStr` needs `IMGUI_HAS_IMSTR`; and
    `cImGui_ImplWGPU_GetLogLevelName` exists only with the wgpu-native backend. The dcimgui functions
    are not prefixed `cImGui_` either (only the backends are): they carry the C++ class name
    (`ImGui_`, `ImDrawList_`, `ImFontAtlas_` and 22 more) or `DearBindings_`.
  - The smoke check runs two frames, not one: ImGui hides a new window during its first frame while
    it measures it, so that frame has no draw list.
  - Outside the brief's file list: the Linux Dockerfile (unzip), the native prerequisites line of
    CLAUDE.md and README, and `stage.lua`. Editing `stage.lua` changes every package's recipe hash, so
    every package, Dawn included, rebuilds once, here and in CI's caches.
- Follow-ups:
  - xmake: `binutils.readsyms` and `extractlib` skip the GNU archive members named through the `//`
    table (checked on linux-x64 only; BSD and MSVC archives not tried). `jade.bundle` expands `*`
    patterns with `readsyms` on macOS and Windows: today's only package pattern, `ma_*`, sits in a
    short-named member, but a later package with patterns and long object names would silently lose
    exports there. Worth an upstream issue (an outward action, not taken).
  - CI still lists no Windows exports (since 103), so the new unexpected-name check covers Linux and
    macOS only. On Windows the `.def` holds exact names and ImGui has no `dllexport`, but a listing
    with whichever of `dumpbin /exports` or `llvm-readobj --coff-exports` the runners have would show
    it.
  - 311: `ExportCheck` filters exports by prefix; dcimgui needs the `Im*_` class prefixes,
    `DearBindings_` and `cImGui_`, and the 38 functions listed in the JSON but not exported must be
    excluded through their conditionals. `ImWchar` is 16-bit (`IMGUI_USE_WCHAR32` is not defined), which
    caps code points at U+FFFF; decide before binding, since it changes `ImWchar` and struct layouts.
  - 312: `imgui_impl_wgpu.cpp`'s Objective-C++ compile on macOS is only proven by CI's build; its
    surface helper has not run anywhere.
