# ECS survey: storage, queries, scheduling and source generation

- Task: [401](../tasks/401-ecs-survey.md), 2026-10-03
- Feeds: [ADR-0017](../adr/0017-ecs-architecture.md) (Proposed)

This survey compares six ECS cores (Arch with Arch.Extended, Friflo.Engine.ECS, flecs, EnTT, `bevy_ecs`)
and two engines (Nazara on EnTT, Godot) to design Jade's ECS. It records what each library does, the
.NET 10 and C# 14 features that matter, what Jade should generate at compile time, and the numbers
behind the few decisions that needed measuring.

Ideas only: nothing from these trees is copied into Jade. Source references read
`<folder>/<path>:<line>`, relative to the folder of ZIP downloads the user collected
(`~/Téléchargements/inspirations/`). Experiments are labelled X1 to X4 and benchmarks B1 to B3; section
10 lists their commands and results. Learn pages were read on 2026-10-03.

## 1. Sources

GitHub ZIPs carry no history. Their files are stamped with the commit time, so the snapshot commit below
was found by matching the newest file time with the upstream commit list (`gh api
repos/<repo>/commits`): the times match to the second, seven hours apart (the ZIP is in UTC-7). For flecs,
three commits share that second.

| Folder | Upstream | Snapshot commit | Version in the tree | Latest release (`gh api`) | License |
| --- | --- | --- | --- | --- | --- |
| `Arch-master` | genaray/Arch | `9eb9ff27ca`, 2026-09-22 | 2.1.0 (`Arch-master/src/Arch/Arch.csproj:17`) | v2.1.0, 2025-05-27 | Apache-2.0 |
| `Arch.Extended-master` | genaray/Arch.Extended | `18d1e4c1fa`, 2026-05-30 | 1.0.1 to 2.1.0 per package | 1.0.8, 2023-03-13 | Apache-2.0 |
| `Friflo.Engine.ECS-main` | friflo/Friflo.Engine.ECS | `717caad3ae`, 2026-09-25 | tag-driven; README announces v3.6.0 | engine-v3.6.0, 2026-03-23 | MIT |
| `flecs-master` | SanderMertens/flecs | `9e874bca2c`, 2026-09-13 | 4.1.6 (`flecs-master/include/flecs.h:34-36`) | v4.1.6, 2026-06-29 | MIT |
| `entt-main` | skypjack/entt | `e0f4c37639`, 2026-09-17 | 4.1.0 (`entt-main/src/entt/config/version.h:8-10`) | v4.0.0, 2026-07-23 | MIT |
| `bevy-main` | bevyengine/bevy | `ad678262ce`, 2026-10-03 | `bevy_ecs` 0.20.0-dev (`bevy-main/crates/bevy_ecs/Cargo.toml:3`) | v0.19.1, 2026-08-13 | MIT or Apache-2.0 |
| `NazaraEngine-main` | NazaraEngine/NazaraEngine | `b5e1300d3b`, 2026-09-21 | none ("Nazara-Next") | none | MIT |
| `NazaraUtils-main` | NazaraEngine/NazaraUtils | `5a40c5df59`, 2026-09-15 | none | v1.1.4, 2026-09-12 | MIT |
| `godot-master` | godotengine/godot | `e7cfa294a0`, 2026-10-02 | 4.8.0-dev (`godot-master/version.py:3-6`) | 4.7.2-stable, 2026-08-18 | MIT |

Arch.Extended's latest GitHub release is three years older than its packages; its NuGet versions are not
tied to GitHub releases.

## 2. Findings in one screen

- **Storage.** Every archetype ECS here keeps one array per component per archetype, structure of arrays.
  Only Arch splits archetypes into chunks, and its chunk is a bundle of separate managed arrays sized from a
  16 KiB budget, not one block. Bevy and flecs add opt-in sparse storage for components that change often.
  EnTT is pure sparse sets. B2 measured chunks of 16 KiB at +2.7 % over one contiguous table, and 64 KiB
  within noise.
- **Entities.** Bevy and flecs use a 64-bit index plus generation with no world inside; Arch (12 bytes) and
  Friflo (16 bytes) put a world or store reference in the handle. Arch's `Get`/`Set` and Friflo's bulk
  extensions skip the generation check, so a stale handle can act on a recycled entity.
- **AOT.** Neither C# library is reflection-free by default. Arch builds every chunk column with
  `Array.CreateInstance` and converts `Type` to ids through `MakeGenericMethod` unless its AOT generator ran;
  Friflo scans assemblies, or under NativeAOT still calls `MakeGenericMethod` and hides 37 analyzer warnings
  behind suppressions. A source-generated registry removes all of it.
- **Generators.** Arch.Extended's three Roslyn generators carry syntax nodes and symbols through the pipeline
  and combine with the whole compilation, so every edit regenerates everything; Friflo ships none in its tree.
  X2 shows that a value-equatable pipeline stays cached on unrelated edits.
- **Change detection.** Arch and Friflo have none for value writes. flecs counts per table and column, Bevy
  per entity and component with ticks, plus an opt-in per-column summary tick. C# cannot intercept a write
  through `ref`, so marking has to happen where the access is granted.
- **Scheduling.** Only Bevy derives parallelism from declared access, and only flecs inserts sync points from
  it. Arch.System and Friflo run systems in insertion order, Nazara sorts one integer. Bevy and flecs both run
  the same schedule single-threaded, which is the browser-wasm case; Bevy picks that executor automatically on
  wasm.
- **Relationships.** flecs pairs live in the archetype key and fragment storage; flecs now also has a
  non-fragmenting `Parent` hierarchy. Bevy's relationships are component pairs kept in sync by hooks, which
  never fragment. Arch's are a class component holding a `SortedList`; Friflo's live in side archetypes.
- **Iteration cost (B3).** A generated loop and a struct callback cost the same; a lambda costs 1.28×, a
  function pointer 1.89×. SIMD over a flattened column (B1) runs 2.2× to 3.2× faster than the scalar loop
  while a chunk fits in cache.
- **Interceptors** still need a per-namespace opt-in on SDK 10.0.401 (CS9137) and Learn still calls them
  experimental.

## 3. Comparison by axis

Arch rows include Arch.Extended where it adds the feature.

### 3.1 Storage

| Library | Model | Layout and sizing | Structural change |
| --- | --- | --- | --- |
| Arch | Archetypes split into chunks | A `Chunk` holds one managed `T[]` per component plus `Entity[]` (`Arch-master/src/Arch/Core/Chunk.cs:145-207`); capacity from a 16 KiB budget with at least 100 entities (`Arch-master/src/Arch/Core/World.cs:119`, `Arch-master/src/Arch/Core/Archetype.cs:827-842`); tags still get an array (`Arch-master/src/Arch/Core/Chunk.cs:173-177`) | Single-component add/remove edges (`Arch-master/src/Arch/Core/Edges/Archetype.Edges.cs:13-27`); move copies each column with `Array.Copy`, then swap-removes (`Arch-master/src/Arch/Core/Chunk.cs:621-667`) |
| Friflo | Archetypes, no chunks | One `T[]` per component per archetype (`Friflo.Engine.ECS-main/src/ECS/Archetype/StructHeap.generic.cs:21-41`), 512 rows minimum, doubling (`Friflo.Engine.ECS-main/src/ECS/Archetype/Archetype.cs:453`, `Friflo.Engine.ECS-main/src/ECS/Archetype/Archetype.cs:341-362`); tags are part of the archetype key (`Friflo.Engine.ECS-main/src/ECS/Archetype/EntityStore.Mutation.cs:108-136`) | 4-entry transition cache per archetype (`Friflo.Engine.ECS-main/src/ECS/Archetype/TypeCache.cs:7-41`); archetypes are never removed (`Friflo.Engine.ECS-main/src/ECS/Archetype/EntityStore.Archetype.cs:178-190`) |
| flecs | Tables, opt-in sparse | One `malloc`'d column per component, no chunks, the component's alignment not used (`flecs-master/src/storage/table.h:66-78`, `flecs-master/src/storage/table.c:1258-1298`); `Sparse` keeps the id in the table type but stores values in a paged sparse set; `DontFragment` keeps it out of the type (`flecs-master/docs/ComponentTraits.md:1831-1833`, `flecs-master/src/storage/table.c:244-251`) | Cached edges per id (`flecs-master/src/storage/table_graph.c:899-974`); empty tables stay until `ecs_delete_empty_tables` (`flecs-master/include/flecs.h:2530-2583`) |
| EnTT | Sparse set per component | Sparse pages of 4096, component pages of 1024 that never move (`entt-main/src/entt/config/config.h:47-52`) | O(1) add/remove that moves no other component; `destroy` visits every pool (`entt-main/src/entt/entity/registry.hpp:504-511`) |
| Bevy | Tables plus per-component sparse sets | `StorageType::Table` (default) or `SparseSet` (`bevy-main/crates/bevy_ecs/src/component/mod.rs:763-771`); a column is a byte array plus added and changed tick arrays (`bevy-main/crates/bevy_ecs/src/storage/table/column.rs:27-33`) | Edges cached per bundle; archetypes and tables are never freed (`bevy-main/crates/bevy_ecs/src/archetype.rs:14-15`) |

