# 304: Survey: asset libraries

- Depends on: 201, 003
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

- Summary: [ADR-0015](../adr/0015-asset-libraries.md) (Proposed) states six assumptions about the
  pipeline (desktop-only import, textures cooked to KTX2 with a Basis Universal payload and
  transcoded at load, meshoptimizer-processed meshes, zstd for bulk data, audio shipped as
  WAV/FLAC/MP3/Ogg, PNG/JPEG still loadable at runtime) and picks one library per need. Runtime set,
  in `jade_native` on every RID: zstd 1.5.7, the basis_universal v2_50 transcoder through its own C
  API (`bt_*`), stb_image 2.30, meshoptimizer 1.3, and stb_vorbis compiled into the existing
  miniaudio package. Import set, in a new desktop-only combined library `jade_tools`: the
  basis_universal encoder (`bu_*`), cgltf 1.15 and ufbx 0.23.1. Every other candidate in the brief is
  rejected or deferred with a reason and a source. Measured on linux-x64 (clang `-O2`, text+data):
  the runtime set adds about 2.1 MiB to today's 11.6 MiB `jade_native`; the import set weighs about
  5.2 MiB. Main findings that drove the picks:
  - WebGPU (Dawn's `dawn.json` at the pinned tag) has BC, ETC2 and ASTC only as optional features,
    and ASTC only as LDR, so a single cooked texture must be transcoded per device.
  - basis_universal v2.x ships a plain C API (`extern "C"` natively), so it needs no shim. Its
    transcoder needs only `ZSTD_decompress`, `ZSTD_isError` and `ZSTD_getFrameContentSize`.
  - libktx lags basisu (v4.4.2 embeds basisu 1.16, from before UASTC HDR; v5.0.0-rc2 embeds 2.10)
    and compiles its own zstd. It is rejected.
  - meshoptimizer 1.3 made `meshopt_generateTangents` stable, with a MikkTSpace-compatible mode, so
    MikkTSpace is not needed.
  - miniaudio already decodes WAV, MP3 and FLAC, and ships a byte-identical stb_vorbis that it uses
    when compiled in (+67 KiB, no new binding).
  - Wuffs' only stable version (0.3) has no JPEG decoder, and its 0.4 alpha interface has 469
    `static inline` functions. stb_image covers the need in 114 KiB.
  - .NET 10 has no Zstandard API (the 10.0.12 ref pack has none; .NET 11 adds one, unsupported on
    browser), so zstd needs a binding.
- Verification (commands and results), on 2026-10-03, CachyOS, clang 23.1.1, CMake 4.4.3, zig
  0.16.0. Measurement scripts were throwaway bash scripts in the session scratchpad and are not
  committed (repository automation must be cross-platform C#). The ADR's Measurements section
  describes each one well enough to rerun it.
  - Metadata: `gh api repos/<owner>/<repo>`, `.../releases/latest` and `.../releases` for 35
    repositories, `git ls-remote --tags` for projects without GitHub releases (Wuffs, ufbx, Opus,
    stb, bc7enc_rdo), and `gh api repos/<r>/commits/<tag>` for tag dates. The results are in the
    ADR's "Upstream snapshot" table.
  - Sources: 30 shallow checkouts of 28 projects at their latest tag (two versions each of
    KTX-Software and Wuffs; KTX-Software needed a second checkout without git-lfs smudge). Licenses
    were read from the license files, because the API reports `NOASSERTION` for 11 of them.
  - Sizes (M2): 23 configurations of 16 single-file or small libraries compiled with
    `clang -O2 -fPIC -c` and summed with `size -t` (text+data). 8 CMake projects built as static
    Release libraries with Ninja. assimp failed the first time because CMake found an incomplete
    system minizip (`unzip.h` not found). It built with `-DASSIMP_BUILD_ZLIB=ON`. bc7enc_rdo needed
    `-include cstdint`.
  - `nm --defined-only libbasisu_encoder.a`: 273 `ZSTD_*`, 22 `TinyDDS_*`, 4 `qoi_*` and tinyexr's
    `LoadEXR`/`SaveEXR`/`IsEXR` family, all global.
  - `git hash-object` of the staged `dawn.json` equals the blob of `src/dawn/dawn.json` at
    `v20260930.214659` (`f53d1a62`), so S1 links to the exact file jade_native builds from.
  - Cross-compilation smoke test (M4) with `zig cc`/`zig c++`: stb_image, the basisu transcoder and
    its C API, zstd and meshoptimizer compile for `wasm32-wasi`, `aarch64-linux-gnu`,
    `aarch64-macos` and `x86_64-windows-gnu`. miniaudio with stb_vorbis compiles for
    `aarch64-linux-gnu` and `x86_64-windows-gnu`. On `aarch64-macos` it fails only because the host
    has no Apple SDK. Nothing was linked or run. Emscripten, the Android NDK and the iOS SDK were
    not used, so the mobile and browser rows remain unverified until 104 and 105.
  - No build or test of the repository was needed: the change touches `design/` only.
- Decisions taken (and ADRs added): ADR-0015, Proposed. It extends ADR-0003 with a second combined
  library, `jade_tools`, built the same way but only for the six desktop RIDs. jade_tools never
  exports a symbol that jade_native exports, keeps private hidden copies of what its upstreams need,
  and does not link against jade_native. It extends ADR-0002 with a separate package pair for
  jade_tools, referenced only by the editor and cooker, whose names are left to the jade_tools
  task. Rejected or deferred, with reasons in the ADR: libktx, astc-encoder, bc7enc_rdo,
  Compressonator, ISPC Texture Compressor, lz4, Wuffs, libspng, libjpeg-turbo, MikkTSpace, fastgltf,
  assimp, Draco, Opus, libvorbis, stb_image_resize2, stb_image_write, tinyexr, OpenEXR, xatlas and
  msdf-atlas-gen.
- Deviations from the brief:
  - Platform support was checked through upstream CI and documentation, plus a compile-only zig
    smoke test. No Emscripten, NDK or iOS build was made: none of those toolchains is installed.
  - The ADR extends ADR-0002 (packages) as well as ADR-0003, since a tooling library needs its own
    package to stay out of shipped games.
  - The brief's audio item named stb_vorbis and Opus as candidates. stb_vorbis is selected, but
    inside the miniaudio package rather than as a separately bound library.
- Follow-ups:
  - ADR number: task 301 runs in parallel and will likely also write an ADR-0015. Whichever merges
    second renumbers its ADR and the links to it.
  - Task 303 left msdf-atlas-gen to this survey: it is deferred. Runtime glyph generation needs only
    msdfgen, glyph packing is managed code, and offline atlases would put msdf-atlas-gen into
    jade_tools behind a shim.
  - Task 105: decide whether browser-wasm gets a decompression-only zstd (about 440 KiB less, but
    the binding would differ per RID). Also check how basisu's `BU_WASM_EXPORT` macro, which adds
    `export_name` when `__wasm__` is defined, behaves under Emscripten.
  - Task 106: jade_native grows by about 2.1 MiB per RID, which counts toward the size guard.
    basis_universal is Apache-2.0 with a `NOTICE` file that must be reproduced in
    `THIRD-PARTY-NOTICES.md`.
  - Once ADR-0015 is accepted, `design/architecture.md` (layers, packages, layout) needs
    jade_tools.
  - Engine phase: confirm or revise A1 to A6. Re-encoding audio at import (Vorbis or Opus encoders
    in jade_tools) and hardened image decoding (Wuffs once 0.4 is stable) are open.
  - When Jade moves to .NET 11, managed code could use the BCL `ZstandardStream` on non-browser
    RIDs. basisu still needs the native zstd.
  - The roadmap row for 304 lists only 201 under "Depends on", while the brief and the Phase 0 note
    also list 003.

### Draft briefs

The orchestrator numbers and finalizes these. Every version is a 2026-10-03 snapshot, and every
size is from ADR-0015's M2. Common to all: pin each upstream with its SHA-256 in
`native/packages/`, record exports through `modules/stage.lua`, add licenses to
`THIRD-PARTY-NOTICES.md`, generate bindings with `scripts/generate-bindings.cs`, and pass the
export cross-check.

#### Draft A: zstd and meshoptimizer in jade_native

- Depends on: 201, 103. ADRs: 0003, 0004, 0005, 0006, 0015.
- Goal: zstd v1.5.7 (full library, single-threaded) and meshoptimizer v1.3 are linked into
  jade_native for the desktop RIDs and bound in `Jade.Interop`.
- Context: zstd's stable API is `zstd.h` without `ZSTD_STATIC_LINKING_ONLY`. Full library 585 KiB,
  decompression only 146 KiB. meshoptimizer's C API sits inside an `extern "C"` block of
  `src/meshoptimizer.h`, followed by C++ template overloads that a C parse does not see. Its
  functions are marked `MESHOPTIMIZER_API` (stable) or `MESHOPTIMIZER_EXPERIMENTAL` (both expand to
  the same thing by default). meshoptimizer is 238 KiB. The basis_universal transcoder (Draft B)
  will use this zstd.
- Scope: both packages and their `bundled` entries. Decide whether `ZDICT_*` (dictionary training)
  is exported and record its size. Decide whether experimental meshoptimizer functions are bound;
  a generator filter is the likely way. Tests: zstd round-trip with `ZSTD_compress`/`ZSTD_decompress`
  and `ZSTD_getFrameContentSize`; meshopt vertex and index codec round-trip;
  `meshopt_generateTangents` on a small mesh, with and without `meshopt_TangentCompatible`.
- Out of scope: mobile and browser RIDs (104, 105), multi-threaded zstd.
- Pitfalls: zstd ships x86-64 assembly (`lib/decompress/huf_decompress_amd64.S`). Check how each
  toolchain handles it, and use `ZSTD_DISABLE_ASM` where needed. `ZSTD_CONTENTSIZE_UNKNOWN` and
  `ZSTD_CONTENTSIZE_ERROR` are macros over `unsigned long long` (`0ULL - 1`, `0ULL - 2`). A `ZSTD_*`
  export pattern would leak internals. The full library's objects define 290 global `ZSTD_*`
  functions, of which only 75 are declared `ZSTDLIB_API` in `zstd.h`, plus 46 `HUF_*` and `FSE_*`
  ones. Export the exact `ZSTDLIB_API` names (`stage.lua` accepts exact names), plus `ZDICT_*` if
  chosen.

#### Draft B: basis_universal transcoder in jade_native

- Depends on: Draft A, 201, 103. ADRs: 0003, 0005, 0006, 0012, 0015.
- Goal: the basis_universal v2_50 transcoder and its C API (`transcoder/basisu_transcoder.cpp` plus
  `encoder/basisu_wasm_transcoder_api.cpp`) are linked into jade_native and bound. A test transcodes
  committed KTX2 samples to the formats WebGPU can sample.
- Context: build with `BASISD_SUPPORT_KTX2_ZSTD=1` against Draft A's zstd (not basisu's
  `zstddeclib.c`), and with `BASISD_SUPPORT_PVRTC1=0`, `BASISD_SUPPORT_PVRTC2=0`,
  `BASISD_SUPPORT_ATC=0` and `BASISD_SUPPORT_FXT1=0`: 1,190 KiB instead of 1,406 KiB. It needs
  C++17 and compiles with `-fno-exceptions` (M4). The C API covers KTX2 only. Its header says DDS is
  not in the C wrappers yet.
- Scope: package, exports `bt_*`, generator config, tests. Transcode an ETC1S, a UASTC LDR, a UASTC
  HDR and an XUBC7 sample to BC7, BC6H, ETC2 RGBA, ASTC 4x4 and RGBA32 where supported
  (`bt_basis_is_format_supported`), and check output sizes against
  `bt_basis_compute_transcoded_image_size_in_bytes`. Samples are made once with basisu's CLI from
  an image under a permissive license, committed under `tests/` with that license.
- Out of scope: the encoder (Draft E), KTX2 container parsing for non-Basis payloads (engine).
- Pitfalls: pointers travel as `uint64_t` offsets and booleans as `uint32_t` (`wasm_bool_t`). Keep
  those widths (ADR-0012) and add a hand-written helper only if the raw form is unusable.
  `bt_ktx2_open` keeps the data pointer until `bt_ktx2_close`, so no span overload for it.
  `bt_get_version` prints to stdout, so the smoke check must not call it, or must accept the noise.
  `bt_init` must run once before transcoding.

#### Draft C: stb_image and Vorbis through miniaudio

- Depends on: 201, 103, 204. ADRs: 0003, 0005, 0006, 0015.
- Goal: stb_image v2.30 is linked into jade_native and bound. The miniaudio package compiles its
  bundled stb_vorbis so that `ma_decoder` and `ma_sound` play Ogg Vorbis.
- Context: stb has no tags: pin a commit (master was `2c980bb5` on 2026-10-03). stb_image is
  114 KiB with every format. Build with `STBI_NO_STDIO`, so only the memory and callback APIs exist
  and .NET does the file I/O (no `fopen` path-encoding issues on Windows or with Android assets).
  miniaudio enables Vorbis when stb_vorbis's header is included before its implementation and
  stb_vorbis's implementation after it (the translation unit in ADR-0015 M4), which costs 67 KiB.
- Scope: stb package, `stbi_*` exports, generator config. miniaudio package change (its generated
  xmake.lua compiles a wrapper translation unit) and the `defines` record in `versions.json`.
  Tests: decode committed PNG (8- and 16-bit) and JPEG images and check dimensions and a few
  pixels; decode a short committed Ogg file through `ma_decoder` and check channels, sample rate
  and frames read.
- Out of scope: image writing and resizing, Opus.
- Pitfalls: `stbi_set_flip_vertically_on_load` is global state (a `_thread` variant exists).
  `stb_vorbis_*` symbols must stay hidden: miniaudio exports `ma_*` only. With stb_vorbis,
  `ma_decoder_get_length_in_pcm_frames` returns 0, so tests must not expect a length. stb_image is
  not hardened against hostile input (stb README).

#### Draft D: jade_tools skeleton with cgltf

- Depends on: 103, 106, 201. ADRs: 0002, 0003, 0004, 0005, 0006, 0015.
- Goal: a second combined library, `jade_tools`, is built for the six desktop RIDs with cgltf v1.15
  as its first upstream. Its bindings live in their own assembly, which imports `jade_tools`, and
  it ships in its own package pair.
- Context: ADR-0015's rules: no export shared with jade_native, private hidden copies of shared
  dependencies, no link against jade_native. The generator hard-codes `jade_native` in
  `scripts/generate-bindings/CSharpEmitter.cs` (both `DllImport` and `LibraryImport` forms). cgltf is
  C, 97 KiB (38 KiB more for the writer).
- Scope: xmake target and `bundled` list for jade_tools, reusing `jade.bundle`. `build-native.cs`
  and `smoke-native.cs` handle both libraries, and the smoke check fails if any export of
  jade_tools also exists in jade_native. Library name per `LibraryConfig` in the generator. New
  projects (for example `src/Jade.Tools.Interop`, packed into `Jade.Tools`, depending on
  `Jade.Tools.Native`) and the CI jobs. The final package names are settled here, in an ADR that
  extends ADR-0002. Tests: parse a committed glTF and GLB (permissive license) and check node, mesh
  and accessor counts.
- Out of scope: other tooling libraries (Drafts E and F), the editor itself.
- Pitfalls: an export present in both libraries would let ELF symbol interposition bind jade_tools'
  calls to jade_native's copy. `cgltf_load_buffers` reads external buffers from paths through
  `cgltf_options` callbacks: prefer memory input, or bind the callbacks as
  `delegate* unmanaged[Cdecl]`. `Jade` (the engine package) must not depend on the tools package.

#### Draft E: basis_universal encoder in jade_tools

- Depends on: Draft D, Draft B. ADRs: 0003, 0005, 0006, 0012, 0015.
- Goal: the basis_universal v2_50 encoder and its C API (`bu_*`, `encoder/basisu_wasm_api.cpp`)
  are linked into jade_tools and bound. A round-trip test encodes with jade_tools and transcodes
  with jade_native.
- Context: `libbasisu_encoder.a` is 4,838 KiB and already contains its own transcoder and zstd,
  plus tinyexr, QOI and TinyDDS as global symbols. The encoder generates mipmaps
  (`BU_COMP_FLAGS_GEN_MIPS_*`) and writes KTX2 (`BU_COMP_FLAGS_KTX2_OUTPUT`). SSE4.1 is an x86-64
  option (`BASISU_SSE`). OpenCL is optional.
- Scope: package (OpenCL off, SSE only on x86-64), exports `bu_*` only (not `bt_*`, which
  jade_native exports), generator config. Tests: encode a small RGBA image to ETC1S, UASTC LDR and
  XUBC7, and a float image to UASTC HDR 4x4, then transcode each through jade_native and check
  dimensions and level count.
- Out of scope: a cooker or editor integration.
- Pitfalls: everything the encoder carries (zstd, tinyexr, QOI, TinyDDS, its transcoder copy) must
  stay hidden, and jade_tools must not link a second copy of any of them. `BU_COMP_FLAGS_THREADED`
  spawns threads. Fine on desktop, but tests should cover both modes.

#### Draft F: ufbx in jade_tools

- Depends on: Draft D. ADRs: 0005, 0006, 0015.
- Goal: ufbx v0.23.1 is linked into jade_tools and bound. A test loads committed FBX and OBJ files.
- Context: ufbx is one C file of 426 KiB. Its header has 56 `union` occurrences. The generator
  models unions (`StructModel.IsUnion`, `LibraryConfig.UnionMembers`). `ufbx_real` is `double`
  unless `UFBX_REAL_IS_FLOAT` is defined. jade_tools is desktop-only, so all its RIDs are 64-bit.
- Scope: package with an explicit `ufbx_real` choice recorded in `versions.json`, exports
  `ufbx_*`, generator config (likely a large `UnionMembers` and handle list), tests on small
  committed files under a permissive license: node, mesh, material and vertex counts.
- Out of scope: converting ufbx scenes into engine meshes.
- Pitfalls: anonymous unions inside structs. `ufbx_vec3`, for example, overlays `x, y, z` and
  `v[3]`.
  Loading from memory or callbacks (`ufbx_load_memory`, stream callbacks) avoids path encoding
  issues. The API surface is large, so bind it deliberately rather than by default.
