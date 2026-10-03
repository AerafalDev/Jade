# 312: ImGui backend bindings and a sample

- Depends on: 311, 202, 203, 206
- ADRs: 0006, 0014

## Goal

The SDL3 and WebGPU backends are bound with types from `Jade.Interop.Sdl3` and
`Jade.Interop.WebGpu`; `samples/` shows ImGui's demo window over SDL3 and Dawn.

## Context

Promoted from draft I3 of the 301 survey (see its Outcome for the evidence).

13 and 16 backend functions (`cImGui_ImplSDL3_*`, `cImGui_ImplWGPU_*`); use
`InitForOther` with WebGPU; `ImGui_ImplWGPU_InitInfo` loses its C++ defaults (frames in flight 3,
multisample count 1, mask `0xFFFFFFFF`); the texture ID is a `WGPUTextureView`; IME needs
`SDL_HINT_IME_SHOW_UI` before the window is created.

## Scope

Cross-library type references in the generator, the backend bindings, a headless test
(SDL dummy video driver, Dawn's Null backend, rendering into an offscreen texture), the sample.

## Acceptance criteria

- [ ] The library builds into `jade_native` in `native.yml` for the six desktop RIDs, with an exact
      export list (no `*` pattern unless upstream publishes the exact list), and the export
      cross-check passes.
- [ ] Generated output is deterministic (two runs, same hash), the build has 0 warnings, the `style`
      job's commands pass, and the generated layout tests pass.
- [ ] Every test listed under Scope passes. Any skip says why (no display, no GPU).
- [ ] Licenses are in `THIRD-PARTY-NOTICES.md` and staged under `metadata/licenses/`.
- [ ] The Outcome reports the growth of `jade_native` per RID.
- [ ] The sample shows ImGui's demo window over SDL3 and Dawn on this machine, and the user confirms
      it visually: ask them, since the session cannot see the screen.

## Verification

Run the commands from CLAUDE.md: `scripts/build-native.cs`, `scripts/smoke-native.cs`,
`scripts/generate-bindings.cs` (twice, compare hashes), build, tests and the style checks.
`native.yml` runs the other desktop RIDs once the orchestrator pushes the branch; the Outcome says
which RIDs were built locally.

## Pitfalls

No multi-viewports with the WebGPU renderer; platform windows only on the `windows`,
`cocoa` and `x11` SDL drivers.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
