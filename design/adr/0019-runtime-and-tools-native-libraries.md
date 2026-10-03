# ADR-0019: Two native libraries: jade_native for the runtime, jade_tools for tools

- Status: Accepted
- Date: 2026-10-03
- Amends: ADR-0003 (one combined library per RID becomes two), ADR-0002 (one more package pair),
  ADR-0014 (where ImGui lives) and the `jade_tools` part of ADR-0015

## Context

ADR-0003 links every upstream into one `jade_native`. Since then, Dear ImGui and its extensions
(ADR-0014, task 310) joined it, ImPlot alone adding about 5.8 MB per RID. They are mostly used by the
editor and by tools. A shipped 2D game (ADR-0018) would carry them for nothing. ADR-0015 had already
planned a desktop-only `jade_tools` for import-time libraries (the basisu encoder).

On 2026-10-03 the user chose to split runtime and tools before the ImGui bindings (311) are
written.

## Decision

- **`jade_native`** keeps what a shipped game needs: SDL3, Dawn, miniaudio, Box2D, the text stack
  and the runtime asset libraries. Its rules (ADR-0003) do not change.
- **`jade_tools`** is a second combined library per RID, built by the same xmake project and
  `jade.bundle` rule, with its own exact export list. It holds Dear ImGui with its extensions and
  backends (moved out of `jade_native`) and later the import-time libraries of ADR-0015.
- **One SDL and one Dawn per process.** ImGui's SDL3 and WebGPU backends must work on the
  application's own SDL state and Dawn objects. `jade_tools` therefore never embeds its own copy of
  a library that `jade_native` bundles. It reaches them through `jade_native`: dynamically linked on
  shared-library RIDs, in the same final link on static ones (browser-wasm, possibly iOS). Task 108
  validates the mechanism on every desktop RID. If it fails somewhere, the fallback (porting the two
  ImGui backends to C# over the existing bindings, which amends ADR-0014) is the user's decision.
- **RIDs**: `jade_tools` targets every RID of ADR-0007, so development builds can show ImGui on
  device. An import-time library may still be desktop-only inside it.
- **Packages and assemblies**: a `Jade.Tools` package (managed: the tool bindings, later the editor
  pieces) depends on `Jade` and on a `Jade.Tools.Native` package (`runtimes/<rid>/native/` for
  `jade_tools`). `Jade` and `Jade.Native` never reference them. Tool bindings live in their own
  assembly and import from `jade_tools`. Task 108 fixes the names.
- ADR-0015's rule that `jade_tools` "does not link against jade_native" is replaced by the rule
  above.

## Consequences

- Task 108 moves ImGui from `jade_native` to `jade_tools`. Tasks 311 and later ImGui tasks bind
  against `jade_tools`.
- The generator needs a library name and a target assembly per library config (311, or 108 if it is
  cheap there).
- Two libraries per RID to build, stage, check and pack, and a load-order constraint: `jade_native`
  must be found when `jade_tools` loads.
