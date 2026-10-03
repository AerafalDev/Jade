# 315: ImGuiColorTextEdit

- Depends on: 311
- ADRs: 0014

## Goal

Goossens' ImGuiColorTextEdit `v1.92.9` behind a `jade_imgui_texteditor` shim, bound and tested.

## Context

Promoted from draft I6 of the 301 survey (see its Outcome for the evidence).

MIT, C++17, `std::string`-based API, about 400 KiB. cimCTE exists but has no license and
pulls the example fonts.

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

Text crosses the boundary as UTF-8 spans; returning text means a length query plus a
copy into a caller buffer, since generated code never allocates.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
