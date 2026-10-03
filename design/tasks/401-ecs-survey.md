# 401: Survey: ECS design from the inspirations

- Depends on: none (research only; it can run in parallel with anything)
- ADRs: 0002, 0006, 0007, 0011

## Goal

A research document, a Proposed ADR and draft briefs that design Jade's ECS before any code is
written. The ECS should be as user-friendly as the best C# ECS, as complete as flecs and Bevy, and at
least as fast as Arch and Friflo, while using what .NET 10 and C# 14 offer today, source generators
first.

## Context

- The user collected the sources to study in `/home/aerafal/Téléchargements/inspirations/` (ZIP
  downloads, no git history). Licenses were checked by the orchestrator on 2026-10-03:

  | Folder | What it is | License |
  | --- | --- | --- |
  | `Arch-master` | C# archetype ECS (`src/Arch`, benchmarks, samples, tests) | Apache-2.0 |
  | `Arch.Extended-master` | Arch add-ons: source-generated queries and systems, relationships, persistence, ... | Apache-2.0 |
  | `Friflo.Engine.ECS-main` | C# archetype ECS with queries, events, relations, hierarchy, ... | MIT |
  | `flecs-master` | C ECS with relationships, queries, observers, prefabs, pipelines, reflection | MIT |
  | `entt-main` | C++ sparse-set ECS (registry, views, groups, signals) | MIT |
  | `bevy-main` | Rust engine; its ECS is `crates/bevy_ecs` (scheduling, change detection, observers) | MIT or Apache-2.0 |
  | `NazaraEngine-main` and `NazaraUtils-main` | C++ engine built around EnTT: how an engine's subsystems sit on an ECS | MIT |
  | `godot-master` | C++ engine with a node and scene tree, not an ECS (358 MB) | MIT |

- **Ideas, not code.** Nothing from these trees is copied into Jade. If a fragment is ever worth
  reusing, list it in the Outcome with its license (Apache-2.0 needs its NOTICE terms honoured) and
  ask the user first.
- Jade's constraints, which the design must respect:
  - NativeAOT and trimming (`IsAotCompatible`): no runtime reflection or `Reflection.Emit` on hot or
    registration paths.
  - Every RID of ADR-0007, including browser-wasm, where .NET is single-threaded by default. The
    scheduler needs a single-threaded mode that is not an afterthought.
  - ADR-0011 conventions; public API documented.
  - The ECS lives in the engine layer (`Jade`) and will drive rendering through `Jade.Interop.WebGpu`
    and input through `Jade.Interop.Sdl3`.
  - Analyzers and source generators ship inside the `Jade` package (ADR-0002).

## Scope

Research and throwaway experiments only. The deliverables go under `design/`; no engine code is
committed.

1. **Read the ECS cores in depth**: Arch, Arch.Extended, Friflo, flecs, EnTT, `bevy_ecs`. Read
   NazaraEngine and Godot for engine integration only. In Godot, look at scene composition, signals,
   resources, servers and editor hooks; the node tree itself is not a candidate. Use subagents to
   read the large trees in parallel, then check their key claims in the sources yourself before
   writing them down.
2. **Compare them on every axis that matters**, with file and line references:
   - Storage: archetypes and chunks versus sparse sets versus hybrids; structural change cost;
     memory layout (SoA, alignment, chunk size).
   - Entity IDs, generations, recycling. Component kinds: data, tags, shared and singleton or
     resources, enableable components.
   - Relationships and pairs (flecs), hierarchy (Bevy, Friflo, flecs `ChildOf`), prefabs and
     instancing.
   - Queries: cached and uncached, filters (with, without, optional, changed), iteration API,
     per-chunk access, query by relationship.
   - Change detection (Bevy ticks), events, observers and hooks (flecs, Bevy, Friflo, EnTT signals).
   - Command buffers and deferred structural changes; safety while iterating.
   - Systems and scheduling: system parameters, ordering, automatic parallelism from declared
     access (Bevy), pipelines and phases (flecs), run conditions; a single-threaded fallback.
   - Serialization, reflection, inspection and debug tooling.
   - Determinism, multithreading model, allocation behaviour.
