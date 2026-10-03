# 316: imgui_freetype

- Depends on: 303, 310
- ADRs: 0014

## Goal

ImGui rasterizes fonts through 303's FreeType (`IMGUI_ENABLE_FREETYPE`); decide whether SVG
color fonts (plutosvg or lunasvg, a new upstream) are worth it.

## Context

Promoted from draft I7 of the 301 survey (see its Outcome for the evidence).

Scope: build ImGui with `IMGUI_ENABLE_FREETYPE` against 303's FreeType package, check that a font
atlas builds and renders glyphs through FreeType in a headless test, and report the size difference.
SVG color fonts need a new upstream (plutosvg or lunasvg): write the trade-off in the Outcome and
ask the user before adding one.

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

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
