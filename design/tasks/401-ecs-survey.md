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

- Summary: [ecs-survey.md](../research/ecs-survey.md) compares the six ECS cores on the ten axes of Scope 2
  (section 3), reads Nazara and Godot for integration (section 4), checks the .NET 10 and C# 14 toolbox
  (section 5), sets the source generation strategy with nine analyzer rules (section 6), sketches Jade's API
  (section 7), collects published numbers and three spikes (section 8), and lists what Jade does better than
  Arch and Friflo and what it leaves out (section 9). [ADR-0017](../adr/0017-ecs-architecture.md) (Proposed)
  turns it into thirteen decisions, each against a rejected alternative. Main findings:
  - Every archetype ECS here stores one array per component per archetype; only Arch chunks, and its chunk is
    a set of separate managed arrays sized from a 16 KiB budget. B2: 16 KiB chunks cost +2.7 % over one table,
    64-entity chunks +14.8 %.
  - Neither C# library is reflection-free by default: Arch creates columns with `Array.CreateInstance` unless
    its AOT generator ran; Friflo scans assemblies, or under NativeAOT still calls `MakeGenericMethod` behind 37
    suppressed warnings. Arch.Extended's generators rerun fully on every edit; Friflo ships no generator in its
    tree.
  - Arch and Friflo have no change detection for value writes and no scheduler; Bevy's executor derives
    parallelism from access and picks a single-threaded executor on wasm; flecs inserts sync points from
    access annotations.
  - B3: a generated loop and a struct callback cost the same, a lambda 1.28×, a function pointer 1.89×. B1:
    SIMD over a flattened column is 2.2× to 3.2× faster while the column fits in cache.
  - Interceptors still need an opt-in (CS9137) on SDK 10.0.401; Learn calls them experimental.
