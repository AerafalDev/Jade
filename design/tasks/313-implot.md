# 313: ImPlot

- Depends on: 311
- ADRs: 0014, 0018

## Goal

ImPlot is in `jade_native` and bound through its licensed cimgui wrapper (cimplot).

## Context

Promoted from draft I4 of the 301 survey (see its Outcome for the evidence).

Pins cimgui `125f397e` (only `cimgui.h`) and cimplot `11f13e6c` with implot `1351ab2c`.
Re-check against the ImGui tag of 310. The wrappers' ImGui types are mapped to the ImGui bindings
by C name, with a layout cross-check (equal with `IMGUI_DISABLE_OBSOLETE_FUNCTIONS`). cimgui's
`definitions.json` (`funcname`, `ov_cimguiname`, `defaults`, `argsoriginal`) lets per-type
functions such as `ImPlot_PlotLine_FloatPtrInt` become C# overloads. Compile `implot_demo.cpp` or
exclude `ImPlot_ShowDemoWindow`.

Decided with the user on 2026-10-03: Jade is a 2D engine (ADR-0018), so ImGuizmo (a 3D gizmo) and
ImPlot3D are out of the plan; imnodes comes through our own shim in 314. This task binds ImPlot
only.

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

ImPlot is 5.8 MB per RID with ten numeric types. ImPlot has its own context besides ImGui's.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
