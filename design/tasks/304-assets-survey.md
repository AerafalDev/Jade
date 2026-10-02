# 304: Survey: asset libraries

- Depends on: 201
- ADRs: 0003

## Goal

A Proposed ADR that says which asset libraries Jade binds, split between **runtime** (shipped in
`jade_native` on every RID) and **import-time tooling** (editor and asset pipeline only, possibly a
separate native library). It also comes with draft briefs for the selected libraries.

## Context

- The user wants this area "dissected" for what the engine actually needs. The engine is not
  designed yet, so the survey must state the assumptions it makes about the asset pipeline (for
  example: assets cooked at import time into GPU-ready formats, the runtime loading cooked data).
- miniaudio already embeds WAV, FLAC and MP3 decoders. Count them before adding audio decoders.

## Scope

Research only. For each candidate: license, maintenance, C API (or shim cost), size, wasm and
mobile support, and the role it plays (runtime or tooling).

- Images: stb_image, libspng, libjpeg-turbo, wuffs.
- GPU textures: KTX-Software (libktx), basis_universal, bc7enc or other encoders.
- Models: cgltf, ufbx; fastgltf (C++) and assimp (heavy) as comparison points.
- Meshes: meshoptimizer.
- Compression: zstd, lz4.
- Audio: anything beyond miniaudio's decoders (stb_vorbis, Opus).
- Anything else a modern engine pipeline commonly uses (for example image resizing, mikktspace
  tangents).

Then:

- Propose the runtime / tooling split and whether tooling needs its own native library (for example
  `jade_tools`, a desktop-only RID set). That would be a new ADR, extending ADR-0003.

## Out of scope

- Implementation, asset pipeline design (engine phase).

## Acceptance criteria

- [ ] A Proposed ADR with a decision table, assumptions and sources.
- [ ] Draft briefs in the Outcome for each selected library or group.

## Verification

Every claim links to its source and is dated.

## Pitfalls

- Overlap between candidates (stb_image vs libspng and libjpeg-turbo; KTX vs basis_universal).
  Pick one per need and justify; do not bind everything.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
