# 301: Survey: Dear ImGui, extensions and backends

- Depends on: 201, 003
- ADRs: 0003, 0005, 0006

## Goal

A Proposed ADR and draft briefs: which Dear ImGui branch, which extensions, how each gets a C API
for the generator, and which backends (SDL3 platform, WebGPU renderer) run natively or in C#. The
user wants ImGui "with all its extensions". This survey turns that into a concrete, justified list.

## Context

- Dear ImGui `v1.92.9b` (2026-10-02). The docking branch has its own releases: compare.
- `dearimgui/dear_bindings` generates a C API (`dcimgui`) from ImGui headers, with metadata JSON
  documented in `docs/MetadataFormat.md`. That JSON may be a better generator input than libclang:
  evaluate it.
- Upstream backends `imgui_impl_sdl3` and `imgui_impl_wgpu` are C++.

## Scope

Research only; no code beyond throwaway experiments.

- Candidate extensions to evaluate: ImPlot, ImPlot3D, ImGuizmo, imnodes and imgui-node-editor,
  ImGuiColorTextEdit, imgui_markdown, file dialogs, knobs/toggles/spinners. Add any other widely
  used ones.
- For each: license, maintenance (last release, activity), ImGui version compatibility, C API
  availability (dear_bindings support or a cimgui-family project), shim cost, wasm compatibility,
  binary size.
- Backends: compile the C++ backends into jade_native behind a C shim, or port them to C# over our
  bindings. Weigh correctness, maintenance, web and mobile behaviour.
- The generator path: dear_bindings JSON reader vs libclang over `dcimgui.h`.

## Out of scope

- Implementation (follow-up tasks).

## Acceptance criteria

- [x] A Proposed ADR `design/adr/NNNN-imgui.md` with the decision table and sources.
- [x] Draft briefs in the Outcome, which the orchestrator will finalize: one per implementation step.

## Verification

Every claim about a project (license, version, activity, API) links to its source and is dated.

## Pitfalls

- ImGui's API breaks between versions. Extensions often lag. Choose a version every selected
  extension supports.

## Outcome

- Summary: [ADR-0014](../adr/0014-imgui.md) (Proposed) settles the survey. Dear ImGui's docking
  branch at `v1.92.9b-docking`, built with `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`. Its C API and the
  SDL3, WebGPU and null backends' C wrappers come from dear_bindings' prebuilt release for that tag,
  pinned by SHA-256. The upstream C++ backends are compiled into `jade_native`, not ported to C#.
  The generator reads `dcimgui.h` through libclang and uses `dcimgui.json` as annotations. Selected
  extensions: ImPlot, ImGuizmo, imnodes, ImGuiColorTextEdit (goossens), imgui_markdown, imgui-knobs,
  imgui_toggle, imspinner and imgui_memory_editor. ImPlot3D comes as soon as its C wrapper has a
  license. Native file dialogs come from SDL3. ImGuiFileDialog is optional, imgui-node-editor is
  deferred (upstream does not compile with ImGui 1.92.7 or later) and imgui_freetype waits for 303.
  Each extension gets its C API from an upstream C API, a licensed cimgui-family wrapper, or a
  `jade_imgui_*` shim, in that order. Seven draft briefs follow. Nothing is committed beyond `design/`;
  the experiments ran in a scratch directory.