For Jade: archetype tables split into chunks of typed managed arrays, tags as signature bits with no column,
and an opt-in sparse storage for components that would otherwise churn archetypes (ADR-0017 D1). B2 (section
8.2) shows that chunking costs almost nothing at 16 KiB, and chunks bound growth copies: a single column of a
large archetype grows by reallocation and lands on the large object heap past 85,000 bytes, which only a
generation 2 collection reclaims ([LOH][learn-loh]).

### 3.2 Entities

| Library | Handle | Recycling | Liveness |
| --- | --- | --- | --- |
| Arch | `{int Id, int WorldId, int Version}`, 12 bytes; static `World.Worlds` (`Arch-master/src/Arch/Core/Entity.cs:143-165`, `Arch-master/src/Arch/Core/World.cs:75-153`) | FIFO queue (`Arch-master/src/Arch/Core/World.cs:266-285`) | `IsAlive` checks the version, `Get`/`Set`/`Has` do not (`Arch-master/src/Arch/Core/World.cs:1126-1213`) |
| Friflo | Store reference + `int` id + `short` revision, 16 bytes (`Friflo.Engine.ECS-main/src/ECS/Entity.cs:367-379`) | LIFO; `short` revision with no wrap guard (`Friflo.Engine.ECS-main/src/ECS/Entity/Store/NodeTree.cs:485-535`) | Bulk `Add`/`Remove`/`Set` extensions skip the revision (`Friflo.Engine.ECS-main/src/ECS/Entity/Extensions/Add.cs:17-29`) |
| flecs | 64 bits: 32 index, 16 generation, flag bits (`flecs-master/include/flecs/private/api_defines.h:371-376`) | LIFO through an alive/dead partition (`flecs-master/src/storage/entity_index.c:175-200`, `flecs-master/src/storage/entity_index.c:271-298`) | Full 64-bit compare (`flecs-master/src/storage/entity_index.c:110-124`) |
| EnTT | 32 bits: 20 index, 12 version; 64 bits: 32/32 (`entt-main/src/entt/entity/entity.hpp:37-57`) | Swap-only entity storage, released ids at the back | One compare through the sparse slot (`entt-main/src/entt/entity/sparse_set.hpp:688-696`) |
| Bevy | 32 index + 32 generation laid out as a `u64` (`bevy-main/crates/bevy_ecs/src/entity/mod.rs:446-456`) | LIFO; allocation separate from spawning, so `Commands` returns a real id at once (`bevy-main/crates/bevy_ecs/src/system/commands/mod.rs:231-238`) | Generation compare; a wrap only logs a warning (`bevy-main/crates/bevy_ecs/src/entity/mod.rs:1066-1068`) |

For Jade: Bevy's handle and allocate/spawn split, with every accessor checking the generation (D2).

### 3.3 Component kinds

| Library | Tags | Singletons and resources | Enable and disable | Type identity |
| --- | --- | --- | --- | --- |
| Arch | Allocated column | None | None | First-use static ids, `Dictionary<Type, ComponentType>`, `MakeGenericMethod` for `Type` inputs (`Arch-master/src/Arch/Core/ComponentRegistry.cs:319-337`) |
| Friflo | `ITag`, in the archetype key | `UniqueEntity` looked up by an index | `Disabled` tag, excluded by default (`Friflo.Engine.ECS-main/src/ECS/Query/QueryFilter.cs:76-81`); adding it moves the entity | Assembly scan, or `NativeAOT.Register*` then `MakeGenericMethod` (`Friflo.Engine.ECS-main/src/ECS/Base/SchemaUtils.cs:16-33`, `Friflo.Engine.ECS-main/src/ECS/Base/SchemaTypes.cs:114-168`) |
| flecs | No column | A component set on its own component entity | `CanToggle`: a bitset column per table (`flecs-master/src/entity.c:1976-2012`) | Components are entities |
| EnTT | Empty-type optimization | `registry.ctx()` | None (a TODO) | Compile-time hash of compiler type names, or a runtime counter |
| Bevy | Zero-sized column | Resources are components on their own entity with an `IsResource` marker (`bevy-main/crates/bevy_ecs/src/resource.rs:87`, `bevy-main/crates/bevy_ecs/src/resource.rs:124`) | `Disabled` plus `DefaultQueryFilters` (`bevy-main/crates/bevy_ecs/src/entity_disabling.rs:175-178`) | Per-world dense ids |

Lifecycle hooks: Arch has events only in builds compiled with `EVENTS`, and otherwise `Subscribe*` silently
does nothing (`Arch-master/src/Arch/Core/Events/World.Events.cs:54-62`). flecs has type hooks plus
`on_add`, `on_set`, `on_remove`, `on_replace` and `on_validate` (`flecs-master/include/flecs.h:978-1050`).
Bevy allows one hook per kind (`bevy-main/crates/bevy_ecs/src/lifecycle.rs:153-159`); its `Replace` event
is now `Discard` (`bevy-main/crates/bevy_ecs/src/lifecycle.rs:26-31`).

For Jade: generated struct components, tags without columns, resources stored apart from entities, a
`Disabled` tag excluded by default, one generated hook per kind (D3).

### 3.4 Relationships, hierarchy and prefabs

| Library | Relationships | Hierarchy | Prefabs |
| --- | --- | --- | --- |
| Arch | `Relationship<T>`, a class component holding a `SortedList<Entity, T>` (`Arch.Extended-master/Arch.Relationships/Relationship.cs:41-56`); cleanup only with `EVENTS` | None | None; `World.Copy()` only |
| Friflo | Side archetypes plus dictionaries per relation type | `TreeNode` plus a parent map; deleting a parent orphans its children (`Friflo.Engine.ECS-main/src/ECS/Entity/Store/NodeTree.cs:526-529`) | A design document only; `CloneEntity` |
| flecs | Pairs `(R, T)` in the archetype key, wildcards, cleanup traits | Fragmenting `ChildOf`, and the newer non-fragmenting `Parent` (`flecs-master/include/flecs.h:1631-1633`, guidance at `flecs-master/docs/HierarchiesManual.md:298-307`) | `IsA` with `Override`/`Inherit`/`DontInherit` |
| EnTT | None; the docs suggest an intrusive component | None | Copy through the type-erased pool API |
| Bevy | `Relationship` on the source, `RelationshipTarget` on the target, kept in sync by hooks; never fragments (`bevy-main/crates/bevy_ecs/src/relationship/mod.rs:27-40`) | `ChildOf` and `Children`, despawn cascades through `linked_spawn` (`bevy-main/crates/bevy_ecs/src/hierarchy.rs:94-107`) | Templates and BSN scenes; `EntityCloner` |

flecs documents the cost of pairs in the key: more tables, slower table creation, pair edges that always go
through a hash map (`flecs-master/docs/Relationships.md:1869`); its own hierarchy guide recommends `Parent`
for many small, deep hierarchies, which is what a scene graph is.

For Jade: Bevy's component pairs with flecs-style cleanup policies, prefabs as data (D7).

### 3.5 Queries

