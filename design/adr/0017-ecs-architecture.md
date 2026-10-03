# ADR-0017: ECS architecture

- Status: Proposed
- Date: 2026-10-03

## Context

Jade's engine layer needs an ECS that is as pleasant to use as the best C# ECS, as complete as flecs and
Bevy, and at least as fast as Arch and Friflo. It runs under NativeAOT and trimming, on every RID of
ADR-0007 including browser-wasm, where .NET runs on one thread. Analyzers and source generators ship inside
the `Jade` package (ADR-0002), and all code follows ADR-0011.

Task 401 compared Arch with Arch.Extended, Friflo.Engine.ECS, flecs, EnTT and `bevy_ecs`, read Nazara and
Godot for engine integration, verified the .NET 10 and C# 14 features that matter, and measured the decisions
that needed numbers. The evidence is in [ecs-survey.md](../research/ecs-survey.md); section numbers below
refer to it.

## Decision

The ECS lives in the `Jade` assembly, namespace `Jade.Ecs`. Its generators and analyzers are a
netstandard2.0 project, its code fixes a second one, both packed under `analyzers/dotnet/cs/` in `Jade`.

### D1. Storage: archetype tables in chunks, opt-in sparse components

Each archetype (a set of component types) owns a list of chunks. A chunk holds a fixed number of entities
and, for each data component, one typed managed array (`T[]`), plus the entity array and a change version per
column. The capacity comes from a byte budget, 16 KiB by default and configurable per world, with a minimum
entity count so that wide archetypes still hold a useful number of rows. Tags (components with no fields) are
bits of the archetype signature and have no array. Removal swaps the last row into the hole. Archetypes keep
add and remove edges to their neighbours.

A component can opt into sparse storage (`[Component(Storage = Sparse)]`): its values live in a per-type sparse
set outside the archetype, so adding or removing it moves nothing. Sparse components do not fragment
archetypes and are filtered per entity in queries.

- Rejected: one growable array per component per archetype (Bevy tables, Friflo, flecs). It is simpler and
  iterates a huge archetype without per-chunk overhead, but growth copies the whole column and large columns
  land on the large object heap, freed only by generation 2 collections. B2 measured the cost of 16 KiB chunks
  at +2.7 % over one table, and a chunk is also the natural unit for change versions, parallel splitting and
  SIMD that fits in cache (B1).
- Rejected: one native, aligned block per chunk (Unity style). It allows explicit alignment, but components
  that hold references must stay in managed arrays for the GC, which would split storage in two. B1 reached
  2× to 3.2× with ordinary managed arrays.
- Rejected: sparse sets for everything (EnTT). Adding and removing are O(1), but multi-component iteration then
  costs a lookup per component per entity, and EnTT's fix (owning groups) cannot overlap.

### D2. Entities: 64-bit index and generation, checked everywhere

`Entity` is a `readonly struct` of a 32-bit index and a 32-bit generation, 8 bytes, blittable, with no world
reference. Freed indices are reused through a free list with the generation incremented. Every accessor checks
the generation: a stale handle throws (or returns `false` from a `Try` method) instead of reaching a recycled
entity. Allocating an id is separate from spawning, so `Commands.Spawn` returns a valid `Entity` immediately.

- Rejected: a world or store reference inside the handle (Arch 12 bytes, Friflo 16 bytes). It allows
  `entity.Get<T>()`, but the handle is no longer blittable, needs a global world table (Arch) or doubles in
  size, and still breaks with several worlds. Extension members and an `EntityRef` view give the same
  convenience where a world is at hand.
- Rejected: fewer generation bits (EnTT 12, flecs 16, Friflo 16). Wrap-around makes stale handles valid
  again; 32 bits push that past any realistic session.
- Rejected: skipping the check on hot accessors (Arch's `Get`/`Set`, Friflo's bulk extensions). The check is one
  compare; systems do not pay it because they iterate chunks, not handles.

### D3. Components, tags, resources, disabling, hooks

- A component is a `partial struct` marked `[Component]`; the generator implements `IComponent<TSelf>` with its
  metadata. Fields may hold references.
- Resources (world singletons) are stored by type on the world, apart from entities, and reached through
  `Res<T>` and `ResMut<T>` system parameters that take part in access tracking.
- A `Disabled` tag removes an entity from every query that does not mention it (a default filter).
- Each component may declare one hook per lifecycle kind (`OnAdd`, `OnInsert`, `OnRemove`, `OnDespawn`) as a
  static method; the generator registers them. Hooks run during command application and may only record
  commands.