3. **.NET 10 and C# 14 toolbox**: what each of these brings to the design, with a verdict and an
   example:
   - SIMD (`Vector128/256/512`, `Vector<T>`, `TensorPrimitives`) and hardware intrinsics on component
     columns;
   - `Span`, `ref struct` and `allows ref struct`, `[InlineArray]`, generic math, static abstract
     interface members;
   - C# 14 extension members, first-class span conversions and the `field` keyword;
   - function pointers;
   - incremental source generators, analyzers and code fixes (`IIncrementalGenerator`, incremental
     pipeline caching rules);
   - interceptors (their status and API in the .NET 10 SDK).

   Verify every claim on Microsoft Learn (MCP) or by a throwaway file-based app on SDK 10.0.401. Load
   the `vectorization` skill before writing SIMD advice, and the `microbenchmarking` skill before any
   benchmark.
4. **Source generation strategy**: what Arch.Extended and Friflo generate, what they still do at
   runtime, and what Jade should generate: component registration, queries, system parameter
   binding, scheduling metadata, serialization. State which diagnostics analyzers should raise (for
   example a component reference kept across a structural change, or conflicting access between
   parallel systems) and which code fixes are worth it.
5. **User-friendliness**: compare the "hello world" (spawn, query, system) of each C# library, then
   propose Jade's API as C# sketches. Cover error messages, debugging, and how an ImGui inspector
   would read the world later.
6. **Performance**: collect the published benchmark numbers (Arch and Friflo ship benchmark
   projects; record their dates and hardware) and propose Jade's benchmark suite. Optional, and
   throwaway only: small BenchmarkDotNet spikes for the two or three decisions that numbers would
   settle, such as chunk size or SIMD over a column.
7. **Modernization**: what Jade can do better than each C# library today, and what it should
   deliberately not do.

## Out of scope

- Writing the ECS, committing experiments, choosing the renderer architecture.

## Deliverables

- `design/research/ecs-survey.md`: the comparison and the toolbox findings, with references
  (`<folder>/<path>:<line>` for sources, URLs with dates for docs).
- `design/adr/NNNN-ecs-architecture.md`, Proposed: storage model, entity model, query model,
  scheduling model, relationships, what is source-generated, threading on browser-wasm. Take the
  next free ADR number when you start and say so in the Outcome: parallel tasks may also add ADRs.
- Draft briefs in the Outcome, labelled E1, E2, ..., one per implementation step (core storage,
  queries, generators, scheduler, relationships, events, serialization, benchmarks), in the format of
  `design/tasks/README.md`.

## Acceptance criteria

- [ ] Every comparison axis of Scope 2 is covered for every ECS core, or marked "not applicable"
      with a reason.
- [ ] Every .NET feature claim is backed by a Learn page or an experiment on SDK 10.0.401.
- [ ] The Proposed ADR explains each choice against at least one rejected alternative, with the
      trade-off.
- [ ] The API sketches compile: check them in a throwaway file-based app, against stubs if needed.
- [ ] Nothing from the inspiration trees is committed.

## Verification

List in the Outcome the throwaway experiments, their commands and their results. The documents pass
markdownlint (CLAUDE.md lists nothing for docs yet: `bunx markdownlint-cli2` with MD013 off is what
the orchestrator uses).

## Pitfalls

- The trees are ZIP downloads without history, so dates come from their changelogs or upstream
  repositories (`gh api`). Say when a version is unknown.
- Bevy and flecs rely on language features C# lacks (Rust traits and borrow checking, C macros).
  Separate the idea from its mechanism, and say how it would map to C#.
- Benchmark numbers from different machines and dates are not comparable. Never rank libraries on
  them without saying so.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