| Library | Caching | Filters | Iteration |
| --- | --- | --- | --- |
| Arch | Cached per description forever, keyed by hash only (`Arch-master/src/Arch/Core/World.cs:411-437`, `Arch-master/src/Arch/Core/Query.cs:452-455`) | All, Any, None, Exclusive | Lambda, struct `IForEach`, chunk loops; backwards (`Arch-master/src/Arch/Core/Enumerators.cs:184-205`) |
| Friflo | Incremental over new archetypes (`Friflo.Engine.ECS-main/src/ECS/Query/ArchetypeQuery.cs:266-318`) | Component and tag sets, value conditions | Lambda, chunk spans, struct `IEach` in the Boost package |
| flecs | Cached, uncached or auto; caches follow table creation (`flecs-master/src/query/cache/cache.c:210-255`) | And, Or, Not, Optional, traversal, variables, a DSL | Trivial fast path, otherwise a backtracking interpreter (`flecs-master/src/query/engine/eval.c:1675-1720`) |
| EnTT | Views uncached, owning groups as perfect SoA | Exclude only | Smallest pool leads |
| Bevy | `QueryState` updated incrementally through the rarest component's index (`bevy-main/crates/bevy_ecs/src/query/state.rs:572-628`) | `With`, `Without`, `Option`, `Or`, `Has`, `Added`, `Changed` | Dense or archetypal; contiguous slices for SIMD (`bevy-main/crates/bevy_ecs/src/system/query.rs:1478-1520`); `par_iter` falls back to a sequential loop on wasm (`bevy-main/crates/bevy_ecs/src/query/par_iter.rs:87-101`) |

Bevy keeps access as read and write sets with filters in disjunctive normal form
(`bevy-main/crates/bevy_ecs/src/query/access.rs:227-237`), and that one model drives in-system conflict
errors, the executor and ambiguity detection.

For Jade: cached queries updated when archetypes appear, Bevy's filter set, generated query rows, chunk spans
and an access model shared with the scheduler (D4).

### 3.6 Change detection, events, observers and hooks

| Library | Change detection | Events and observers |
| --- | --- | --- |
| Arch | None; `Archetype.Version` counts population changes (`Arch-master/src/Arch/Core/Archetype.cs:384-409`) | `EVENTS` builds only; Arch.EventBus is a global generated bus |
| Friflo | None for value writes | Structural events, per-entity signals in dictionaries, an `EventRecorder` for "added since" filters |
| flecs | Counter per table and column, opt-in per query; misses writes through refs (`flecs-master/docs/Queries.md:3353`, `flecs-master/docs/Queries.md:3368-3371`) | Query-based observers, propagation along relationships, monitors; hooks run before observers |
| EnTT | None; `on_update` fires only through `patch`/`replace` (`entt-main/docs/md/entity.md:362-365`) | Signals per pool, reactive storage, a dispatcher with queues |
| Bevy | Added and changed ticks per entity and component, marked by `Mut<T>`; opt-in summary tick per column (`bevy-main/crates/bevy_ecs/src/component/mod.rs:503-520`); clamped before wrap (`bevy-main/crates/bevy_ecs/src/change_detection/mod.rs:22-27`) | `Event` runs observers at once; `Message` is a double-buffered pull queue lost after two updates (`bevy-main/crates/bevy_ecs/src/message/messages.rs:25-38`) |

For Jade: versions per chunk and column, written where write access is granted; hooks, observers and
messages as three separate tools (D5, D8).

### 3.7 Command buffers and safety while iterating

| Library | Deferral | Iteration safety |
| --- | --- | --- |
| Arch | `CommandBuffer` with placeholder entities and no public way back to the real ones; playback on the main thread (`Arch-master/src/Arch/Buffer/CommandBuffer.cs:277-403`) | Not enforced; backward iteration tolerates changing the current entity |
| Friflo | Typed per component, all changes of one entity merged into one move (`Friflo.Engine.ECS-main/src/ECS/CommandBuffer/CommandBuffer.cs:384-403`); ids without revision | `StructuralChangeException` inside query loops (`Friflo.Engine.ECS-main/src/ECS/Archetype/EntityStore.cs:234`) |
| flecs | Deferred mode, per-entity batching into one move, double-buffered queues (`flecs-master/src/commands.c:859-1010`) | `LOCKED_STORAGE` assert, compiled out in release (`flecs-master/include/flecs/addons/log.h:470-478`) |
| EnTT | None | Rules: only the current entity may change (`entt-main/docs/md/entity.md:2244-2250`) |
| Bevy | Per-system byte queues; sync points inserted on ordering edges (`bevy-main/crates/bevy_ecs/src/schedule/auto_insert_apply_deferred.rs:17-25`) | The borrow checker |

For Jade: typed commands merged per entity, real ids at record time, and a world that refuses structural
changes during iteration in every build (D6).

### 3.8 Systems and scheduling

| Library | Systems | Ordering | Parallelism | Single-threaded |
| --- | --- | --- | --- | --- |
| Arch | `BaseSystem`, `Group` with Before/Update/After (`Arch.Extended-master/Arch.System/Systems.cs:89-317`) | Insertion order | Parallel queries on an external scheduler; throw without it (`Arch-master/src/Arch/Core/Jobs/World.Jobs.cs:97-100`) | Only by avoiding parallel queries |
| Friflo | `SystemRoot`, `SystemGroup`, `QuerySystem` (`Friflo.Engine.ECS-main/src/ECS/Systems/SystemGroup.cs:259-304`) | Insertion order | Inside one query; worker threads busy-spin (`Friflo.Engine.ECS-main/src/ECS/Query/ParallelJobRunner.cs:217-221`) | `Run()` or one thread |
| flecs | Systems are entities; phases chained by `DependsOn` (`flecs-master/src/addons/pipeline/pipeline.c:936-975`) | Phase, then entity id; sync points from `[out]` annotations (`flecs-master/src/addons/pipeline/pipeline.c:131-216`) | Workers take a slice of every table | One stage, unchanged |
| EnTT | Plain functions; `organizer` builds a graph from `const` parameters (`entt-main/src/entt/entity/organizer.hpp:39-80`) | The graph | Left to the user | Registration order |
| Bevy | Function systems with `SystemParam` | Sets, `before`/`after`, run conditions | Static per-system access (`bevy-main/crates/bevy_ecs/src/schedule/executor/multi_threaded.rs:148-235`); ambiguity detection off by default (`bevy-main/crates/bevy_ecs/src/schedule/schedule.rs:1884-1889`) | Chosen on wasm (`bevy-main/crates/bevy_ecs/src/schedule/executor/mod.rs:44-66`) |

Bevy warns that its multi-threaded executor can produce orders the single-threaded one never would
(`bevy-main/crates/bevy_ecs/src/schedule/schedule.rs:1921-1924`): a game tested on desktop can behave
differently in the browser when an ordering constraint is missing.

For Jade: generated system types, phases plus explicit ordering, one graph for both executors, and ambiguity
detection on by default in debug builds and in the analyzer (D9).

### 3.9 Serialization, reflection and tooling

| Library | Serialization | Reflection and inspection |
| --- | --- | --- |
| Arch | MessagePack and Utf8Json, components stored by numeric id (`Arch.Extended-master/Arch.Persistence/Binary.cs:201-216`), so the loader must register components in the same order | Debugger proxy that boxes; `Dangerous*` extensions |
| Friflo | JSON through the author's reflection-based library; unknown components kept in `Unresolved` | 18 debugger proxies; `MemberPath` compiles expression trees |
| flecs | JSON, REST server and web explorer, also over wasm (`flecs-master/docs/FlecsRemoteApi.md:25-31`) | Runtime reflection stored as entities; stats, metrics, alerts |
| EnTT | Snapshots and a continuous loader that remaps remote ids (`entt-main/src/entt/entity/snapshot.hpp:303-506`) | Template-built `meta`; `davey`, an ImGui inspector (`entt-main/src/entt/tools/davey.hpp:234-336`) |
| Bevy | `DynamicWorld` in RON through the type registry | `#[derive(Reflect)]` function tables; `World::inspect_entity` |

For Jade: generated metadata and visitors feed both serialization and the inspector; persistent data uses
stable names, never runtime ids (D12).

### 3.10 Determinism, threading and allocation

- **Order.** Bevy and Friflo iterate archetypes in creation order and rows in insertion order until a
  swap-remove. flecs changes table order inside its caches on swaps (`flecs-master/src/storage/table_cache.c:146-151`).
  Arch iterates backwards. All recycle ids deterministically for the same sequence of operations, LIFO except
  Arch (FIFO).
- **Hidden nondeterminism.** Arch hashes signatures with `System.HashCode`, which is randomly seeded per
  process; it only affects lookup, but equality compares hashes alone, with no collision handling. Bevy stores
  observers in hash maps with no documented order. Bevy's parallel commands apply in thread order.
- **Threading.** Every library has a single writer. flecs and Bevy give each worker its own command queue and
  merge at sync points; Friflo spins dedicated threads; Arch needs an external job scheduler.
