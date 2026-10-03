# 314: Small ImGui widgets and imnodes

- Depends on: 311
- ADRs: 0014

## Goal

imgui-knobs, imgui_toggle and imgui_memory_editor (shim `jade_imgui_widgets`), imgui_markdown (shim
`jade_imgui_markdown`), imspinner (its upstream `cimspinner/`) and imnodes (shim
`jade_imgui_imnodes`) are in `jade_native` and bound.

## Context

Promoted from draft I5 of the 301 survey (see its Outcome for the evidence).

Revisions and licenses in ADR-0014's table (imgui_toggle is 0BSD, imgui_markdown Zlib).
imgui_markdown and the memory editor are header-only, compiled in the shim's translation unit.

Decided with the user on 2026-10-03: imnodes goes through a `jade_imgui_imnodes` shim in this task
(cimnodes has no license, and no issue is opened to ask for one).

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

Cimspinner exports unprefixed `Spinner*` names and takes `ImColor` by value. The markdown
config holds callbacks and one `ImFont*` per heading level, and 1.92 fonts take a size in
`PushFont`.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
