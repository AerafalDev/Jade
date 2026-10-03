# 313: ImPlot and ImGuizmo

- Depends on: 311
- ADRs: 0014

## Goal

ImPlot and ImGuizmo are in `jade_native` and bound through their licensed cimgui wrappers.

## Context

Promoted from draft I4 of the 301 survey (see its Outcome for the evidence).

Pins cimgui `125f397e` (only `cimgui.h`), cimplot `11f13e6c` with implot `1351ab2c`,
cimguizmo `eaf7d7b0` with ImGuizmo `dc25afb9`, and if licensed cimplot3d `8d04820c` with implot3d
`41ae3e44` and cimnodes `e8502aff` with imnodes `c9bb8e9b` (`-DIMNODES_NAMESPACE=imnodes`).
Re-check against the ImGui tag of 310. The wrappers' ImGui types are mapped to the ImGui bindings
by C name, with a layout cross-check (equal with `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`). cimgui's
`definitions.json` (`funcname`, `ov_cimguiname`, `defaults`, `argsoriginal`) lets per-type
functions such as `ImPlot_PlotLine_FloatPtrInt` become C# overloads. Compile `implot_demo.cpp` or
exclude `ImPlot_ShowDemoWindow`.

Decided with the user on 2026-10-03: no unlicensed wrapper is used. ImPlot3D comes in 317
through a C wrapper we produce ourselves, and imnodes through our own shim in 314. This
task binds ImPlot and ImGuizmo only.

## Acceptance criteria

- [ ] The library builds into `jade_native` in `native.yml` for the six desktop RIDs, with an exact
      export list (no `*` pattern unless upstream publishes the exact list), and the export
      cross-check passes.
- [ ] Generated output is deterministic (two runs, same hash), the build has 0 warnings, the `style`
      job's commands pass, and the generated layout tests pass.
- [ ] Every test listed under Scope passes. Any skip says why (no display, no GPU).
- [ ] Licenses are in `THIRD-PARTY-NOTICES.md` and staged under `metadata/licenses/`.
- [ ] The Outcome reports the growth of `jade_native` per RID.

## Verification

Run the commands from CLAUDE.md: `scripts/build-native.cs`, `scripts/smoke-native.cs`,
`scripts/generate-bindings.cs` (twice, compare hashes), build, tests and the style checks.
`native.yml` runs the other desktop RIDs once the orchestrator pushes the branch; the Outcome says
which RIDs were built locally.

## Pitfalls

ImPlot is 5.8 MB per RID with ten numeric types. cimplot3d returns the non-POD
`ImPlot3DRay` with C linkage, an ABI risk on MSVC arm64. ImPlot and ImPlot3D have their own
contexts besides ImGui's.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
