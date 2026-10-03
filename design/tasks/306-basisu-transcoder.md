# 306: basis_universal transcoder

- Depends on: 305, 201, 103
- ADRs: 0003, 0005, 0006, 0012, 0015

## Goal

The basis_universal v2_50 transcoder and its C API (`transcoder/basisu_transcoder.cpp` plus
`encoder/basisu_wasm_transcoder_api.cpp`) are linked into jade_native and bound. A test transcodes
committed KTX2 samples to the formats WebGPU can sample.

## Context

Promoted from draft B of the 304 survey (see its Outcome for the evidence).

Build with `BASISD_SUPPORT_KTX2_ZSTD=1` against 305's zstd (not basisu's
`zstddeclib.c`), and with `BASISD_SUPPORT_PVRTC1=0`, `BASISD_SUPPORT_PVRTC2=0`,
`BASISD_SUPPORT_ATC=0` and `BASISD_SUPPORT_FXT1=0`: 1,190 KiB instead of 1,406 KiB. It needs
C++17 and compiles with `-fno-exceptions` (M4). The C API covers KTX2 only. Its header says DDS is
not in the C wrappers yet.

## Scope

Package, exports `bt_*`, generator config, tests. Transcode an ETC1S, a UASTC LDR, a UASTC
HDR and an XUBC7 sample to BC7, BC6H, ETC2 RGBA, ASTC 4x4 and RGBA32 where supported
(`bt_basis_is_format_supported`), and check output sizes against
`bt_basis_compute_transcoded_image_size_in_bytes`. Samples are made once with basisu's CLI from
an image under a permissive license, committed under `tests/` with that license.

## Out of scope

The encoder (the deferred jade_tools work), KTX2 container parsing for non-Basis payloads (engine).

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

Pointers travel as `uint64_t` offsets and booleans as `uint32_t` (`wasm_bool_t`). Keep
those widths (ADR-0012) and add a hand-written helper only if the raw form is unusable.
`bt_ktx2_open` keeps the data pointer until `bt_ktx2_close`, so no span overload for it.
`bt_get_version` prints to stdout, so the smoke check must not call it, or must accept the noise.
`bt_init` must run once before transcoding.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
