# 305: zstd

- Depends on: 201, 103
- ADRs: 0003, 0004, 0005, 0006, 0015, 0018

## Goal

Zstd v1.5.7 (full library, single-threaded) is linked into jade_native for the desktop RIDs and
bound in `Jade.Interop`. meshoptimizer is out of the plan: Jade is a 2D engine (ADR-0018).

## Context

Promoted from draft A of the 304 survey (see its Outcome for the evidence).

Zstd's stable API is `zstd.h` without `ZSTD_STATIC_LINKING_ONLY`. Full library 585 KiB,
decompression only 146 KiB. The basis_universal transcoder (306) will use this zstd.

## Scope

The zstd package and its `bundled` entry. Decide whether `ZDICT_*` (dictionary training) is
exported and record its size. Tests: a zstd round-trip with `ZSTD_compress`/`ZSTD_decompress`, and
`ZSTD_getFrameContentSize` on the result.

## Out of scope

Mobile and browser RIDs (104, 105), multi-threaded zstd.

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

Zstd ships x86-64 assembly (`lib/decompress/huf_decompress_amd64.S`). Check how each
toolchain handles it, and use `ZSTD_DISABLE_ASM` where needed. `ZSTD_CONTENTSIZE_UNKNOWN` and
`ZSTD_CONTENTSIZE_ERROR` are macros over `unsigned long long` (`0ULL - 1`, `0ULL - 2`). A `ZSTD_*`
export pattern would leak internals. The full library's objects define 290 global `ZSTD_*`
functions, of which only 75 are declared `ZSTDLIB_API` in `zstd.h`, plus 46 `HUF_*` and `FSE_*`
ones. Export the exact `ZSTDLIB_API` names (`stage.lua` accepts exact names), plus `ZDICT_*` if
chosen.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