- Rejected: classes as components (Friflo `Script`). They put behaviour in data and cannot be stored in
  columns.
- Rejected: resources as components on their own entities (Bevy). It unifies change detection and
  serialization, but every whole-world query must exclude resource entities. Jade gives resources their own
  change versions instead.
- Deferred: per-component enable bits (flecs `CanToggle`). They add a bitset test to every query; `Disabled`
  and tag changes cover the common cases.

### D4. Queries: cached, incremental, generated rows

A query is described by its access list (read, write, with, without, optional, has, changed, added) and
caches the archetypes it matches. When an archetype is created, every live query tests it once, through the
component index of the query's rarest required component (Bevy). Iteration comes in four forms: generated
system loops (main path), generated query rows (`[Query] ref partial struct` with `ref` fields), chunk views
with spans, and a lambda `ForEach` for tools and tests. Access lists use Bevy's model (read and write sets,
with/without filters in disjunctive normal form), shared with the scheduler.

- Rejected: lambdas as the main API (Arch, Friflo). B3 measured a lambda at 1.28× a generated loop, and
  capturing lambdas allocate.
- Rejected: a runtime query language and engine (flecs). Very expressive, but it needs runtime reflection over
  types to be typed, and Jade has no scripting need yet. Relationship queries are traversal helpers (D7).
- Rejected: hash-keyed query caches compared by hash alone (Arch). A collision silently merges two queries.

### D5. Change detection: versions per chunk and column

The world tick advances once per system run. A system that has write access to a column stores the current
tick in that column's chunk version when it visits the chunk; `Changed<T>` and `Added<T>` skip chunks whose
version is not newer than the system's last run. Explicit writes through `World` mark the same versions.
Per-entity precision is an opt-in for components that need it (an added and changed tick per row, Bevy style),
measured before it is offered. Ticks are clamped before they wrap, as Bevy does.

- Rejected: Bevy's per-entity ticks for every component. They cost 8 bytes per component per entity and need a
  `Mut<T>` wrapper to mark writes, which C# cannot hide behind `ref`.
- Rejected: no change detection (Arch, Friflo). Rendering extraction, physics sync and networking all need it.
- Rejected: table-level counters (flecs). Chunk granularity is finer for the same cost.

### D6. Structural changes: commands and a locked world while iterating

Inside systems and query loops, structural changes go through `Commands`: typed records per component (no
boxing), all changes of one entity merged into a single archetype move at apply time (Friflo, flecs), applied
at sync points that the scheduler inserts after a system with commands when a later system depends on it, and
at the end of each phase. The world counts active iterations and throws on a structural call while any is
running, in every build. Analyzer JADE0101 reports such calls at compile time.

- Rejected: tolerating changes to the current entity through backward iteration (Arch, EnTT). It works for one
  case and leaves the others undefined.
- Rejected: a debug-only check (flecs `LOCKED_STORAGE`). The counter costs an increment per loop, not per
  entity.

### D7. Relationships and hierarchy: component pairs without fragmentation

A relationship is a component on the source holding the target `Entity` (`ChildOf(parent)`), and a generated
target component on the target holding its sources (`Children`), kept in sync by hooks (Bevy). The target is a
field, not part of the archetype key, so relationships never fragment storage. A relationship declares what
happens when its target despawns: despawn the source, remove the relationship, or forbid (flecs cleanup
policies). Hierarchy traversal and transform propagation are systems over `ChildOf`/`Children`. Prefabs and
scenes are data assets, instantiated in batches with entity references remapped, with Godot's delta overrides
in mind (E7).

- Rejected: pairs in the archetype key (flecs `(ChildOf, parent)`). It allows queries by target and wildcards
  through the storage itself, but creates one archetype per target, and flecs added a non-fragmenting `Parent`
  hierarchy for exactly this reason.
- Rejected: an intrusive pointer hierarchy inside a component (Nazara `Node`). It breaks when storage moves
  rows.

### D8. Events: hooks, observers, messages

Three tools with distinct jobs: hooks (D3) for invariants, one per component and kind; observers, which are
systems triggered by lifecycle events or by custom events targeted at an entity, run at command application
with deferred semantics (they record commands), and are registered by the generator; messages, typed
double-buffered queues read by systems through `MessageReader<T>` and written through `MessageWriter<T>`.

- Rejected: C# events or delegates per entity (Friflo signals, Nazara signal slots). They allocate per
  subscription and per entity, and cannot be scheduled.
