# 307: stb_image and Vorbis through miniaudio

- Depends on: 201, 103, 204
- ADRs: 0003, 0005, 0006, 0015

## Goal

Stb_image v2.30 is linked into jade_native and bound. The miniaudio package compiles its
bundled stb_vorbis so that `ma_decoder` and `ma_sound` play Ogg Vorbis.

## Context

Promoted from draft C of the 304 survey (see its Outcome for the evidence).

Stb has no tags: pin a commit (master was `2c980bb5` on 2026-10-03). stb_image is
114 KiB with every format. Build with `STBI_NO_STDIO`, so only the memory and callback APIs exist
and .NET does the file I/O (no `fopen` path-encoding issues on Windows or with Android assets).
miniaudio enables Vorbis when stb_vorbis's header is included before its implementation and
stb_vorbis's implementation after it (the translation unit in ADR-0015 M4), which costs 67 KiB.

## Scope

Stb package, `stbi_*` exports, generator config. miniaudio package change (its generated
xmake.lua compiles a wrapper translation unit) and the `defines` record in `versions.json`.
Tests: decode committed PNG (8- and 16-bit) and JPEG images and check dimensions and a few
pixels; decode a short committed Ogg file through `ma_decoder` and check channels, sample rate
and frames read.

## Out of scope

Image writing and resizing, Opus.

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

`stbi_set_flip_vertically_on_load` is global state (a `_thread` variant exists).
`stb_vorbis_*` symbols must stay hidden: miniaudio exports `ma_*` only. With stb_vorbis,
`ma_decoder_get_length_in_pcm_frames` returns 0, so tests must not expect a length. stb_image is
not hardened against hostile input (stb README).

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