- **Allocation.** All five iterate without allocating once queries are cached. Friflo's tests check it with
  `Mem.AssertNoAlloc` (`Friflo.Engine.ECS-main/src/Tests/Utils/Mem.cs:15-23`) in about 70 places. Lambda
  queries allocate a closure when they capture.

For Jade: deterministic single-threaded execution by definition, allocation-free steady state guarded by
tests (D11, D13).

## 4. Engine integration: Nazara on EnTT, and Godot's engine-level patterns

Read for integration only, as the brief asks. Godot's node tree is not a candidate.

### 4.1 Nazara

- **Application and worlds.** `ApplicationBase` owns type-keyed application components (windowing,
  filesystem, plugins, the ECS) and updates them in insertion order
  (`NazaraEngine-main/include/Nazara/Core/ApplicationBase.inl:133-148`). `EnttWorld` wraps an `entt::registry`
  and a system list (`NazaraEngine-main/include/Nazara/Core/EnttWorld.hpp:17-50`). On the web the loop is
  inverted into an `emscripten_set_main_loop_arg` callback timed by `requestAnimationFrame`
  (`NazaraEngine-main/src/Nazara/Core/ApplicationBase.cpp:31-63`).
- **Ordering.** A system declares one `ExecutionOrder` integer; the list is sorted with `std::sort`, which is
  not stable, so ties run in unspecified order (`NazaraEngine-main/src/Nazara/Core/EnttSystemGraph.cpp:18-37`).
  Systems declare `AllowConcurrent`, a trait reads it, and nothing uses the trait
  (`NazaraEngine-main/include/Nazara/Core/EnttSystemGraph.inl:12-16`).
- **Rendering.** The render backend never sees entities: `FramePipeline` registers instances and renderables
  and hands back indices (`NazaraEngine-main/include/Nazara/Graphics/FramePipeline.hpp:64-87`). `RenderSystem`
  mirrors entities into it through `EnttObserver`, a reactive storage with a replay of entities that existed
  before the system (`NazaraEngine-main/include/Nazara/Core/EnttObserver.inl:167`). Change tracking connects
  six signal slots per drawable entity (`NazaraEngine-main/include/Nazara/Graphics/Systems/RenderSystem.hpp:90-104`),
  each a `shared_ptr` (`NazaraUtils-main/include/NazaraUtils/Signal.hpp:57-71`), with a re-entrancy FIXME
  (`NazaraEngine-main/include/Nazara/Core/EnttObserver.inl:36`).
- **Transforms.** `NodeComponent` inherits an intrusive `Node` with raw pointers; because EnTT moves
  components, `Node`'s move constructor patches every pointer (`NazaraEngine-main/include/Nazara/Core/Node.inl:37-61`).
- **Physics.** Bodies are created in an `on_construct` handler, so the system must exist before any body
  (`NazaraEngine-main/src/Nazara/Physics3D/Systems/Physics3DSystem.cpp:18-21`), and poses are written back after
  each fixed step with a per-body replication mode (`NazaraEngine-main/src/Nazara/Physics3D/Systems/Physics3DSystem.cpp:235-289`).
- **Disabled entities.** An empty `DisabledComponent` sits in the exclude list of every engine query
  (`NazaraEngine-main/src/Nazara/Core/Systems/LifetimeSystem.cpp:13`).

### 4.2 Godot

- **Servers and RIDs.** The scene side keeps opaque 64-bit RIDs: a validator from a process-wide counter in
  the high half, a slot index in the low half (`godot-master/core/templates/rid_owner.h:157-163`), in chunked,
  address-stable storage that reports leaks at exit (`godot-master/core/templates/rid_owner.h:429-431`).
- **One API, threaded or not.** Server methods call directly on the server thread and enqueue a command
  otherwise; creation allocates the RID at once and enqueues its initialization
  (`godot-master/servers/server_wrap_mt_common.h:58-67`). Without a render thread every call takes the direct
  path.
- **Frame order.** Physics steps capped at 8 by default (`godot-master/main/main.cpp:2235`), then `process`,
  then the render sync and draw, with deferred-call queues flushed between stages
  (`godot-master/main/main.cpp:4982-5124`).
- **Signals.** Emission snapshots the connections first, one-shot connections are dropped before the call,
  and `CONNECT_DEFERRED` queues the call for the next flush (`godot-master/core/object/object.cpp:1290-1362`).
  Names are strings and arguments `Variant`s.
- **Prefabs.** `SceneState` is a flat table format that stores only properties differing from the class
  default, base scene or enclosing instance (`godot-master/scene/resources/packed_scene.h:41-93`,
  `godot-master/scene/resources/packed_scene.cpp:1049-1062`), and resolves node references once all nodes
  exist (`godot-master/scene/resources/packed_scene.cpp:700-710`). A resource marked local to scene is
  duplicated per instance (`godot-master/scene/resources/packed_scene.cpp:772-800`).
- **Editor metadata.** `PropertyInfo` (type, hint, hint string, usage flags) drives both the inspector and the
  serializer (`godot-master/core/object/property_info.h:128-134`). Godot's C# integration already generates
  property lists and static getter and setter trampolines with a Roslyn generator
  (`godot-master/modules/mono/editor/Godot.NET.Sdk/Godot.SourceGenerators/ScriptPropertiesGenerator.cs:184-260`).

### 4.3 What Jade takes from them

| Pattern | Source | Jade |
| --- | --- | --- |
| Engine services separate from ECS components | Nazara | Adopt: windowing, assets and audio are services; the ECS is one of them |
| Host-driven frame callback on the web | Nazara | Adopt: a frame is a call that never blocks |
| One integer for ordering | Nazara | Reject: phases plus explicit before and after |
| Render backend behind validated handles | Nazara, Godot | Adopt: an extract step copies changed components into a render world keyed by handles |
| Direct call on the owning thread, queued otherwise | Godot | Adopt for the render and physics bridges: one API for threaded desktop and wasm |
| Observer that replays existing entities | Nazara | Adopt as an ECS feature |
| Per-entity signal slots for change tracking | Nazara | Reject: chunk versions and observers |
| Intrusive pointer hierarchy inside components | Nazara | Reject: `ChildOf` and `Children` components with a propagation system |
| `Disabled` tag excluded by default | Nazara | Adopt, as a default query filter |
| Fixed step with a step cap | Godot | Adopt as a `FixedUpdate` phase |
| Deferred, one-shot, weak-target semantics | Godot | Adopt with typed, generated events |
| Flat table prefabs with delta overrides and stable ids | Godot | Adopt for scenes and prefabs |
| Metadata shared by inspector and serializer | Godot | Adopt, generated at compile time |

## 5. The .NET 10 and C# 14 toolbox

Every claim below comes from a Learn page or from experiments X1 to X4, run on SDK 10.0.401, runtime
10.0.12 and Roslyn 5.9.0, linux-x64.

| Feature | What it brings to an ECS | Verdict |
| --- | --- | --- |
| `Vector128<T>`, `TensorPrimitives` | Column math in chunk systems | Use in chunk systems; not inside the core loop |
| `Vector256/512<T>`, `Vector<T>` | Wider math on x64 | Leave to `TensorPrimitives`; never the portable baseline |
| `Span<T>`, `ref struct`, ref fields | Columns as spans, query rows as `ref` views | The core of the iteration API |
| `allows ref struct` | Generic callbacks and rows that are ref structs | Use in the query and visitor APIs |
| `[InlineArray]`, `InlineArrayN<T>` | Fixed-size buffers with no heap object | Archetype signatures, small inline lists |
| Static abstract interface members | Per-type metadata without reflection | The backbone of generated metadata |
| Generic math | Numeric helpers over any element type | No core role |
| C# 14 extension members | Modules add properties to `World` | Use for module APIs; not a core mechanism |
| `field` keyword | Shorter validated properties | Cosmetic |
| First-class span conversions | Arrays flow into span parameters | Use; mind the `ReadOnlySpan` preference |
| Function pointers | Delegate-free dispatch | Per system or per hook, never per entity |
| Incremental generators | Registration, metadata, system glue, serializers | The main mechanism (section 6) |
| Analyzers and code fixes | Misuse the type system cannot catch | Ship with the generator (section 6.4) |
| Interceptors | Call sites rewritten at compile time | Do not depend on them |

### 5.1 SIMD on component columns

- Learn recommends `Vector128<T>` as the starting point because every vectorizing platform supports it, and
  notes that only x86/x64 offers wider vectors today ([SIMD guide][learn-simd]). Jade ships arm64 phones and
  browser-wasm, so 128 bits is the portable baseline.
