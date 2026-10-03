# 311: Dear ImGui core bindings

- Depends on: 310, 201
- ADRs: 0005, 0006, 0012, 0014

## Goal

`Jade.Interop.ImGui` generated from `dcimgui.h` and `dcimgui_impl_null.h`, annotated by their
JSON, with layout tests and a headless frame test.

## Context

Promoted from draft I2 of the 301 survey (see its Outcome for the evidence).

The `dcimgui.json` figures in 301's Verification (bit-fields, anonymous members, struct
arrays, varargs, unformatted helpers, by-value structs, conditionals). Parsing needs an `assert.h`
stub. `ImTextureID` is `ImU64`, `ImWchar` 16 bits, `ImDrawIdx` 16 bits by default.

## Scope

Generator features (bit-field accessors, anonymous members, `[InlineArray]` struct arrays,
callback typedefs, JSON annotations through source-generated `System.Text.Json`); ImGui config
(names from `original_fully_qualified_name`, `ref`/`in`/`out` from `is_reference`, struct defaults
from `default_value`, varargs excluded); tests: context, null backends, `NewFrame`, a window with
`TextUnformatted`, `Render`, draw-list counts.

## Out of scope

SDL3 and WebGPU backends (312), internal API, extensions.

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

- Decided with the user on 2026-10-03 (ADR-0014): `ImWchar` is 32-bit. Add `IMGUI_USE_WCHAR32` to
  the ImGui recipe (it changes layouts, so the native build and the bindings move together), and
  check that dear_bindings' headers honour it.
- From 310: `ExportCheck` filters exports by prefix. dcimgui needs the `Im*_` class prefixes,
  `DearBindings_` and `cImGui_`, and the 38 functions listed in the JSON but not exported (obsolete
  API, `IMGUI_HAS_IMSTR`, wgpu-native-only) must be excluded through their conditionals.
ImGui state is global and not thread-safe, so its tests share one class. Decide how
transparent structs reached through pointers (`ImDrawList*`, `ImGuiIO*`) get instance methods;
ADR-0006's handles are for opaque types. Default-argument helpers and `Ex` functions must not
collide once C++ names are restored.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
