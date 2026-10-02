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

- [ ] A Proposed ADR `design/adr/NNNN-imgui.md` with the decision table and sources.
- [ ] Draft briefs in the Outcome, which the orchestrator will finalize: one per implementation step.

## Verification

Every claim about a project (license, version, activity, API) links to its source and is dated.

## Pitfalls

- ImGui's API breaks between versions. Extensions often lag. Choose a version every selected
  extension supports.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