- On browser-wasm .NET uses WebAssembly SIMD by default; `<WasmEnableSIMD>false</WasmEnableSIMD>` turns it off
  for old browsers ([WebAssembly runtime performance][learn-wasm-perf]).
- `TensorPrimitives` comes from the `System.Numerics.Tensors` package (10.0.12 on 2026-10-03); the shared
  framework folder `shared/Microsoft.NETCore.App/10.0.12/` has no `System.Numerics.Tensors.dll` (X1).
  `MultiplyAdd(x, y, addend, destination)` accepts a destination that overlaps `addend` only when both start at
  the same element ([MultiplyAdd][learn-multiplyadd]), which is exactly `p += v * dt` in place.
- A column of `Position { float X, Y, Z }` cast with `MemoryMarshal.Cast<Position, float>` is a flat float
  array. When two components share a layout, lane *i* of one lines up with lane *i* of the other, so
  element-wise math runs over whole columns without changing the component types. B1 checks the flattened
  kernels bit for bit against the scalar loop for lengths 0, 1, 2, 3, 5, 7, 1000 and 1001, then measures
  2.2× (`Vector128`) and 3.2× (`TensorPrimitives`) at 1,000 entities, about 1.3× at 100,000 and 2× at
  4,000,000 (section 8.2).
- Verdict: storage guarantees that a chunk column is one contiguous span, which is all SIMD needs. Small
  chunks keep a column in cache, where B1 shows the largest gain. The core does not vectorize user code and
  promises no alignment beyond the element type's; chunk systems are the documented way to vectorize.

### 5.2 Spans, ref structs, ref fields and `allows ref struct`

