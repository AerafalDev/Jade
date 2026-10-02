# 302: Box2D v3 and Box3D

- Depends on: 201, 103
- ADRs: 0003, 0004, 0005, 0006

## Goal

Box2D and Box3D are built into jade_native for the desktop RIDs from pinned packages and bound into
`src/Jade.Interop/Generated/Box2D/` and `.../Box3D/`, with smoke tests stepping a world in each.

## Context

- Box2D `v3.1.1` and Box3D `v0.1.0` (first release, 2026-06-30, MIT). Box3D's public C headers are
  `include/box3d/{base,box3d,collision,config,constants,id,math_functions,types}.h`. At v0.1, expect
  API churn: pin exactly.
- Both use typed id structs (`b2BodyId`, ...) passed by value. These map naturally to ADR-0006
  handles with instance methods.

## Scope

- xmake packages with pinned versions. SIMD options chosen for portability per RID (no AVX2
  baseline on x64 unless the user agrees), with determinism-related options recorded.
- Exports through the 101 mechanism; licenses in notices.
- Generator configs: id types as handles, math types (`b2Vec2`, ...) as blittable structs, task
  system callbacks as function pointers.
- Mobile and web: confirm they build in 104 and 105 pipelines, or file follow-ups.
- Tests: create a world, add a dynamic body over a static ground, step N times, assert the body
  fell and came to rest, for each engine.

## Out of scope

- Engine physics abstraction.

## Acceptance criteria

- [ ] Both libraries are in jade_native for linux-x64, with the other desktop RIDs in the CI
      workflow.
- [ ] Deterministic generated output; clean build; the export cross-check passes.
- [ ] Both world-stepping tests pass.

## Verification

Native build, generator run, test run.

## Pitfalls

- Box2D and Box3D may share helper symbol names, either internal or public `b2`/`b3` prefixed.
  Check for duplicate symbols when linking both into one library.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
