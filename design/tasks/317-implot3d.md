# 317: ImPlot3D through our own C wrapper

- Depends on: 313
- ADRs: 0003, 0005, 0006, 0014

## Goal

ImPlot3D is in `jade_native` and bound, through a C API we produce ourselves rather than the
unlicensed cimplot3d. `float` and `double` plots come first.

## Context

- ADR-0014's table: ImPlot3D `v0.4` (2026-04-05), MIT, compiles against `v1.92.9b-docking` with and
  without `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`. It adds about +1,496 KiB with every numeric type and
  +433 KiB with `float` and `double` only. 301 pinned implot3d `41ae3e44` (cimplot3d's submodule).
  Re-check against the ImGui tag that 310 pins.
- Decided with the user on 2026-10-03: cimplot3d has no license, so it is neither vendored nor
  compiled. A C wrapper we produce is ours. Two routes:
  1. Run cimgui's generator (MIT, in the cimgui repository at the pinned `125f397e`) on ImPlot3D's
     headers ourselves, pinned, and commit or regenerate its output. Record the exact command.
  2. Write a `jade_imgui_implot3d` shim by hand in `native/shims/`.

  Choose one and justify it. Route 1 matches how 313 binds ImPlot (cimplot also comes from that
  generator), so the per-type overloads can reuse 313's handling of `definitions.json`.
- ImPlot3D has its own context besides ImGui's and ImPlot's.

## Scope

- Pinned package for ImPlot3D, with exact exports (our wrapper's functions only).
- The C wrapper, by route 1 or 2, limited to `float` and `double` instantiations, with the per-type
  functions mapped to C# overloads in the generator config, as 313 does for ImPlot.
- Tests: create and destroy an ImPlot3D context next to ImGui's; run one headless frame with the
  null backends that plots a small `float` line and a `double` scatter; the draw list must not be
  empty.

## Out of scope

- Other numeric types (a follow-up if the engine needs them).

## Acceptance criteria

- [ ] The library builds into `jade_native` in `native.yml` for the six desktop RIDs, with an exact
      export list, and the export cross-check passes.
- [ ] Generated output is deterministic (two runs, same hash), the build has 0 warnings, the `style`
      job's commands pass, and the generated layout tests pass.
- [ ] Every test listed under Scope passes. Any skip says why.
- [ ] Licenses are in `THIRD-PARTY-NOTICES.md` (ImPlot3D's MIT notice, plus cimgui's generator
      license if its output is committed) and staged under `metadata/licenses/`.
- [ ] The Outcome reports the growth of `jade_native` per RID and which route was taken.

## Verification

Run the commands from CLAUDE.md: `scripts/build-native.cs`, `scripts/smoke-native.cs`,
`scripts/generate-bindings.cs` (twice, compare hashes), build, tests and the style checks.
`native.yml` runs the other desktop RIDs once the orchestrator pushes the branch.

## Pitfalls

- cimplot3d returns the non-POD `ImPlot3DRay` by value with C linkage, an ABI risk on MSVC arm64.
  Our wrapper returns such values through an out pointer instead.
- Do not copy anything from cimplot3d, not even as a reference for names: produce the wrapper from
  ImPlot3D's own headers.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