- Verification (commands and results), 2026-10-03, CachyOS, SDK 10.0.401, runtime 10.0.12. Experiments ran
  in the session scratchpad and are not committed; section 10 of the survey describes each one.
  - X1 `dotnet run features.cs`: every listed C# 11 to 14 feature compiles and runs; CS0029 on
    `MemoryMarshal.Cast` of an array (C# 14 prefers `ReadOnlySpan`); `Thread.Start` carries
    `[UnsupportedOSPlatform("browser")]`.
  - X2: generator project (netstandard2.0, `Microsoft.CodeAnalysis.CSharp` 5.9.0, the SDK's Roslyn) plus a
    consumer and a `GeneratorDriver` check project. `dotnet build` fails with CS9137 until
    `InterceptorsNamespaces` is set, then builds and runs; tracked steps report `Unchanged`/`Cached` after an
    unrelated file and `Modified` interceptors after an edit to the intercepted file; `dotnet publish -r
    linux-x64` (NativeAOT) gives no warning; adding `Microsoft.CodeAnalysis.CSharp.Workspaces` raises RS1038.
  - X3: BenchmarkDotNet 0.15.8, default job, AMD Ryzen 7 7800X3D: `--check` shows the SIMD kernels equal to the
    scalar loop bit for bit at lengths 0 to 1001; B1, B2 and B3 results in survey section 8.2.
    BenchmarkDotNet flagged one `TensorPrimitives` case as multimodal.
  - X4 `dotnet run sketch.cs` and `dotnet publish sketch.cs` with `TreatWarningsAsErrors` and
    `IsAotCompatible`: the API sketches compile against stubs, run with the expected output, and publish with
    NativeAOT with no warning.
  - Sources: 145 `<folder>/<path>:<line>` references in the survey checked by script (file exists, lines in
    range), and each one read by hand in this session; the subagent reports had wrong line numbers in places
    (EnTT `config.h`, Bevy `change_detection/mod.rs`, Friflo `SystemPerf.cs`), corrected before use.
  - Upstream metadata: `gh api repos/<repo>`, `.../releases/latest` and `.../commits` for the nine trees; the
    snapshot commits come from matching ZIP file times with commit times (survey section 1).
  - `bunx markdownlint-cli2` with MD013 off on the five changed documents: 0 issues.
  - No repository build or test: the change touches `design/` only.
- Decisions taken (and ADRs added): ADR-0017, Proposed, numbered 0017 because it was the next free number on
  `main` (`d850b4a`) when the task started. D1 chunked archetype storage with opt-in sparse components; D2
  8-byte entity handles checked everywhere; D3 generated struct components, separate resources, `Disabled`
  default filter, one hook per kind; D4 cached incremental queries with generated rows; D5 change versions per
  chunk and column; D6 commands merged per entity and a world locked during iteration; D7 relationships as
  component pairs without fragmentation; D8 hooks, observers and messages; D9 generated system types, one graph,
  two executors, ambiguity detection on by default; D10 source generation without reflection or interceptors;
  D11 single-threaded executor on browser-wasm; D12 generated serialization and inspection with stable names;
  D13 deterministic single-threaded execution.
- Deviations from the brief:
  - Browser-wasm behaviour (threads, SIMD in the browser) is documented from Learn and the runtime metadata,
    not run: no wasm workload is installed. Benchmarks ran on one x64 machine only, no arm64 and no wasm.
  - Several Bevy names in the brief do not exist in the snapshot (`Replace` is now `Discard`; no `BlobVec`,
    `ThinColumn` or `reserve_entity`; `DynamicScene` became `DynamicWorld` in a new crate). The survey uses the
    snapshot's names.
  - Friflo's README announces a v3.6 Query Generator that is not in the tree, so its generated code could not
    be compared.
  - RS1038 is backed by an experiment and the rule page the compiler links to, not by a Learn page.
- Follow-ups:
  - ADR numbering: tasks 004, 106, 202 and 310 run in parallel worktrees. If one of them merges an ADR-0017
    first, this ADR and its links are renumbered.
  - Nothing from the inspiration trees is proposed for reuse.
  - Once ADR-0017 is accepted: `design/architecture.md` gains the ECS and the two analyzer assemblies, CLAUDE.md
    gains their folders and a location for benchmarks, and the roadmap gets the Phase 4 tasks from E1 to E8.
  - E3 must choose the oldest SDK `Jade` supports for consumers: a generator built against a newer Roslyn than
    the consumer's compiler does not load (CS9057, listed with the source generator errors on Learn). The repo
    pins SDK 10.0.401 (Roslyn 5.9.0) in `global.json`; which Roslyn ships with earlier 10.0 SDKs was not checked.
  - Interceptors: revisit when Learn stops calling them experimental (survey section 5.8).
  - .NET's multithreaded WebAssembly mode is not covered; D11 assumes one thread.
  - The ImGui inspector (survey section 7.5) waits for tasks 311 and 312.

### Draft briefs

The orchestrator numbers and finalizes these. Common to all: code in `src/Jade/` under `Jade.Ecs`, tests in
xunit.v3 with Shouldly and CsCheck, ADR-0011 style, public members documented, NativeAOT and trimming with
zero warnings, and allocation tests (`GC.GetAllocatedBytesForCurrentThread`) for every steady-state path.

#### E1: ECS core: entities, archetypes and chunks

- Depends on: 003. ADRs: 0002, 0007, 0011, 0017.
- Goal: `World` stores entities in chunked archetypes and supports spawn, despawn, add, remove, get and
  try-get, tags, sparse components, resources and the `Disabled` tag, with checked handles.
- Context: ADR-0017 D1 to D3; survey sections 3.1 to 3.3 and 8.2 (B2). Until E3, component metadata
  (`IComponent<TSelf>`) is written by hand in tests, in the shape of survey section 7.2.
- Scope: `Entity` (index and generation, `42v3` display), allocator with allocate/spawn split, archetypes with
  add/remove edges, chunks from a 16 KiB budget with a measured minimum entity count, columns as typed arrays,
  tags as signature bits, inline-array signatures, sparse storage, resources, the iteration counter that
  forbids structural changes, error messages of survey section 7.3, debugger proxies.
- Out of scope: queries and filters (E2), generators (E3), commands and systems (E4), relationships (E5).
- Acceptance criteria: CsCheck property tests run random operation sequences against a dictionary model; a
  stale handle never reaches a recycled entity; steady-state iteration allocates nothing; a NativeAOT test
  app publishes with no warning.
- Verification: `dotnet build -c Release`, `dotnet test -c Release`, the AOT publish of the test app.
- Pitfalls: clear vacated slots that hold references (Arch does not, so dead objects stay reachable), using
  `RuntimeHelpers.IsReferenceOrContainsReferences<T>()`; `MemoryMarshal.Cast` of an array returns a
  `ReadOnlySpan` under C# 14; component ids follow registration order, so nothing persistent may store them.

#### E2: Queries, filters and change detection

- Depends on: E1. ADRs: 0017.
- Goal: cached queries match new archetypes incrementally and iterate through query rows, chunk spans and a
  lambda form, with `With`, `Without`, optional, `Has`, `Changed` and `Added` filters.
- Context: D4 and D5; survey sections 3.5, 3.6 and 8.2 (B3); Bevy's access model
  (`bevy-main/crates/bevy_ecs/src/query/access.rs`).
- Scope: query descriptions and cache, matching through the rarest component's index, query rows (hand-written
  `IQueryData<TSelf>` until E3), `Chunk<...>` views, `ForEach` with ref delegates, world tick, chunk and column
  versions, tick clamping, the `Disabled` default filter, an access model (read and write sets, filters in
  disjunctive normal form) with a conflict test.
- Out of scope: the scheduler (E4), per-entity change ticks (measure first, then decide).
- Acceptance criteria: a query created before and after archetype creation sees the same entities; `Changed<T>`
  skips untouched chunks; access conflicts match a table of expected pairs.
- Verification: build and tests as in E1.
- Pitfalls: a ref struct enumerator must release the iteration counter even when the loop exits early (an
  abandoned Friflo enumerator blocks all later structural changes); a new system must see everything on its
  first run (Bevy starts `last_run` at the oldest tick).

#### E3: Source generator, analyzers and code fixes

- Depends on: E2, 003. ADRs: 0002, 0011, 0017.
- Goal: a netstandard2.0 generator and analyzer project and a code-fix project, packed into `Jade` under
  `analyzers/dotnet/cs/`, generate component metadata and registration, query rows and component visitors, and
  report JADE0001, JADE0101, JADE0102 and JADE0301.
- Context: D10; survey sections 5.7, 6 and the traps met in X2.
- Scope: `ForAttributeWithMetadataName` pipelines with value-equatable models, module-initializer registration
  in ordinal name order, visitors (fields by `ref`, properties by copy), snapshot tests of generated output, a
  tracked-step test for caching, the four analyzers and their code fixes, packaging.
- Out of scope: system generation (E4), serializers (E7).
- Acceptance criteria: generated code passes `dotnet format --verify-no-changes`; an unrelated edit leaves every
  model `Cached` or `Unchanged`; `Jade` packs the analyzers and a consumer project picks them up.
- Verification: build, tests, `dotnet pack -c Release -o artifacts/packages` and a consumer build from the local
  feed.
- Pitfalls: RS1038 (code fixes need their own assembly); CS9057 if the Roslyn reference is newer than the
  consumer's compiler; `record struct` is a `RecordDeclarationSyntax`; CS9203 on span-returning properties;
  records need an `IsExternalInit` polyfill on netstandard2.0.

#### E4: Systems, commands and the scheduler

- Depends on: E3. ADRs: 0007, 0017.
- Goal: `[System(Phase)]` structs run from a schedule of phases and ordering constraints on a single-threaded
  or multi-threaded executor, with structural changes recorded through `Commands`.
- Context: D6, D9, D11; survey sections 3.7, 3.8 and 7.2.
- Scope: system generation (`ISystem`, access from parameters, loops, change marking), `Res<T>`/`ResMut<T>`,
  `Commands` with per-entity merging and immediate ids, sync points, phases with a fixed-step accumulator and
  step cap, `[After<T>]`/`[Before<T>]`, run conditions, both executors, ambiguity detection at build time,
  JADE0103/0201/0202 with code fixes, a schedule dump, per-system timings.
- Out of scope: observers (E6), relationships (E5).
- Acceptance criteria: the same schedule gives identical world state on both executors when it has no
  ambiguity; commands apply in system order, not thread order; no frame allocates after warm-up.
- Verification: build and tests; a browser-wasm check once task 105 is done (record as unverified until then).
- Pitfalls: never start threads when `OperatingSystem.IsBrowser()`; keep system structs in a generic runner class
  so their state is not boxed; Bevy's executors can disagree on order when constraints are missing, which is why
  ambiguity detection is on by default.

#### E5: Relationships, hierarchy and component hooks

- Depends on: E4. ADRs: 0017.
- Goal: components declare one hook per lifecycle kind, and `[Relationship]` components get a generated target
  component kept in sync by hooks, with `ChildOf`/`Children` built in.
- Context: D3 and D7; survey section 3.4; Bevy's relationship model
  (`bevy-main/crates/bevy_ecs/src/relationship/mod.rs`).
- Scope: hook generation and dispatch, relationship and target generation, despawn policies (despawn, remove,
  forbid), traversal helpers (ancestors, descendants, roots), cycle detection on insert.
- Out of scope: transform components and propagation (engine module), relationship pairs in the archetype key.
- Acceptance criteria: random insert, retarget and despawn sequences keep sources and targets consistent; a
  cycle is rejected with a clear error.
- Verification: build and tests.
- Pitfalls: hooks run during command application and must only record commands; Bevy does not detect cycles in
  traversal; Friflo orphans children on delete, which a despawn policy must make explicit.

#### E6: Observers and messages

- Depends on: E4; E5 for propagation along `ChildOf`. ADRs: 0017.
- Goal: `[Observer]` systems run on lifecycle and custom entity events, and typed messages flow through
  `MessageWriter<T>` and `MessageReader<T>`.
- Context: D8; survey section 3.6; Godot's deferred and one-shot semantics (section 4.2).
- Scope: observer generation and registration, lifecycle triggers, custom entity events, opt-in propagation,
  observers that replay matching entities on registration (Nazara's `SignalExisting`), double-buffered message
  queues updated once per frame.
- Out of scope: networking of events.
- Acceptance criteria: observer order is deterministic (registration order); a message written in one frame is
  readable in that frame and the next, then dropped.
- Verification: build and tests.
- Pitfalls: Bevy iterates observers in hash-map order; flecs warns observers are slower than a queue for
  frequent events, so messages are the default for high-volume events.

#### E7: Serialization, prefabs and inspection

- Depends on: E3, E5. ADRs: 0017, 0015 (asset assumptions).
- Goal: worlds, scenes and prefabs save and load through generated binary and JSON code, and an inspector API
  exposes components through generated visitors with field hints.
- Context: D12; survey sections 3.9, 4.2 (Godot's `SceneState`) and 7.5.
- Scope: generated readers and writers per component, stable component names with an override attribute,
  entity remapping on load, a prefab format with delta overrides and nested prefabs, batch instantiation,
  field hint attributes, the visitor API that an ImGui panel uses later.
- Out of scope: the ImGui panel itself (after 311 and 312), the asset pipeline (ADR-0015).
- Acceptance criteria: save, load and save again gives identical bytes; renaming a component type with a kept
  stable name still loads; a prefab instance stores only its overrides.
- Verification: build and tests; NativeAOT publish of a round-trip test app.
- Pitfalls: no reflection-based `System.Text.Json` (choose generated `Utf8JsonWriter` code or its source
  generator, and check AOT); entity fields inside components must be remapped, which is why the generator
  marks them.

#### E8: ECS benchmark suite

- Depends on: E2, extended after E4 to E6. ADRs: 0017.
- Goal: a BenchmarkDotNet project measures Jade against Arch and Friflo in the same run, on the scenarios of
  survey section 8.3.
- Context: survey section 8; Doraku's scenario names for comparability.
- Scope: the project, Arch and Friflo as package references (versions through `dotnet add package`), the
  scenario list, a NativeAOT run, a CI job that runs the short job and keeps results as an artifact.
- Out of scope: gating CI on numbers; the browser run until task 105.
- Acceptance criteria: every scenario runs for the three libraries in one invocation; results record machine,
  date and runtime.
- Verification: `--job Dry` in CI, a full local run reported in the Outcome.
- Pitfalls: a project without `<IsTestProject>true</IsTestProject>` gets MinVer and SourceLink from
  `Directory.Build.targets` as if it shipped, so the benchmark project needs its own opt-out; numbers from
  shared runners are noisy and must not gate merges.