- Verification (commands and results), all on 2026-10-03, linux-x64 (CachyOS, clang 23.1.1,
  Python 3.14.7, zig 0.16.0, Docker `emscripten/emsdk:6.0.11`):
  - Project facts: `gh api repos/<owner>/<repo>` (license, archived), `.../releases`, `.../tags`,
    `.../commits?since=2025-10-03T00:00:00Z` counted with `--paginate`, and each license file read
    from a shallow clone. Results are in ADR-0014's tables. ImGui tags `v1.92.0` to `v1.92.9b` each
    have a `-docking` twin (`gh api repos/ocornut/imgui/git/matching-refs/tags/v1.92`).
  - Pinned archives: `sha256sum` gives
    `90ded916bd57db2e0e171b6b098940a47c6f5042725dcdc67fb19940ca8bfdcc` for
    `https://github.com/ocornut/imgui/archive/refs/tags/v1.92.9b-docking.tar.gz` and
    `ee7a51d4220b5ba28672253dc95e287d9e00ae9f9a5321725d8c3b978789694b` for
    `DearBindings_v0.24_ImGui_v1.92.9b-docking.zip` (release
    `DearBindings_v0.24_ImGui_v1.92.9b-docking`). The zip holds `dcimgui.{h,cpp,json}`,
    `dcimgui_internal.*`, `dcimgui_nodefaultargfunctions*` and `backends/dcimgui_impl_*`
    (`sdl3`, `wgpu` and `null` among them). Its JSON: 790 functions in `dcimgui.json`, 829 in
    `dcimgui_internal.json`, 13, 16 and 10 in the `sdl3`, `wgpu` and `null` backends (all prefixed
    `cImGui_`).
  - dear_bindings run locally:
    `uv run --with ply python dear_bindings.py -o <out>/dcimgui ../imgui/imgui.h` takes 3.0 s;
    `--backend --include ../imgui/imgui.h` converts `imgui_impl_sdl3.h` and `imgui_impl_wgpu.h`.
    The same `--backend` run on 13 extension headers fails for all of them: a missing
    `<name>-header-template.h` (knobs, imnodes, node-editor, markdown, ImCoolBar, imspinner, memory
    editor), `AttributeError: 'float' object has no attribute 'len'` in `mod_flatten_namespaces`
    (toggle, ImGuizmo, ImPlot3D, ImGuiFileDialog), a parse exception (ImPlot) and
    `std::vector<std::u32string_view>` (ImGuiColorTextEdit).
  - `dcimgui.json` survey (with `jq`): 777 functions without and 790 with
    `--generateunformattedfunctions` (the release uses it: 13 `*Unformatted` helpers), 108
    default-argument helpers, 15 varargs functions, 71 structs, 44 enums, 66 typedefs, 34 defines;
    bit-fields `ImFontGlyph.{Colored,Visible,SourceIdx,Codepoint}` and
    `ImFontBaked.{MetricsTotalSurface,WantDestroy,LoadNoFallback,LoadNoRenderOnLayout}`; anonymous
    members in `ImGuiStoragePair` and `ImFontAtlas`; arrays of structs such as `ImGuiStyle.Colors`
    and `ImGuiIO.KeysData`; by-value structs `ImVec2`, `ImVec4`, `ImTextureRef` and `ImColor` in 143
    functions; 66 elements under `#ifndef IMGUI_DISABLE_OBSOLETE_FUNCTIONS`.
  - Compile matrix: a throwaway script compiled each project with
    `clang++ -std=c++17 -O2 -fPIC -fvisibility=hidden -ffunction-sections -fdata-sections -I imgui -c`
    against `v1.92.9b-docking`, at each default-branch head. Everything compiles, with no warning at
    clang's default level, except
    upstream imgui-node-editor (`imgui_extra_math.inl:34: redefinition of 'operator*'`, defined by
    `imgui.h` since v1.92.7, found by fetching `imgui.h` of each tag: absent in v1.92.6, present in
    v1.92.7) and ImNodeFlow, which needs `-DIMGUI_DEFINE_MATH_OPERATORS`. The Dear ImGui Bundle fork
    of imgui-node-editor (`ed0fe7a1`) compiles. With `-DIMGUI_DISABLE_OBSOLETE_FUNCTIONS`, two more fail
    besides node-editor: BalazsJako's ImGuiColorTextEdit (`GetWindowContentRegionMax`) and ImNodeFlow
    (`CmdListsCount`).
  - Release tags, same flags: ImPlot3D `v0.4`, ImGuizmo `1.10`, ImGuiColorTextEdit `v1.92.9` and
    ImGuiFileDialog `v0.6.8` compile with and without obsolete functions. ImPlot `v1.0` compiles only
    with them (`call to deleted member function 'AddRect'`, `'AddPolyline'`); imnodes `v0.5` (2022)
    fails (`Please '#define IMGUI_DEFINE_MATH_OPERATORS'`).
  - Cross targets, at the heads: the same set with `em++` 6.0.11 in Docker, plus
    `imgui_impl_sdl3.cpp` and `imgui_impl_wgpu.cpp -DIMGUI_IMPL_WEBGPU_BACKEND_DAWN
    --use-port=emdawnwebgpu`: all compile except upstream node-editor. `zig c++ -target
    x86_64-windows-gnu` and `-target aarch64-macos`: same result (zig leaves empty `.o` files on
    failure, so objects were counted by non-zero size). The Windows and macOS runs exercise the
    `_WIN32` and `__APPLE__` paths but are not MSVC or Xcode builds.
  - Backends against what `jade_native` stages (`artifacts/native/linux-x64/include`, SDL 3.4.16 and
    Dawn `20260930.214659` per `versions.json`): `clang++ -std=c++17 -O2 -Wall -Wextra
    -DIMGUI_IMPL_WEBGPU_BACKEND_DAWN` on both backends, exit 0, no warning. The dear_bindings wrappers
    `dcimgui_impl_sdl3.cpp` and `dcimgui_impl_wgpu.cpp` compile with only `-Wunused-function` warnings
    (unused `ConvertFromCPP_*` helpers).
  - libclang view of the C headers: `clang -fsyntax-only -x c -std=c11 --target=<triple> -nostdinc
    -isystem <copy of scripts/generate-bindings/sysroot>` for the 12 ADR-0007 triples. Without an
    `assert.h` stub every header stops at `'assert.h' file not found`. With it: `dcimgui.h` and
    `dcimgui_impl_sdl3.h` 0 errors on 12 triples; `dcimgui_impl_wgpu.h` 0 errors on the 11 native
    triples, and on wasm Dawn's `dawn/webgpu.h` refuses Emscripten ("Use the headers provided by
    Emdawnwebgpu"); `dcimgui_internal.h` (parsed after `dcimgui.h`, with `-DIMGUI_DISABLE_SSE`) needs
    `FILE` from the `stdio.h` stub, plus `math.h` and `limits.h` stubs. The cimgui-family headers
    (`-DCIMGUI_DEFINE_ENUMS_AND_STRUCTS`) parse with 0 errors on 12 triples once `stdio.h` declares
    `FILE` and a `time.h` stub defines `struct tm`.
  - cimgui versus dcimgui layouts: `sizeof` probes for the 69 complete public structs of
    `dcimgui.json`, dumped with `-Xclang -fdump-record-layouts-simple` for x86_64 Linux, arm64 Windows
    and wasm32. With `-DIMGUI_DISABLE_OBSOLETE_FUNCTIONS` on the dcimgui side, sizes and field offsets
    are identical (the one reported difference is two same-named records of `ImGuiSizeCallbackData`,
    whose sets match). Without it, `ImDrawData`, `ImFont`, `ImFontAtlas`, `ImFontConfig` and `ImGuiIO`
    differ: cimgui's generator always preprocesses with `-DIMGUI_DISABLE_OBSOLETE_FUNCTIONS`
    (`generator/generator.lua:42`).
  - cimgui-family wrappers, at their own commits with their submodules, against `v1.92.9b-docking`
    and `cimgui.h` of cimgui `125f397e`: all six compile with and without obsolete functions on
    linux-x64 and with `em++` 6.0.11 (cimnodes needs `-DIMNODES_NAMESPACE=imnodes`, as its README
    says). Warnings: cimplot3d `'ImPlot3D_PixelsToPlotRay_*' has C-linkage specified, but returns
    user-defined type 'ImPlot3DRay'`, cimguizmo `'SetID' is deprecated`. Linking each with ImGui,
    `imgui_demo.cpp` and `dcimgui.cpp` under `-Wl,--no-undefined`: cimguizmo, cimnodes and
    cimnodes_editor resolve fully; cimplot and cimplot3d miss only their demo functions; cimCTE misses
    `notosans` and `dejavu` (fonts from the editor's `example/` folder). Nothing needs `cimgui.cpp`.
  - Sizes: objects rebuilt with `-fvisibility=default`, then `clang++ -shared -fuse-ld=lld
    -Wl,--gc-sections -s` over ImGui core plus `dcimgui.cpp` (1,245 KiB), each extension's growth
    measured on top. Demo +285, `dcimgui_internal` +101, ImPlot +5,836 (+1,490 with
    `-DIMPLOT_CUSTOM_NUMERIC_TYPES=(float)(double)`), ImPlot3D +1,496 (+433), ImGuizmo +89 (its other
    widgets +62), imnodes +54, node-editor fork +311, ImGuiColorTextEdit goossens +397, BalazsJako
    +343, ImGuiFileDialog +399, knobs +13, toggle +17, cimspinner +58, ImNodeFlow +20, ImCoolBar +9,
    imgui_markdown +8 and memory editor +12 (one call site each), all in KiB. With their wrappers:
    cimplot +5,901, cimplot3d +1,544, cimguizmo +94, cimnodes +66, cimnodes_editor +311, cimCTE +480.
    The backends are 15 KB and 16 KB of code (`size`). gzip -9 shrinks ImPlot's growth to about
    0.93 MB.
  - The cimgui generator's template list: `generator/generator.lua:132` of cimplot lists the ten
    numeric types; 324 of cimplot's 792 functions are per-type expansions.
  - Prior art and docs: Hexa.NET.ImGui's README (cimgui for the C interface, natively compiled
    backends in a separate package); Microsoft Learn, "ASP.NET Core Blazor WebAssembly native
    dependencies" (.NET 10 view), for the browser-wasm notes in Follow-ups.
  - Not verified: anything on MSVC, Xcode, Android or iOS toolchains; running any of this (no window,
    no GPU, no browser); by-value struct P/Invoke on browser-wasm; whether .NET's wasm link drops
    unused exports from `jade_native.a`.