- Ref fields (C# 11) let a query row hold `ref Position` and `ref readonly Velocity`; X1 writes through such a
  field and X4 iterates a generated query row built from them.
- C# 13 lets a type parameter accept ref structs with `where T : allows ref struct`, and lets ref structs
  implement interfaces, never through a boxing conversion ([constraints][learn-allows-ref-struct]). X1 runs a
  generic loop whose callback is a ref struct holding state; X4's `IQueryData<TSelf>` and inspector visitor use
  the same constraint.
- Verdict: chunks are spans and rows are ref structs. Neither can reach the heap, so a component reference
  cannot outlive the loop that produced it. Analyzer JADE0102 covers the remaining case (section 6.4).

### 5.3 `[InlineArray]`

- Inline arrays (C# 12) are fixed-size structs indexed like arrays, the safe form of fixed buffers
  ([struct types][learn-inlinearray]). .NET 10 also ships generic `InlineArray2<T>` to `InlineArray16<T>`
  ([InlineArray12][learn-inlinearray12]); X1 compiles and indexes `InlineArray4<int>`.
- X1: four `ulong`s make a 32-byte, 256-bit component mask with no heap allocation.
- Verdict: archetype signatures as inline bitsets in the common case, spilling to an array past 256 ids.

### 5.4 Static abstract interface members and generic math

- X2 and X4: `IComponent<TSelf>` with `static abstract` members, implemented by generated code, gives per-type
  metadata through a type parameter, and NativeAOT publishes both with no warning.
- Learn's NativeAOT guide for games recommends static abstract members or constraints resolved at compile time
  over generic virtual methods, which can need pre-instantiation and bloat code
  ([NativeAOT for gaming][learn-aot-gaming]).
- A static generic class (`ComponentId<T>.Value`) gives O(1) ids in first-use order (X1). X2's generator
  registers components from a module initializer in ordinal name order instead, so ids are stable within an
  assembly. Across assemblies module initializers follow load order, so saved data must name components, never
  store runtime ids.
- Generic math (`INumber<T>`) works (X1) and has no core role.

### 5.5 C# 14

- Extension members: extension properties and static extension members compile and run (X1)
  ([C# 14][learn-cs14]). A physics module can declare `extension(World world) { public PhysicsWorld Physics => ...; }`
  so users write `world.Physics` while `World` knows nothing of physics.
- `field`: works (X1), cosmetic.
- First-class span conversions: arrays convert implicitly to `Span<T>` and `ReadOnlySpan<T>`, also as
  extension receivers, and `ReadOnlySpan<T>` is the better conversion target
  ([first-class span types][learn-firstclassspan]). X1 hit the consequence:
  `Span<float> f = MemoryMarshal.Cast<Position, float>(array);` fails with CS0029 because the `ReadOnlySpan`
  overload wins. Jade passes `array.AsSpan()` and avoids overload pairs that differ only in span mutability.
- Modifiers on untyped lambda parameters: `static (ref p, ref readonly v) => p.Y += v.Y` compiles against a
  known delegate type (X4), which keeps the lambda query form short.

### 5.6 Function pointers

- `delegate*<...>` compiles and runs (X1) and is NativeAOT-safe. B3 measured a function pointer per entity at
  1.89× the generated loop, slower than a delegate (1.28×). B3 does not show why; a likely reason, not checked
  here, is that the JIT can devirtualize the delegate call but not the pointer call.
- Verdict: dispatch tables called once per system, hook or chunk. Per-entity code goes through generated loops
  or struct callbacks.

### 5.7 Incremental generators, analyzers and code fixes

- SDK 10.0.401 runs Roslyn 5.9.0 (`Microsoft.CodeAnalysis.CSharp.dll` product version `5.9.0-1.26423.113`), so a
  generator references `Microsoft.CodeAnalysis.CSharp` 5.9.0 or older.
- `ForAttributeWithMetadataName` is the entry point for attribute-driven generation ([API][learn-fawmn]). The
  compiler may create a new generator instance per pass, so a generator holds no state
  ([IIncrementalGenerator][learn-iincgen]).
- X2 built a generator for components, systems and query call sites and checked caching with tracked steps:
  after adding an unrelated file, component and system models report `Unchanged` and the collected registry
  `Cached`. The models are records of strings plus a value-equatable array wrapper; no `ISymbol`, `SyntaxNode`
  or `Location` leaves a transform.
- A generator or analyzer assembly that references `Microsoft.CodeAnalysis.Workspaces` triggers RS1038, because
  command-line builds do not load Workspaces (X2). Code fixes, which need Workspaces, go in their own
  assembly. Arch.Extended's three generators reference `Microsoft.CodeAnalysis.CSharp.Workspaces` 4.1.0
  (`Arch.Extended-master/Arch.System.SourceGenerator/Arch.System.SourceGenerator.csproj:36`).
- Traps met in X2: a generator targets netstandard2.0, so records need an `IsExternalInit` polyfill; a
  `record struct` is a `RecordDeclarationSyntax`, so a predicate on `StructDeclarationSyntax` silently skips it;
  a property cannot return a `ReadOnlySpan<string>` collection expression (CS9203); members of a positional
  record are properties, so a generated visitor copies them instead of passing them by `ref` (X4).

### 5.8 Interceptors

- Learn still calls interceptors experimental: the C# 12 page warns they may change or be removed
  ([C# 12][learn-cs12-interceptors]), and EF Core's NativeAOT page says they are "currently an experimental
  feature" requiring an `InterceptorsNamespaces` opt-in ([EF Core NativeAOT][learn-ef-interceptors]). The
  current location format is version 1 ([compiler messages][learn-interceptor-errors]).
- X2 on SDK 10.0.401: without the opt-in the build fails with CS9137, which names the property to add. With
  `<InterceptorsNamespaces>$(InterceptorsNamespaces);Spike.Generated</InterceptorsNamespaces>`, two
  `world.Query<Position, Velocity>()` call sites were rewritten into per-site cached queries. The location data
  from `SemanticModel.GetInterceptableLocation` embeds a checksum of the file, so any edit to a file with an
  intercepted call regenerates the output (tracked step `Modified`).
- Verdict: nothing in the design depends on interceptors. Revisit when Learn documents them as stable.

### 5.9 Browser-wasm threading

- Blazor WebAssembly runs on the browser UI thread and Blazor describes the WebAssembly model as single-threaded
  ([hosting models][learn-blazor-hosting], [synchronization context][learn-blazor-sync]).
- `Thread.Start()` carries `[UnsupportedOSPlatform("browser")]` in runtime 10.0.12 (X1), so the platform
  analyzer flags any engine code that starts a thread for a browser target.
- `OperatingSystem.IsBrowser()` exists for the runtime check ([API][learn-isbrowser]).
- Not verified: .NET's experimental multithreaded wasm mode; none of the wasm workloads is installed here.

## 6. Source generation strategy

### 6.1 What the C# libraries generate today

| Library | Generator | Input | Output | Runtime work left |
| --- | --- | --- | --- | --- |
| Arch | T4 templates, design time | none | Overloads for 1 to 25 components | none |
| Arch.Extended | `QueryGenerator` | `[Query]` methods | A cached `QueryDescription` and a chunk loop per method, an `Update` calling them | `new Signature(typeof(...))`, which reaches `MakeGenericMethod` for unregistered types |
| Arch.Extended | `ComponentRegistryGenerator` (Arch.AOT.SourceGenerator) | `[Component]` types | A module initializer calling `ArrayRegistry.Add<T>()` | Unmarked types still use reflection |
| Arch.Extended | EventBus generator | `[Event]` methods | A static `EventBus.Send` per event type | none |
| Friflo | `src/CodeGen`, a console app | none | The library's own 2 to 5 component overloads | Schema by assembly scan or `NativeAOT.Register*` |
| Friflo | none in the tree | `[Query]` attributes exist | The README announces a v3.6.0 generator that is not in the tree (`Friflo.Engine.ECS-main/README.md:56-58`) | — |

All three Arch.Extended generators use `CreateSyntaxProvider`, keep syntax nodes in the pipeline and combine
them with `CompilationProvider` (`Arch.Extended-master/Arch.System.SourceGenerator/SourceGenerator.cs:23-30`),
so any edit reruns the whole generation; their models hold symbols, and they match attributes by substring of
the name. Friflo carries 37 `UnconditionalSuppressMessage` attributes, 15 justified "TODO" and 19 "Not called
for NativeAOT", while its NativeAOT path still reaches `MakeGenericMethod` through `CreateSchema`
(`Friflo.Engine.ECS-main/src/ECS/Base/NativeAOT.cs:72-80`, `Friflo.Engine.ECS-main/src/ECS/Base/SchemaTypes.cs:123`).

### 6.2 What Jade generates

| Artifact | Input | Generated | Replaces |
| --- | --- | --- | --- |
| Component metadata | `[Component]` partial structs | `IComponent<TSelf>`: name, size, tag flag, storage kind, field visitor, hook entry points; a module initializer that registers the assembly's components in name order | Reflection-based registration, `Type` dictionaries |
| Query rows | `[Query]` partial ref structs with `ref`/`ref readonly`/`Entity`/`Has<T>` fields | `IQueryData<TSelf>`: static access list and `Fetch` | Lambdas and tuple overloads |
| Systems | `[System(Phase)]` partial structs with an `Execute` method | `ISystem`: name, phase, access set, ordering, the chunk loop calling `Execute`, change-version marking | Hand-written loops, runtime access declarations |
| Observers and hooks | `[Observer]` systems, static `OnAdd`/`OnRemove`/... on components | Registration tables of static methods | Delegates per entity |
| Serialization | Components, plus `[Serializable]` resources | Binary and JSON readers and writers per component, entity fields remapped | Reflection serializers |
| Inspector data | Field attributes (`[Range]`, `[Hidden]`, `[ReadOnly]`) | The visitor carries names, types and hints | Reflection, Godot-style `PropertyInfo` at runtime |
| Arity overloads | none | `Query<T0..T7>`, `Spawn<T0..T7>` in the engine itself | T4 |

No interceptors (section 5.8). No runtime reflection anywhere: `Jade` keeps `IsAotCompatible` with zero
warnings, as X2 and X4 already show for the generated shapes.

### 6.3 Pipeline rules

- One `ForAttributeWithMetadataName` per attribute; transforms return records of strings, numbers and
  value-equatable arrays.
- Diagnostics come from analyzers, not from the generator: a diagnostic carries a `Location`, which ties the
  model to a syntax tree and defeats caching.
- Generated code follows ADR-0011 and is checked by `dotnet format` like the bindings.
- Snapshot tests of generated output, plus a tracked-step test that an unrelated edit leaves every model
  `Cached` or `Unchanged`, as in X2.

### 6.4 Analyzers and code fixes

| Id | Severity | Rule | Code fix |
| --- | --- | --- | --- |
| JADE0001 | Error | `[Component]`, `[Query]` or `[System]` on a type that is not `partial`, or a component that is not a struct | Add `partial` |
| JADE0002 | Error | A system parameter of an unsupported type, or an `Execute` method missing | none |
| JADE0101 | Error | A structural `World` call (`Spawn`, `Add`, `Remove`, `Despawn`) inside a system's `Execute` or a query `foreach` | Replace with the `Commands` equivalent, adding a `Commands` parameter if needed |
| JADE0102 | Warning | A `ref` to a component (`world.Get<T>`, a query row) used after a structural call on the same world in the same method | none: the fix is to fetch again, which needs intent |
| JADE0103 | Info | A `ref` component parameter that the body never writes | Change to `in`, which lets the scheduler run more systems in parallel |
| JADE0201 | Warning | Two systems of the same assembly in the same phase, with conflicting access and no ordering between them | Add `[After<T>]` to one of them |
| JADE0202 | Error | An ordering cycle among `[After<T>]`/`[Before<T>]` attributes | none |
| JADE0301 | Warning | A capturing lambda passed to `Query<...>.ForEach` (allocates a closure per call) | Make it `static` when nothing is captured |
| JADE0401 | Error | A component field of a type the serializer cannot handle, on a component marked for serialization | none |

The schedule builder repeats JADE0201 at run time across assemblies, like Bevy's ambiguity detection but on by
default in debug builds. Code fixes ship in their own assembly because of RS1038 (section 5.7).

## 7. User-friendliness

### 7.1 Hello world in the C# libraries

| Library | Spawn | Query and update | System |
| --- | --- | --- | --- |
| Arch | `world.Create(new Position(0, 0), new Velocity(1, 1))` | `var q = new QueryDescription().WithAll<Position, Velocity>(); world.Query(in q, (Entity e, ref Position p, ref Velocity v) => ...)` (`Arch-master/README.md:30-48`) | `partial class MovementSystem : BaseSystem<World, GameTime>` with a constructor and `[Query] void Move([Data] GameTime t, ref Position p, ref Velocity v)`, then a `Group<GameTime>` driven by four calls per frame (`Arch.Extended-master/Arch.Extended.Sample/Systems.cs:16-44`) |
| Friflo | `world.CreateEntity(new Position(n, 0, 0), new Velocity { value = ... })` | `world.Query<Position, Velocity>().ForEachEntity((ref Position p, ref Velocity v, Entity e) => ...)` (`Friflo.Engine.ECS-main/README.md:219-233`) | `class MoveSystem : QuerySystem<Position, Velocity>` overriding `OnUpdate`, inside a `SystemRoot` (`Friflo.Engine.ECS-main/README.md:276-299`) |

Both read well, and both make the lambda the obvious path, which B3 prices at 1.28× the generated loop. Arch's
README form does not compile as written: it says `using Arch;` while the types live in `Arch.Core`
(`Arch-master/src/Arch/Core/World.cs:12`). Arch.System needs a constructor, a `[Data]` attribute for
non-component parameters, and four calls per frame. Friflo's systems are classes holding their query.

### 7.2 Jade's API, as sketches

X4 compiles and runs these sketches, against hand-written stand-ins for the generated code and a stub runtime,
and publishes them with NativeAOT with no warning.

Sketch S1, components, spawning, an inline query and a lambda query:

```csharp
var world = new World();
world.InsertResource(new Time { Delta = 1f / 60f });

var player = world.Spawn(new Position(0, 0), new Velocity(1, 2), new Player());
var rock = world.Spawn(new Position(5, 5));

ref var pos = ref world.Get<Position>(player);
pos.X = 10;

// Inline query: a generated query struct with ref fields, no lambda, no allocation.
foreach (var m in world.Query<Movers>())
{
    m.Position.X += m.Velocity.X;
}

// Quick lambda form for tools and tests (C# 14: modifiers on untyped lambda parameters).
world.Query<Position, Velocity>().ForEach(static (ref p, ref readonly v) => p.Y += v.Y);

[Component]
public partial record struct Position(float X, float Y);

[Component]
public partial record struct Velocity(float X, float Y);

/// <summary>A tag: no fields, so no column; it only changes the archetype.</summary>
[Component]
public partial struct Player;

[Query]
public ref partial struct Movers
{
    public ref Position Position;
    public ref readonly Velocity Velocity;
    public Entity Entity;
}
```

Sketch S2, systems. A system is a type, so ordering constraints and registration are type-checked and need no
reflection; its fields are system-local state:

```csharp
var schedule = new Schedule(world, threading: ScheduleThreading.Auto);
schedule.Add<Move>();
schedule.Add<Integrate>();
schedule.Add<DespawnFar>();
schedule.Run();

[System(Phase.Update)]
public partial struct Move
{
    public void Execute(ref Position p, in Velocity v, Res<Time> time)
    {
        p.X += v.X * time.Value.Delta;
        p.Y += v.Y * time.Value.Delta;
    }
}

[System(Phase.Update)]
[After<Move>]
public partial struct Integrate
{
    public void Execute(Chunk<Position, Velocity> chunk, Res<Time> time)
    {
        var p = MemoryMarshal.Cast<Position, float>(chunk.Span0);
        var v = MemoryMarshal.Cast<Velocity, float>(chunk.Span1);
        TensorPrimitives.MultiplyAdd(v, time.Value.Delta, p, p);
    }
}

[System(Phase.PostUpdate)]
[Without<Player>]
public partial struct DespawnFar
{
    private int _despawned;

    public void Execute(Entity e, in Position p, Commands commands)
    {
        if (p.X > 4)
        {
            commands.Despawn(e);
            _despawned++;
        }
    }
}
```

Sketch S3, what the generator emits for `Move` and `Movers` (hand-written in X4):

```csharp
public partial struct Move : ISystem
{
    public static SystemInfo Info { get; } = new(
        "Game.Move",
        Phase.Update,
        [ComponentAccess.Write<Position>(), ComponentAccess.Read<Velocity>(), ComponentAccess.ReadResource<Time>()],
        runsAfter: []);

    public void Run(World world)
    {
        var time = world.Resource<Time>();
        foreach (var table in world.Tables(Info.Access))
        {
            var c0 = table.Column<Position>();
            var c1 = table.Column<Velocity>();
            for (var i = 0; i < c0.Length; i++)
            {
                Execute(ref c0[i], in c1[i], time);
            }
        }
    }
}

public ref partial struct Movers : IQueryData<Movers>
{
    public static ReadOnlySpan<ComponentAccess> Access => s_access;

    private static readonly ComponentAccess[] s_access =
    [
        ComponentAccess.Write<Position>(),
        ComponentAccess.Read<Velocity>(),
    ];

    public static Movers Fetch(TableView table, int row) => new()
    {
        Position = ref table.Column<Position>()[row],
        Velocity = ref table.Column<Velocity>()[row],
        Entity = table.Entities[row],
    };
}
```

The real loop also skips chunks that a `Changed<T>` filter rules out and bumps the written columns' change
versions; the stub leaves both out.

The parameter list carries the whole contract: `ref` writes, `in` reads, `Res<T>` reads a resource,
`ResMut<T>` writes one, `Commands` defers structural changes, `Entity` is the current entity, `Chunk<...>`
switches to per-chunk calls, `Query<TRow>` gives a nested query, `MessageReader<T>`/`MessageWriter<T>` handle
messages. Filters are attributes (`[With<T>]`, `[Without<T>]`, `[Changed<T>]`).

### 7.3 Error messages

Errors name the entity, what it has, and what to do. Examples of the intended wording:

- Stale handle: "Entity 42v3 is not alive: index 42 is at generation 4. The entity was despawned, or its handle
  was kept across a world reset."
- Missing component: "Entity 42v3 ("Player") has no Position. It has Velocity, Player. Use TryGet when the
  component is optional."
- Structural change while iterating: "Cannot add Velocity to entity 7v1 while Game.Move is iterating. Record
  the change with Commands; it applies at the next sync point."
- Ambiguity: "Game.Move and Game.Integrate both write Position in Update with no order between them. Add
  `[After<Move>]` to Integrate, or `[Before<Integrate>]` to Move."

Compile-time diagnostics (section 6.4) catch most of these before run time.

### 7.4 Debugging

- `[DebuggerDisplay]` on `Entity` (`42v3`), and debugger proxies on `World`, archetypes and chunks that list
  components through the generated visitors, without boxing.
- A debug-only registry maps an `Entity` to the world that last touched it, so the debugger can show its
  components even though the handle holds no world reference.
- Per-system timings and allocation counts, as Friflo's `SetMonitorPerf` provides
  (`Friflo.Engine.ECS-main/src/ECS/Systems/SystemGroup.cs:311-336`).
- A schedule dump: phases, systems, access sets, sync points and detected ambiguities, as text or Graphviz
  (EnTT exports its graphs to Graphviz, `entt-main/docs/md/graph.md:114-138`).

### 7.5 How an ImGui inspector reads the world

The inspector walks `world.Inspect(entity)` and calls each component's generated `Visit` with a visitor that
draws widgets. X4 runs the same path with a console visitor:

```csharp
foreach (var component in world.Inspect(player))
{
    var printer = new ConsolePrinter();
    component.Accept(ref printer);
}

public struct ConsolePrinter : IComponentVisitor
{
    public void Field(string name, ref float value) => Console.WriteLine($"  {name} = {value}");

    public void Begin(ComponentInfo info) => Console.WriteLine($"{info.Name} ({info.Size} bytes)");
}
```

An ImGui visitor implements `Field` with `ImGui.DragFloat` and friends from the 311 bindings and reads hints
(`[Range]`, `[ReadOnly]`) from the generated metadata. Writes go straight through the `ref`, so editing a
component in the inspector is a normal write, recorded by change detection like any other. One metadata
source feeds the inspector and the serializers, as Godot's `PropertyInfo` does.

## 8. Performance

### 8.1 Published numbers

Neither Arch, flecs, EnTT nor Bevy publishes numbers in its tree; Arch, EnTT and Friflo point to external
benchmark projects. Friflo publishes a subset of Doraku's Ecs.CSharp.Benchmark, dated 2024-05-29, on a Mac
Mini M2, for its package 2.0.0-preview.3; the .NET version is not stated
(`Friflo.Engine.ECS-main/docs/doraku-benchmark.md:4-10`).

| Benchmark (Friflo's table) | Friflo | Arch | Source |
| --- | --- | --- | --- |
| Create 100,000 entities with three components | 405.3 μs | 6,980.1 μs | `Friflo.Engine.ECS-main/docs/doraku-benchmark.md:18-36` |
| Update 100,000 entities with two components, one thread | 57.62 μs | 62.09 μs; 52.43 μs source-generated | `Friflo.Engine.ECS-main/docs/doraku-benchmark.md:40-80` |
| Same, Friflo multi-threaded / SIMD on one thread | 17.17 μs / 11.00 μs | — | same |

These numbers are two years old, come from one machine, and were published by one of the contestants. They
rank nothing; they only show that the two C# libraries iterate within the same order of magnitude and that
creation strategies differ widely.

### 8.2 Jade spikes

Run on 2026-10-03: AMD Ryzen 7 7800X3D (8 cores, 96 MiB L3), CachyOS with Linux 7.2.8, SDK 10.0.401,
runtime 10.0.12 (RyuJIT, x86-64-v4), BenchmarkDotNet 0.15.8, default job. Other work ran on the machine, so
only ratios within one run mean anything.

B1, `p += v * dt` over two columns of three-float components, mean time:

| Entities | Scalar loop | `System.Numerics.Vector3` | Flattened `Vector128` | `TensorPrimitives.MultiplyAdd` |
| --- | --- | --- | --- | --- |
| 1,000 | 726.7 ns | 668.6 ns (0.92) | 322.8 ns (0.44) | 229.9 ns (0.32) |
| 100,000 | 79.6 μs | 68.2 μs (0.86) | 62.3 μs (0.78, standard deviation 11.6 μs) | 59.6 μs (0.75) |
| 4,000,000 | 3.51 ms | 2.77 ms (0.79) | 1.75 ms (0.50) | 1.79 ms (0.51) |

At 100,000 entities (2.4 MB) every variant waits on the cache hierarchy; one `TensorPrimitives` case was
flagged multimodal.

B2, the same scalar kernel over 1,000,000 entities stored in chunks of N entities (two 12-byte components):

| Entities per chunk | 64 | 682 (16 KiB) | 2,730 (64 KiB) | 1,000,000 (one table) |
| --- | --- | --- | --- | --- |
| Mean | 867.2 μs | 776.2 μs | 745.7 μs | 755.7 μs |
| Against one table | +14.8 % | +2.7 % | −1.3 % | — |

B3, per-entity dispatch over 100,000 entities:

| Generated loop | Struct callback | Lambda | Function pointer |
| --- | --- | --- | --- |
| 77.87 μs (1.00) | 77.53 μs (1.00) | 99.85 μs (1.28) | 147.42 μs (1.89) |

Decisions drawn from them: chunks with a 16 KiB default budget (B2), generated loops and struct callbacks for
systems, lambdas only as a convenience (B3), chunk systems as the place for SIMD (B1).

### 8.3 Proposed benchmark suite

A BenchmarkDotNet project that references Arch and Friflo as packages, so every comparison runs on the same
machine, date and runtime. Scenarios follow Doraku's names where they exist, so results can be read next to
the public tables:

- Create N entities with 1, 2 and 3 components; in bulk and one by one; through `Commands`.
- Iterate with one, two and three components; with a filter; on a fragmented world (many small archetypes);
  generated system, query row, chunk span, lambda.
- Add and remove a component; add and remove a tag; despawn and respawn churn; command playback of 10,000 mixed
  operations.
- `Changed<T>` over 1 %, 50 % and 100 % changed chunks.
- Hierarchy: propagate transforms through 10,000 nodes, wide and deep.
- Observers and messages: throughput of 100,000 events.
- Scheduler: overhead of 100 empty systems; scaling from 1 to 8 threads on a parallel-friendly schedule.
- The same suite under NativeAOT, and a browser-wasm run reported separately (single-threaded).

CI runs the short job and records results without failing on them: shared runners are too noisy to gate.

## 9. Modernization

What Jade can do better than the C# libraries today:

- Reflection-free by construction: a generated registry, generated serializers and visitors; no
  `Array.CreateInstance`, no assembly scanning, no suppressed trimming warnings.
- Incremental generators that stay cached, and analyzers with code fixes, where Arch.Extended regenerates
  everything on each edit and Friflo ships no generator.
- A scheduler that derives order and parallelism from declared access, with a single-threaded executor that
  is first-class on browser-wasm instead of a missing job scheduler (Arch) or a spinning thread pool (Friflo).
- Change detection, which neither has for value writes.
- Relationships that cost no archetype churn, with cleanup policies, where Arch's need `EVENTS` builds and
  Friflo orphans children.
- Handles that are 8 bytes, blittable, and always checked.
- Error messages that name the entity and the fix, compile-time diagnostics for the common mistakes.

What Jade deliberately does not do:

- No classes or scripts as components (Friflo's `Script`): behaviour lives in systems.
- No relationship pairs in the archetype key, no runtime query language, no runtime-defined components until
  an editor or scripting need appears (flecs).
- No interceptors, no reflection fallback, no compile-time feature switches that turn APIs into silent no-ops
  (Arch's `EVENTS`).
- No 25-component overloads: eight components per query or spawn, query rows beyond that.
- No built-in game components (`Position`, `Transform`) in the ECS assembly: they belong to engine modules.
- No promise of entity order inside a chunk across structural changes; order is defined only for the
  single-threaded executor and documented as such.

## 10. Experiments

All throwaway, in the session scratchpad, not committed.

| Id | What | Command | Result |
| --- | --- | --- | --- |
| X1 | C# 11 to 14 features an ECS needs: static abstract members, `[InlineArray]` and `InlineArray4<T>`, `allows ref struct` callback, ref fields, function pointers, extension members, `field`, first-class spans, generic math; `Thread.Start` attributes | `dotnet run features.cs` | All compile and run on SDK 10.0.401; CS0029 on `MemoryMarshal.Cast` of an array (section 5.5); `Thread.Start` is `[UnsupportedOSPlatform("browser")]`; `Vector128/256/512.IsHardwareAccelerated` true here, `Vector<float>.Count` 8 |
| X2 | Incremental generator (netstandard2.0, Roslyn 5.9.0) for components, systems and interceptors; caching check through `GeneratorDriver` tracked steps; NativeAOT publish; RS1038 check | `dotnet build`, `dotnet run` on the check project, `dotnet publish -r linux-x64` | Generated code builds and runs; CS9137 until `InterceptorsNamespaces` is set; after an unrelated file, models `Unchanged`/`Cached`; after a comment line in the intercepted file, interceptors `Modified`; AOT publish with no warning (1.5 MB binary); RS1038 when Workspaces is referenced |
| X3 | Benchmarks B1, B2, B3 (BenchmarkDotNet 0.15.8, `System.Numerics.Tensors` 10.0.12), after an equivalence check of the SIMD kernels | `dotnet run -c Release -- --check`, then `--filter '*ColumnSimd*'` and so on | Section 8.2; flattened kernels equal the scalar loop bit for bit at all tested lengths |
| X4 | The API sketches of section 7 with hand-written generated code and a stub runtime | `dotnet run sketch.cs`, `dotnet publish sketch.cs` (`TreatWarningsAsErrors`, `IsAotCompatible`) | Compiles, runs and prints the expected state; NativeAOT publish with no warning |

## References

Learn pages, read 2026-10-03:

- [C# 14][learn-cs14], [first-class span types][learn-firstclassspan], [C# 12 interceptors][learn-cs12-interceptors]
- [Constraints, `allows ref struct`][learn-allows-ref-struct], [inline arrays][learn-inlinearray],
  [`InlineArray12<T>`][learn-inlinearray12]
- [SIMD and hardware intrinsics][learn-simd], [`TensorPrimitives.MultiplyAdd`][learn-multiplyadd],
  [WebAssembly runtime performance][learn-wasm-perf]
- [NativeAOT for gaming][learn-aot-gaming], [NativeAOT limitations][learn-aot-limits], [IL3050][learn-il3050]
- [`ForAttributeWithMetadataName`][learn-fawmn], [`IIncrementalGenerator`][learn-iincgen],
  [source generator and interceptor errors][learn-interceptor-errors], [EF Core NativeAOT][learn-ef-interceptors]
- [Large object heap][learn-loh], [`OperatingSystem.IsBrowser`][learn-isbrowser],
  [Blazor hosting models][learn-blazor-hosting], [Blazor synchronization context][learn-blazor-sync]

Other:

- RS1038, linked from the compiler message in X2: <https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1038.md>
- Doraku's Ecs.CSharp.Benchmark, cited by Friflo: <https://github.com/Doraku/Ecs.CSharp.Benchmark>

[learn-cs14]: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-14
[learn-firstclassspan]: https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-14.0/first-class-span-types
[learn-cs12-interceptors]: https://learn.microsoft.com/dotnet/csharp/whats-new/csharp-12#interceptors
[learn-allows-ref-struct]: https://learn.microsoft.com/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters#allows-ref-struct
[learn-inlinearray]: https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/struct#inline-arrays
[learn-inlinearray12]: https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.inlinearray12-1?view=net-10.0
[learn-simd]: https://learn.microsoft.com/dotnet/standard/simd
[learn-multiplyadd]: https://learn.microsoft.com/dotnet/api/system.numerics.tensors.tensorprimitives.multiplyadd
[learn-wasm-perf]: https://learn.microsoft.com/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-10.0
[learn-aot-gaming]: https://learn.microsoft.com/gaming/gdk/docs/gdk-dev/pc-dev/tutorials/get-started-with-custom-engine/native-aot-for-gaming
[learn-aot-limits]: https://learn.microsoft.com/dotnet/core/deploying/native-aot/#limitations-of-native-aot-deployment
[learn-il3050]: https://learn.microsoft.com/dotnet/core/deploying/native-aot/warnings/il3050
[learn-fawmn]: https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.syntaxvalueprovider.forattributewithmetadataname
[learn-iincgen]: https://learn.microsoft.com/dotnet/api/microsoft.codeanalysis.iincrementalgenerator
[learn-interceptor-errors]: https://learn.microsoft.com/dotnet/csharp/language-reference/compiler-messages/source-generator-errors
[learn-ef-interceptors]: https://learn.microsoft.com/ef/core/performance/nativeaot-and-precompiled-queries
[learn-loh]: https://learn.microsoft.com/dotnet/standard/garbage-collection/large-object-heap
[learn-isbrowser]: https://learn.microsoft.com/dotnet/api/system.operatingsystem.isbrowser
[learn-blazor-hosting]: https://learn.microsoft.com/aspnet/core/blazor/hosting-models?view=aspnetcore-10.0#blazor-webassembly
[learn-blazor-sync]: https://learn.microsoft.com/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0
