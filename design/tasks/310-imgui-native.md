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

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