- Rejected: one global static event bus (Arch.EventBus). It ignores worlds and access tracking.

### D9. Systems and scheduling: generated system types, one graph, two executors

A system is a `partial struct` marked `[System(Phase)]` with an `Execute` method. Its parameters declare its
access (`ref` writes, `in` reads, `Res<T>`, `ResMut<T>`, `Commands`, `Entity`, `Chunk<...>`, `Query<TRow>`,
messages); filters and ordering are attributes (`[With<T>]`, `[After<T>]`, run conditions). The struct's fields
are its local state. The schedule is a graph of phases (startup, pre-update, fixed update with an accumulator
and a step cap, update, post-update, extract, last) and of ordering constraints inside them. The same graph
runs on a single-threaded executor (topological order, deterministic) or a multi-threaded one (systems whose
access does not conflict run together; chunks of one system can be split across workers). Ambiguities
(conflicting access without an order) are reported at schedule build, on by default in debug builds, and by
analyzer JADE0201 within an assembly.

- Rejected: insertion order or a priority integer (Arch.System, Friflo, Nazara). Order then depends on
  registration code, ties are unspecified (Nazara sorts unstably), and no parallelism can be derived.
- Rejected: function systems identified by method (Bevy). A C# method cannot carry a generated property of the
  same name, and ordering by method name would be stringly typed; a type gives `[After<T>]`.
- Rejected: ambiguity detection off by default (Bevy). Its executors can then disagree on order between
  desktop and the browser.

### D10. Source generation, no reflection, no interceptors

The generator produces component metadata and registration (module initializer, ordinal name order), query
rows, system glue (access, ordering, loops, change marking), observer and hook tables, serializers and
inspector visitors. It follows incremental-generator rules: `ForAttributeWithMetadataName`, value-equatable
models, no symbols or locations in models, diagnostics from analyzers only. The engine uses no runtime
reflection; `Jade` keeps `IsAotCompatible` with zero warnings. Interceptors are not used.

- Rejected: runtime registration by reflection or assembly scanning, with suppressed trimming warnings (Arch,
  Friflo). It breaks or degrades under NativeAOT.
- Rejected: interceptors for call-site query caching. They still need a per-namespace opt-in on SDK 10.0.401
  and Learn calls them experimental.

### D11. Threading on browser-wasm

The scheduler picks the single-threaded executor when `OperatingSystem.IsBrowser()` is true or when the world
is configured for one thread; the multi-threaded executor is a desktop and mobile option. Parallel chunk loops
fall back to sequential loops. No engine code starts threads on the browser (`Thread.Start` is marked
unsupported there). A frame is a call driven by the host, never a blocking loop, so the browser's animation
callback can drive it.

- Rejected: a separate, simplified browser runtime. Two code paths would drift; Bevy and flecs show that one
  schedule with two executors works.

### D12. Serialization and inspection

Generated visitors and readers/writers per component serve binary saves, JSON scenes and the ImGui
inspector from one metadata source, with field hints from attributes. Persistent data names components by a
stable name (full type name, overridable with an attribute), never by runtime id, and remaps entity fields on
load (EnTT's continuous loader).

- Rejected: numeric runtime ids in saved data (Arch.Persistence). Ids depend on registration order.
- Rejected: reflection-based serializers (Friflo's JSON, Bevy's reflect registry at runtime). Generated code is
  AOT-safe and faster.

### D13. Determinism

With the single-threaded executor, a world evolves deterministically for the same inputs: archetypes are
iterated in creation order, chunks in order, rows in order; ids are recycled deterministically; no ordering
depends on hash seeds. The multi-threaded executor gives the same results when the schedule has no
ambiguities. Order of rows inside a chunk after structural changes is defined but not part of the public
contract.

## Consequences

- Eight implementation tasks follow, drafted as E1 to E8 in task 401's Outcome: core storage, queries and
  change detection, generators and analyzers, scheduler and commands, relationships, events, serialization and
  inspection, benchmarks.
- `Jade` gains two analyzer assemblies and a dependency decision for `System.Numerics.Tensors` (only if engine
  systems use `TensorPrimitives`; user code can reference it directly).
- The benchmark suite references Arch and Friflo packages for comparison; it is a test-only dependency.
- Choices left to measurement in the implementation tasks: the chunk budget's minimum entity count, per-entity
  change ticks, and the multi-threaded executor's chunk splitting.
- Not designed here: the renderer's extract step and render world, beyond the requirement that the ECS can
  feed one (survey section 4.3).
