# 302: Box2D v3

- Depends on: 201, 103
- ADRs: 0003, 0004, 0005, 0006, 0018

## Goal

Box2D is built into jade_native for the desktop RIDs from a pinned package and bound into
`src/Jade.Interop/Generated/Box2D/`, with a smoke test stepping a world.

## Context

- Jade is a 2D engine (ADR-0018), so Box3D is out of the plan.
- Box2D `v3.1.1` on 2026-10-02; re-check the latest release before pinning.
- Box2D uses typed id structs (`b2BodyId`, ...) passed by value. They map naturally to ADR-0006
  handles with instance methods.

## Scope

- An xmake package with a pinned version. SIMD options chosen for portability per RID (no AVX2
  baseline on x64 unless the user agrees), with determinism-related options recorded.
- Exports through the 101 mechanism, as an exact list (CLAUDE.md); license in the notices.
- Generator config: id types as handles, math types (`b2Vec2`, ...) as blittable structs, task
  system callbacks as function pointers (ADR-0016 for the imports).
- Mobile and web: confirm the build in the 104 and 105 pipelines, or file follow-ups.
- Test: create a world, add a dynamic body over a static ground, step N times, and assert that the
  body fell and came to rest.

## Out of scope

- Engine physics abstraction.

## Acceptance criteria

- [ ] Box2D is in jade_native for linux-x64, with the other desktop RIDs in the CI workflow.
- [ ] Deterministic generated output; clean build; the export cross-check passes.
- [ ] The world-stepping test passes.

## Verification

Native build, generator run, test run.

## Pitfalls

- Box2D v3's task system expects the engine's scheduler. Expose it as function pointers and
  document that the default single-threaded path needs none.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