- Decisions taken (and ADRs added):
  - [ADR-0014](../adr/0014-imgui.md), Proposed, with the decision table and sources. Added to
    `design/adr/README.md`.
  - Docking branch, `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`, one ImGui version for every extension.
  - dear_bindings prebuilt release for the core and backends (not cimgui): comments for XML docs,
    richer metadata, WebGPU backend included. The default variant (with default-argument helpers) is
    preferred over `dcimgui_nodefaultargfunctions`: every helper is a real export, so ADR-0006's raw
    1:1 layer stays callable without emulating C++ defaults.
  - libclang stays the declaration and layout source; `dcimgui.json` is an annotation input of the
    ImGui config.
  - Native backends (SDL3, WebGPU with `IMGUI_IMPL_WEBGPU_BACKEND_DAWN`, null) in `jade_native`.
  - Extension C API routes in order: upstream C API, licensed cimgui-family wrapper (extension pinned
    to the wrapper's submodule commit), `jade_imgui_*` shim. A C++ reader in our generator is the
    documented fallback.
- Deviations from the brief:
  - Windows and macOS compatibility was checked with zig's clang (mingw and macOS targets), not MSVC
    or Xcode; Android and iOS were not compiled (no NDK or SDK here).
  - Sizes were measured at default-branch heads, not at the revisions proposed for pinning, and on
    linux-x64 only.
  - "file dialogs" became SDL3's native dialogs plus an optional ImGuiFileDialog, rather than a
    required ImGui widget.
- Follow-ups:
  - User: four cimgui-family wrappers have no license file (cimplot3d, cimnodes, cimnodes_editor,
    cimCTE). Asking their author to add one (an issue on each repository) unblocks ImPlot3D and
    simplifies imnodes. This is an outward action, so it was not taken.
  - 105: the .NET 10 page "ASP.NET Core Blazor WebAssembly native dependencies" says that on
    WebAssembly, `[DllImport]` signatures should use `IntPtr` instead of `delegate* unmanaged` for
    function pointers (dotnet/runtime#56145). That touches ADR-0006's callback rule for every library,
    not just ImGui; check it on browser-wasm. Also check by-value struct arguments (143 ImGui
    functions) there; dear_bindings' `--nopassingstructsbyvalue` is the fallback, at the cost of
    running dear_bindings ourselves.
  - 105: the WebGPU backend needs Emscripten 4.0.10 or later with emdawnwebgpu; only 6.0.11 was
    tried. ImPlot's 5.8 MB is the size to watch for browser downloads.
  - 104: on Android and iOS the SDL3 backend disables platform windows and global mouse state by
    design; touch arrives as mouse events with a touch source. Nothing to build until 104 lands.
  - Generator (first ImGui task): sysroot stubs `assert.h`, `FILE` in `stdio.h`, `math.h`, `limits.h`
    and, for cimgui-family headers, `time.h`; the features listed in ADR-0014's Consequences.
  - Orchestrator: `design/architecture.md`'s upstream snapshot could list dear_bindings `v0.24` and
    cimgui `125f397e`; the roadmap needs rows for the drafts below once ADR-0014 is accepted.
  - Orchestrator: the 304 survey, run in parallel, committed `design/adr/0013-asset-libraries.md` on
    its branch first, so this ADR takes 0014. Both branches add a row after 0012 in
    `design/adr/README.md`, which conflicts on merge; keep both rows.
  - The scratch experiments pulled the Docker image `emscripten/emsdk:6.0.11` (about 830 MB
    compressed); remove it with `docker rmi emscripten/emsdk:6.0.11` if unused.

### Draft briefs

Drafts are labelled I1 to I7 (the 304 survey uses A to F); the orchestrator assigns task numbers.
Other dependencies use the roadmap's numbering. Each draft lists what its session cannot cheaply
rediscover; the orchestrator finalizes the template fields.

#### Draft I1: Dear ImGui in jade_native

- Depends on: 103, 003. ADRs: 0003, 0004, 0014.
- Goal: `jade_native` contains Dear ImGui (docking), the dear_bindings C API and the SDL3, WebGPU and
  null backends on the desktop RIDs in CI. It exports exactly the dear_bindings functions.
- Context: ImGui `v1.92.9b-docking` tarball and `DearBindings_v0.24_ImGui_v1.92.9b-docking.zip`
  (SHA-256 values in 301's Verification); re-check both before pinning, and take the dear_bindings
  release built for the exact ImGui tag. Generated files fall under ImGui's MIT license (dear_bindings
  README). Exports: the `name` of every function in `dcimgui.json` and in the three backend JSON
  files (790 + 13 + 16 + 10 today), read in `on_install` like Dawn's list is read from its header.
  `dcimgui.cpp` includes `dcimgui.h` inside `namespace cimgui`; follow the same pattern for any shim.
- Scope: package `native/packages/i/imgui` (two resources), compile `imgui*.cpp` (demo included),
  `dcimgui.cpp`, the three backends and their `dcimgui_impl_*.cpp`, with
  `IMGUI_DISABLE_OBSOLETE_FUNCTIONS` and `IMGUI_IMPL_WEBGPU_BACKEND_DAWN`, against the `sdl3` and
  `dawn` packages' headers. Stage `dcimgui.h`, the three backend headers, `imconfig.h` and the JSON
  metadata. Record ImGui and dear_bindings versions in `versions.json`. Smoke check: create a context,
  init the null backends, run one frame.
- Out of scope: bindings, `dcimgui_internal`, extensions.
- Acceptance: exports match the JSON lists on every desktop RID in CI, no C++ symbol exported, smoke
  check passes, licenses in `THIRD-PARTY-NOTICES.md`.
- Pitfalls: on macOS `imgui_impl_wgpu.cpp` must build as Objective-C++ (Cocoa surface helper). The
  dear_bindings wrappers warn `-Wunused-function`, so they must not get `jade_native`'s
  warnings-as-errors. ImGui's `IM_ASSERT` aborts by default: decide whether `imconfig` routes it to a
  handler. dear_bindings' release names contain the ImGui version; there is no "latest" alias.

#### Draft I2: Dear ImGui core bindings

- Depends on: I1, 201. ADRs: 0005, 0006, 0012, 0014.
- Goal: `Jade.Interop.ImGui` generated from `dcimgui.h` and `dcimgui_impl_null.h`, annotated by their
  JSON, with layout tests and a headless frame test.
- Context: the `dcimgui.json` figures in 301's Verification (bit-fields, anonymous members, struct
  arrays, varargs, unformatted helpers, by-value structs, conditionals). Parsing needs an `assert.h`
  stub. `ImTextureID` is `ImU64`, `ImWchar` 16 bits, `ImDrawIdx` 16 bits by default.
- Scope: generator features (bit-field accessors, anonymous members, `[InlineArray]` struct arrays,
  callback typedefs, JSON annotations through source-generated `System.Text.Json`); ImGui config
  (names from `original_fully_qualified_name`, `ref`/`in`/`out` from `is_reference`, struct defaults
  from `default_value`, varargs excluded); tests: context, null backends, `NewFrame`, a window with
  `TextUnformatted`, `Render`, draw-list counts.
- Out of scope: SDL3 and WebGPU backends (I3), internal API, extensions.
- Pitfalls: ImGui state is global and not thread-safe, so its tests share one class. Decide how
  transparent structs reached through pointers (`ImDrawList*`, `ImGuiIO*`) get instance methods;
  ADR-0006's handles are for opaque types. Default-argument helpers and `Ex` functions must not
  collide once C++ names are restored.

#### Draft I3: ImGui backend bindings and a sample

- Depends on: I2, 202, 203, 206. ADRs: 0006, 0014.
- Goal: the SDL3 and WebGPU backends are bound with types from `Jade.Interop.Sdl3` and
  `Jade.Interop.WebGpu`; `samples/` shows ImGui's demo window over SDL3 and Dawn.
- Context: 13 and 16 backend functions (`cImGui_ImplSDL3_*`, `cImGui_ImplWGPU_*`); use
  `InitForOther` with WebGPU; `ImGui_ImplWGPU_InitInfo` loses its C++ defaults (frames in flight 3,
  multisample count 1, mask `0xFFFFFFFF`); the texture ID is a `WGPUTextureView`; IME needs
  `SDL_HINT_IME_SHOW_UI` before the window is created.
- Scope: cross-library type references in the generator, the backend bindings, a headless test
  (SDL dummy video driver, Dawn's Null backend, rendering into an offscreen texture), the sample.
- Pitfalls: no multi-viewports with the WebGPU renderer; platform windows only on the `windows`,
  `cocoa` and `x11` SDL drivers.

#### Draft I4: ImPlot and ImGuizmo through their cimgui wrappers

- Depends on: I2. ADRs: 0014.
- Goal: ImPlot and ImGuizmo in `jade_native` and bound; ImPlot3D and imnodes too if their wrappers
  have a license by then.
- Context: pins cimgui `125f397e` (only `cimgui.h`), cimplot `11f13e6c` with implot `1351ab2c`,
  cimguizmo `eaf7d7b0` with ImGuizmo `dc25afb9`, and if licensed cimplot3d `8d04820c` with implot3d
  `41ae3e44` and cimnodes `e8502aff` with imnodes `c9bb8e9b` (`-DIMNODES_NAMESPACE=imnodes`).
  Re-check against the ImGui tag of I1. The wrappers' ImGui types are mapped to the ImGui bindings
  by C name, with a layout cross-check (equal with `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`). cimgui's
  `definitions.json` (`funcname`, `ov_cimguiname`, `defaults`, `argsoriginal`) lets per-type
  functions such as `ImPlot_PlotLine_FloatPtrInt` become C# overloads. Compile `implot_demo.cpp` or
  exclude `ImPlot_ShowDemoWindow`.
- Pitfalls: ImPlot is 5.8 MB per RID with ten numeric types. cimplot3d returns the non-POD
  `ImPlot3DRay` with C linkage, an ABI risk on MSVC arm64. ImPlot and ImPlot3D have their own
  contexts besides ImGui's.

#### Draft I5: Small widgets

- Depends on: I2. ADRs: 0014.
- Goal: imgui-knobs, imgui_toggle, imgui_memory_editor (shim `jade_imgui_widgets`), imgui_markdown
  (shim `jade_imgui_markdown`) and imspinner (its upstream `cimspinner/`) in `jade_native` and bound;
  imnodes through a `jade_imgui_imnodes` shim if I4 could not use cimnodes.
- Context: revisions and licenses in ADR-0014's table (imgui_toggle is 0BSD, imgui_markdown Zlib).
  imgui_markdown and the memory editor are header-only, compiled in the shim's translation unit.
- Pitfalls: cimspinner exports unprefixed `Spinner*` names and takes `ImColor` by value. The markdown
  config holds callbacks and one `ImFont*` per heading level, and 1.92 fonts take a size in
  `PushFont`.

#### Draft I6: ImGuiColorTextEdit

- Depends on: I2. ADRs: 0014.
- Goal: goossens' ImGuiColorTextEdit `v1.92.9` behind a `jade_imgui_texteditor` shim, bound and tested.
- Context: MIT, C++17, `std::string`-based API, about 400 KiB. cimCTE exists but has no license and
  pulls the example fonts.
- Pitfalls: text crosses the boundary as UTF-8 spans; returning text means a length query plus a
  copy into a caller buffer, since generated code never allocates.

#### Draft I7: imgui_freetype

- Depends on: 303, I1. ADRs: 0014.
- Goal: ImGui rasterizes fonts through 303's FreeType (`IMGUI_ENABLE_FREETYPE`); decide whether SVG
  color fonts (plutosvg or lunasvg, a new upstream) are worth it.

Not drafted, for later: ImGuiFileDialog through its `IGFD_*` C API; imgui-node-editor once upstream
compiles with the pinned ImGui (or a fork is chosen deliberately); `dcimgui_internal` for the
DockBuilder API.
