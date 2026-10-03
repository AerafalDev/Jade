# ADR-0015: Asset libraries: a runtime set in jade_native, an import set in jade_tools

- Status: Accepted (2026-10-03) for the runtime set only. `jade_tools` and the import set are
  deferred to the engine phase, where the asset pipeline and the editor get designed; until then
  ADR-0002 and ADR-0003 stay unchanged.
- Date: 2026-10-03
- Extends: ADR-0003 (a second combined library, desktop RIDs only) and ADR-0002 (one more package
  pair for it)

Amended on 2026-10-03 by ADR-0018: Jade is a 2D engine, so meshoptimizer leaves the runtime set
(task 305 binds zstd only), and cgltf and ufbx leave the deferred import set.

Amended on 2026-10-03 by ADR-0019: `jade_tools` is the tools library of ADR-0019, which also holds
ImGui. Its rule against linking `jade_native` is replaced by ADR-0019's: it reaches what `jade_native`
bundles through it, never through its own copy.

## Context

Task 304 surveys the C and C++ libraries an engine asset pipeline commonly uses and picks one per
need. The engine is not designed yet, so the picks rest on the assumptions below. Every fact was
checked on 2026-10-03 against the source listed in [Sources](#sources). Sizes come from the
[Measurements](#measurements) section.

Facts that shape the decision:

- WebGPU exposes block-compressed textures only as optional features: `texture compression BC`,
  `ETC2` and `ASTC` [S1]. Which ones an adapter has depends on the device, so no single compressed
  format works on every RID. WebGPU's ASTC formats are LDR only (`unorm` and `unorm srgb`). HDR
  textures need BC6H or an uncompressed fallback such as `RGB9 E5 ufloat` [S1].
- miniaudio 0.11.25, already in jade_native, decodes WAV, MP3 and FLAC out of the box [S2]. It also
  decodes Ogg Vorbis when stb_vorbis is compiled ahead of its implementation. Its `extras/` folder
  ships stb_vorbis v1.22, byte-identical to upstream stb [S2].
- `libjade_native.so` (linux-x64, release: Dawn, SDL3, miniaudio) holds 11.6 MiB of text and data
  today (M1).
- .NET 10, which Jade targets, has no Zstandard API. The 10.0.12 reference pack contains
  `BrotliStream` but no `ZstandardStream` (M3). `System.IO.Compression.ZstandardStream` is documented
  for .NET 11 and marked unsupported on `browser` [S20].

## Assumptions

- **A1.** Import (source asset to engine data) runs only on desktop, in an editor or a command-line
  cooker. It never runs on mobile or in the browser.
- **A2.** Import cooks textures into KTX2 files with a Basis Universal payload, one file for every
  RID. It cooks meshes into GPU-ready vertex and index buffers, already optimized and optionally
  meshopt-encoded. Bulk data is compressed with zstd. The engine phase designs the containers. This
  ADR picks codecs, not file formats.
- **A3.** At runtime, on every RID, the engine transcodes each texture to the best format the
  adapter supports, decodes meshopt buffers and decompresses zstd.
- **A4.** Audio is not re-encoded at import for now. Shipped audio stays WAV, FLAC, MP3 or Ogg
  Vorbis, and miniaudio decodes or streams it at runtime.
- **A5.** A shipped game may still load PNG or JPEG images that never went through the pipeline,
  such as user content, mods or quick prototypes. It does not load glTF or FBX at runtime.
- **A6.** Managed code handles what it can handle cheaply: container parsing (the KTX2 header,
  archives), packing, JSON. Native libraries are bound for codecs and geometry algorithms only.

If the engine phase drops an assumption, the affected rows move between the runtime and tooling
columns. Nothing here is built before the follow-up tasks.

## Decision

### Picks

| Need | Pick (version, license) | Built into | Rejected alternatives |
| --- | --- | --- | --- |
| Transcode GPU textures | basis_universal transcoder `v2_50` and its C API `bt_*` (Apache-2.0) | jade_native, every RID | libktx |
| Encode GPU textures | basis_universal encoder `v2_50` and its C API `bu_*` (Apache-2.0) | jade_tools | libktx, astc-encoder, bc7enc_rdo, Compressonator, ISPC Texture Compressor |
| General-purpose compression | zstd `v1.5.7`, full library (BSD-3-Clause, or GPL-2.0) | jade_native, every RID | lz4 |
| Decode images | stb_image `v2.30` (MIT or public domain) | jade_native, every RID | Wuffs, libspng with libjpeg-turbo |
| Optimize, encode and simplify meshes, generate tangents | meshoptimizer `v1.3` (MIT) | jade_native, every RID | MikkTSpace (tangents only) |
| Import glTF and GLB | cgltf `v1.15` (MIT) | jade_tools | fastgltf, assimp |
| Import FBX and OBJ | ufbx `v0.23.1` (MIT or public domain) | jade_tools | assimp |
| Decode compressed audio | miniaudio's WAV, FLAC and MP3 decoders, plus stb_vorbis v1.22 inside the miniaudio package (MIT or public domain) | jade_native, every RID | libopus with opusfile, libvorbis |

### Why these

**GPU textures.** No compressed format works on every adapter [S1], so textures ship once in a
universal form and are transcoded at load. basis_universal is the reference implementation of the
Basis payloads that KTX2 carries. Its v2.50 codecs also cover the cases where a platform-specific
format used to win: UASTC HDR 4x4 is built for fast transcoding to BC6H, XUBC7 is a lossless or
lossy supercompressed BC7, and XUASTC covers every ASTC block size [S3]. It has a plain C API, `bt_*`
for the transcoder and `bu_*` for the encoder, declared `extern "C"` in native builds, so neither
needs a shim [S4]. The transcoder depends on nothing but three zstd functions, which it takes from
our zstd package [S5].

libktx is rejected. Its stable release, v4.4.2 (2025-10-04), embeds basisu 1.16, which predates UASTC
HDR (first released in basisu 1.50.0, 2024-09-10). Its v5.0.0-rc2 (pre-release, 2026-08-17) embeds
basisu 2.10 [S6], [S7]. Both versions compile their own copy of zstd into the library [S6], which
collides with a zstd package in the same static link. A standalone basisu and libktx's embedded
basisu cannot share one binary either, since both define the same `basist::` symbols. What libktx
adds over basisu's own KTX2 support (KTX1, Vulkan and OpenGL upload, writing any vkFormat) is not
needed with WebGPU and managed KTX2 parsing (A6).

Dedicated encoders are rejected because basisu v2.50 already produces BC7 (XUBC7), ASTC LDR and HDR,
and UASTC HDR for BC6H [S3]. astc-encoder 5.7.0 (Apache-2.0, maintained) would duplicate ASTC.
bc7enc_rdo does not compile with clang 23 without a fix (a missing `<cstdint>`, M2), and basisu
carries its own BC7 encoders. Compressonator's last push was on 2024-06-19, and ISPC Texture
Compressor has been archived since 2024-09-23 [S8].

The transcoder is built without the PVRTC1, PVRTC2, ATC and FXT1 targets (`BASISD_SUPPORT_*=0`
[S5]), which WebGPU cannot sample. That saves 216 KiB (M2).

**Compression.** zstd is needed as soon as basisu is in: UASTC supercompression, XUASTC's Zstd and
hybrid profiles, and XUBC7 all use it [S3], [S4]. On zstd's own benchmark, `zstd -1` compresses the
Silesia corpus 2.887:1 and decompresses at 1580 MB/s, against 2.101:1 and 4000 MB/s for lz4 [S9]. For
asset files, a smaller file tends to save more load time than faster decoding beyond 1.5 GB/s per
core, so lz4 would be a second codec for a gain nobody has measured a need for. The full library,
compression included, goes into jade_native, so tooling and games (save files, caches) share one
copy. A decompression-only build would save about 440 KiB (M2). That option is left to the
browser-wasm work.

**Images.** stb_image decodes JPEG, PNG, TGA, BMP, PSD, GIF, HDR, PIC and PNM from one C file of
114 KiB (M2), with a plain C API. It serves both roles: shipped games (A5) and import, which calls it
through jade_native. Its README warns that security bugs are discussed in public and can take time
to be fixed [S10]. That risk only applies to A5's untrusted content. If the engine needs hardened
decoding, Wuffs is the upgrade path.

Wuffs is memory-safe by construction, but its latest stable version (v0.3, April 2023) has no JPEG
decoder [S11]. JPEG only exists in the v0.4 alphas, whose interface declares 469 `static inline`
functions that P/Invoke cannot call (M2). PNG and JPEG alone take 285 KiB (M2). libspng (PNG only,
needs zlib or miniz, last release v0.7.4 on 2023-05-08) plus libjpeg-turbo would be two libraries
where stb_image is one [S12]. libjpeg-turbo takes 618 KiB without SIMD (M2), its x86 SIMD build needs
NASM or Yasm, and its libjpeg API reports fatal errors through `exit()` or `longjmp()`, which managed
code cannot provide [S13].

**Meshes.** meshoptimizer covers vertex cache, overdraw and vertex fetch optimization,
simplification, meshlets, and the vertex and index codecs behind `EXT_meshopt_compression`, all
through a C API [S14]. Version 1.3 made `meshopt_generateTangents` stable, with a
`meshopt_TangentCompatible` mode that reproduces MikkTSpace [S14]. MikkTSpace (zlib license, last
commit 2020-03-25) is therefore not bound [S23]. The whole library is 238 KiB (M2), so it sits in
jade_native. The runtime decodes meshopt buffers and may simplify procedural meshes or build their
meshlets. Import uses the rest of the library through the same copy.

**Models.** cgltf (C, 97 KiB) reads glTF 2.0 and GLB, including `EXT_meshopt_compression` and
`KHR_texture_basisu` [S15]. ufbx (C, 426 KiB) reads binary and ASCII FBX from version 3000 on, as well
as Wavefront OBJ. It is fuzzed, and its CI covers Windows, macOS, Linux and WASI [S16]. Neither needs
a shim. fastgltf focuses on parsing speed with SIMD, but it is C++17 only, so it would need a shim
[S17]. Import speed matters less than shim cost in tooling. assimp has a C API (`cimport.h`) [S19],
but it weighs 10 MiB with its default importers and exporters (M2), and cgltf plus ufbx already
cover glTF, FBX and OBJ, the formats this survey targets. Draco (`KHR_draco_mesh_compression`) has no
general C API, only Unity and Maya plugin entry points. It weighs 1.2 MiB (M2), and its latest
release, 1.5.7, dates from 2024-01-17 [S18]. It is deferred.

**Audio.** miniaudio's built-in decoders already cover WAV, FLAC and MP3 [S2]. Ogg Vorbis comes
from compiling miniaudio's bundled stb_vorbis ahead of miniaudio's implementation. That costs 67 KiB
(M2) and no new binding, since Vorbis is reached through `ma_decoder` and `ma_sound` [S2].
Limitation: `ma_decoder_get_length_in_pcm_frames` always returns 0 for Vorbis [S2]. Opus would need
libopus (490 KiB, M2), opusfile and libogg, plus miniaudio's `extras/decoders/libopus` backend [S2].
It is deferred until the engine wants Opus.

### Deferred

None of these is bound until an engine feature needs it.

| Need | Candidate | When |
| --- | --- | --- |
| Opus audio | libopus 1.6.1, opusfile 0.12 and libogg 1.3.6, through miniaudio's libopus backend | voice chat or Opus music |
| Image resizing | stb_image_resize2 (121 KiB) | CPU mipmaps outside basisu, whose encoder already generates mipmaps [S4] |
| Image writing | stb_image_write (34 KiB) | the engine wants PNG output (screenshots, editor thumbnails) |
| OpenEXR import | tinyexr 3.2.0 or OpenEXR 3.5.1 | HDR environment maps. basisu's encoder already defines tinyexr's global symbols (M2), so a second copy cannot go into jade_tools as is |
| Lightmap UVs | xatlas (C API in `xatlas_c.h`, last commit 2022-07-25) [S22] | baked lighting |
| Draco-compressed glTF | Draco 1.5.7 behind a C shim | users need to import such files |
| Offline font atlases | msdf-atlas-gen v1.4 (C++, dependencies through vcpkg by default [S21]) behind a shim, in jade_tools | the engine pre-bakes atlases. Runtime glyph generation needs only msdfgen (task 303), and glyph packing is managed code |

### Runtime and tooling libraries

- **jade_native** gains the runtime picks on every RID: zstd, the basisu transcoder, stb_image,
  meshoptimizer, and stb_vorbis inside the miniaudio package. Together they add about 2.1 MiB of text
  and data on linux-x64 (M2), 18% over today's 11.6 MiB.
- **jade_tools** is a second combined library for the import picks: the basisu encoder, cgltf and
  ufbx, about 5.2 MiB (M2). It follows ADR-0003 in every respect (static upstreams, whole-archive
  linking, explicit exports, hidden internals, static C++ runtime) but is built only for the desktop
  RIDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64` and `osx-arm64`.
- **No overlapping exports.** jade_tools never exports a symbol that jade_native exports. The editor
  loads both libraries, and tooling code calls zstd, stb_image and meshoptimizer through jade_native.
  When a tooling upstream needs one of them internally, jade_tools keeps a private copy with hidden
  visibility, so its calls bind inside jade_tools. The basisu encoder library already contains its
  own zstd, tinyexr, QOI and TinyDDS code as global symbols (M2), so jade_tools must not link a second
  copy of any of them.
- **No link between the two.** jade_tools does not link against jade_native. Each library loads on
  its own, as ADR-0003 intends.
- **Separate bindings and packages.** jade_tools bindings go to their own assembly, whose imports
  name `jade_tools`. It ships in its own package pair (for example `Jade.Tools`, depending on
  `Jade.Tools.Native`) that only the editor and the cooker reference. The exact names and layout
  extend ADR-0002 and are settled by the jade_tools task.

Rejected layouts:

- Tooling picks in jade_native on desktop only. Shipped desktop games would carry about 5.2 MiB (45%
  more) of code they never call. Tooling functions in `Jade.Interop` would compile for every RID and
  fail with `EntryPointNotFoundException` on mobile and the web. A separate assembly makes that
  boundary visible at compile time.
- Everything in jade_native on every RID. Browser and mobile downloads would carry encoders that A1
  never runs there.
- jade_tools linked against jade_native to share zstd. It saves one private copy but adds a
  load-order dependency between two native libraries on every desktop OS.

## Consequences

- New pinned packages under `native/packages/`: zstd, basis_universal (built as the transcoder for
  jade_native and as the encoder for jade_tools), stb (stb_image), meshoptimizer, cgltf and ufbx. The
  miniaudio package compiles stb_vorbis too. Its export pattern `ma_*` already keeps `stb_vorbis_*`
  hidden.
- `THIRD-PARTY-NOTICES.md` gains their licenses. basis_universal is Apache-2.0 and ships a `NOTICE`
  file, which must be reproduced.
- basisu's C API passes pointers as `uint64_t` offsets (it targets WASM64 too) and booleans as
  `uint32_t` [S4]. The bindings keep those widths (ADR-0012). In native builds the offsets are plain
  pointer casts. `bt_ktx2_open` keeps the pointer to the file data until `bt_ktx2_close` [S5], so it
  gets no span overload. `bt_get_version` prints to stdout [S4].
- The binding generator hard-codes `jade_native` in its imports
  (`scripts/generate-bindings/CSharpEmitter.cs`). jade_tools needs the library name to come from the
  library's config.
- CI builds jade_tools only on the six desktop jobs.
- jade_native grows by about 2.1 MiB per RID, which counts toward the 250 MB nuget.org limit that
  task 106's size guard enforces.
- Untrusted images (A5) are decoded by stb_image, which is not hardened [S10].
- miniaudio cannot report the length of a Vorbis stream [S2].

## Measurements

All run on 2026-10-03 on the local CachyOS machine. Reproduce them from shallow clones of each
upstream at the tag listed in [Upstream snapshot](#upstream-snapshot-2026-10-03).

**M1.** `size artifacts/native/linux-x64/lib/libjade_native.so` (release build staged in the main
checkout on 2026-10-02): text 11,921,369 and data 216,808 bytes, so 11.6 MiB.

**M2.** Size of each candidate on linux-x64, built with clang 23.1.1, `-O2 -fPIC`, as static objects
or archives. The number is text plus data from `size`. bss is left out because it costs memory, not
file size. It approximates what each library adds to a shared library that exports its whole API.
Single-file and small libraries were compiled directly. CMake projects were built as static Release
libraries with their tests, tools and examples off. jade_native's release builds use `-O3`, so these
figures compare libraries with each other and do not predict exact deltas (xmake's `fastest` maps
to `-O3` for clang and gcc, `/usr/share/xmake/modules/core/tools/clang.lua`).

| Library | Configuration | KiB |
| --- | --- | --- |
| stb_image v2.30 | every format | 114 |
| stb_image v2.30 | `STBI_ONLY_PNG`, `STBI_ONLY_JPEG` | 68 |
| stb_image_resize2 | default | 121 |
| stb_image_write | default | 34 |
| stb_vorbis v1.22 | alone | 59 |
| miniaudio 0.11.25 | default, as bundled today | 649 |
| miniaudio 0.11.25 | with stb_vorbis compiled in | 716 |
| Wuffs v0.4.0-alpha.10 | every module | 550 |
| Wuffs v0.4.0-alpha.10 | PNG and JPEG modules only | 285 |
| libspng v0.7.4 | `spng.c`, without zlib | 53 |
| libjpeg-turbo 3.2.0 | `libjpeg.a`, `WITH_SIMD=0` (no NASM on the host) | 618 |
| basis_universal v2_50 | transcoder and `bt_*` API, default formats | 1,406 |
| basis_universal v2_50 | transcoder and `bt_*` API, without PVRTC1, PVRTC2, ATC and FXT1 | 1,190 |
| basis_universal v2_50 | `libbasisu_encoder.a` (contains its own transcoder and zstd) | 4,838 |
| libktx v4.4.2 | `libktx_read.a` | 1,868 |
| libktx v4.4.2 | `libktx.a` plus its `libastcenc-avx2-static.a` | 2,828 |
| astc-encoder 5.7.0 | SSE4.1 static library | 190 |
| bc7enc_rdo master (`b9438627`) | compiled with `-include cstdint` | 455 |
| zstd v1.5.7 | full library (common, compress, decompress) | 585 |
| zstd v1.5.7 | decompression only | 146 |
| lz4 v1.10.0 | `lz4`, `lz4hc`, `lz4frame`, `xxhash` | 161 |
| lz4 v1.10.0 | block API only | 75 |
| meshoptimizer v1.3 | every source file | 238 |
| MikkTSpace (`3e895b49`) | `mikktspace.c` | 22 |
| cgltf v1.15 | reader | 97 |
| cgltf v1.15 | writer | 38 |
| ufbx v0.23.1 | default | 426 |
| fastgltf v0.9.1 | with simdjson | 599 |
| assimp v6.0.5 | default importers and exporters, bundled zlib | 10,062 |
| Draco 1.5.7 | `libdraco.a` | 1,263 |
| libopus 1.6.1 | default | 490 |
| xatlas (`f700c779`) | `xatlas.cpp` | 209 |

The runtime picks add up to 585 + 1,190 + 114 + 238 + 67 = 2,194 KiB. The tooling picks add up to
4,838 + 97 + 426 = 5,361 KiB. The Wuffs count of 469 comes from counting lines that start with
`static inline` in `release/c/wuffs-v0.4.c` before its implementation section (line 34868, "Status
Codes Implementations"), so all 469 are in the interface part. The basisu encoder's global symbols
come from `nm --defined-only libbasisu_encoder.a`: 273 `ZSTD_*` functions, 22 `TinyDDS_*`, 4 `qoi_*`
and tinyexr's `LoadEXR`, `SaveEXR`, `IsEXR` and related functions.

**M3.** `rg -c -a "ZstandardStream|BrotliStream"` over
`~/.dotnet/packs/Microsoft.NETCore.App.Ref/10.0.12/ref/net10.0/System.IO.Compression*.dll` finds only
`System.IO.Compression.Brotli.dll`.

**M4.** Cross-compilation smoke test with zig 0.16.0 (`zig cc -std=c11` and `zig c++ -std=c++17
-fno-exceptions`, `-O2`) for `wasm32-wasi`, `aarch64-linux-gnu`, `aarch64-macos` and
`x86_64-windows-gnu`. stb_image, the basisu transcoder with its C API, zstd (every file in `common`,
`compress` and `decompress`, with `ZSTD_DISABLE_ASM`) and meshoptimizer compile for all four.
miniaudio with stb_vorbis compiles for `aarch64-linux-gnu` and `x86_64-windows-gnu`. It fails on
`aarch64-macos` only because this Linux host has no Apple SDK (`CoreAudio/CoreAudio.h` not found),
and `wasm32-wasi` was not attempted. This only compiles: nothing is linked or run, `wasm32-wasi` is
not Emscripten, and neither the Android NDK nor the iOS SDK was used. Tasks 104 and 105 validate the
real targets.

## Upstream snapshot (2026-10-03)

Latest release, or latest tag when the project publishes no GitHub release. "Last push" is the
repository's `pushed_at` from the GitHub API. Licenses were read from the license files, because
GitHub reports `NOASSERTION` for many of these repositories.

| Library | Latest (date) | License | Last push | C API | wasm and mobile evidence | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| [stb](https://github.com/nothings/stb) (stb_image v2.30, stb_vorbis v1.22) | no tags, master `2c980bb5` (2026-08-02) | MIT or public domain | 2026-08-02 | yes | plain C; M4 | runtime |
| [libspng](https://github.com/randy408/libspng) | v0.7.4 (2023-05-08) | BSD-2-Clause | 2026-08-31 | yes | none checked | rejected |
| [libjpeg-turbo](https://github.com/libjpeg-turbo/libjpeg-turbo) | 3.2.0 (2026-06-30) | IJG and BSD-3-Clause | 2026-09-25 | yes, `longjmp` errors | Android and iOS build sections in `BUILDING.md` | rejected |
| [Wuffs](https://github.com/google/wuffs) | v0.3.5 (2026-07-11) stable; v0.4.0-alpha.10 (2026-06-23) | Apache-2.0 or MIT | 2026-09-29 | yes, many `static inline` | plain C | rejected for now |
| [KTX-Software](https://github.com/KhronosGroup/KTX-Software) | v4.4.2 (2025-10-04); v5.0.0-rc2 (2026-08-17) | Apache-2.0, plus bundled licenses | 2026-10-02 | yes | `android.yml`, `web.yml` (Emscripten), iOS in `macos.yml` | rejected |
| [basis_universal](https://github.com/BinomialLLC/basis_universal) | v2_50 (2026-08-03) | Apache-2.0 | 2026-09-01 | yes since v2.0 (`bt_*`, `bu_*`) | README: native, WASM and WASI; M4 | runtime and tooling |
| [astc-encoder](https://github.com/ARM-software/astc-encoder) | 5.7.0 (2026-07-31) | Apache-2.0 | 2026-09-08 | not needed | NEON and SVE build options | rejected |
| [bc7enc_rdo](https://github.com/richgel999/bc7enc_rdo) | no tags, master `b9438627` (2026-07-31) | MIT or public domain | 2026-07-31 | C++ | none | rejected |
| [Compressonator](https://github.com/GPUOpen-Tools/compressonator) | V4.5.52 (2024-01-31) | MIT | 2024-06-19 | not checked | not checked | rejected (inactive) |
| [ISPC Texture Compressor](https://github.com/GameTechDev/ISPCTextureCompressor) | none | MIT | archived 2024-09-23 | not checked | not checked | rejected (archived) |
| [cgltf](https://github.com/jkuhlmann/cgltf) | v1.15 (2025-02-09) | MIT | 2026-02-02 | yes | plain C | tooling |
| [ufbx](https://github.com/ufbx/ufbx) | v0.23.1 (2026-09-25) | MIT or public domain | 2026-10-01 | yes | CI on WASI | tooling |
| [fastgltf](https://github.com/spnda/fastgltf) | v0.9.1 (2026-09-27) | MIT | 2026-10-02 | no, C++17 | not checked | rejected |
| [assimp](https://github.com/assimp/assimp) | v6.0.5 (2026-04-30) | BSD-3-Clause | 2026-10-02 | yes (`cimport.h`) | not checked | rejected |
| [meshoptimizer](https://github.com/zeux/meshoptimizer) | v1.3 (2026-09-25) | MIT | 2026-09-28 | yes | CI builds its wasm decoder; M4 | runtime |
| [MikkTSpace](https://github.com/mmikk/MikkTSpace) | none, last commit 2020-03-25 | zlib-style | 2024-07-20 | yes | plain C | rejected (covered) |
| [zstd](https://github.com/facebook/zstd) | v1.5.7 (2025-02-19) | BSD-3-Clause or GPL-2.0 | 2026-10-01 | yes | `android-ndk-build.yml`; M4 | runtime |
| [lz4](https://github.com/lz4/lz4) | v1.10.0 (2024-07-22) | BSD-2-Clause (`lib/`) | 2026-07-01 | yes | not checked | rejected |
| [Opus](https://github.com/xiph/opus) | v1.6.1 tag (2026-01-13) | BSD-3-Clause | 2026-09-11 | yes | Android arm64 job in `cmake.yml` | deferred |
| [opusfile](https://github.com/xiph/opusfile) | v0.12 (2020-06-27) | BSD-3-Clause | 2026-03-29 | yes | not checked | deferred |
| [libogg](https://github.com/xiph/ogg) / [libvorbis](https://github.com/xiph/vorbis) | v1.3.6 (2025-06-16) / v1.3.7 (2020-07-04) | BSD-3-Clause | 2026-03-03 / 2026-08-04 | yes | not checked | rejected (stb_vorbis covers it) |
| [miniaudio](https://github.com/mackron/miniaudio) | 0.11.25 (2026-03-03) | Unlicense or MIT-0 | 2026-08-19 | yes | already bundled | runtime, extended |
| [xatlas](https://github.com/jpcy/xatlas) | none, last commit 2022-07-25 | MIT | 2024-06-16 | yes (`xatlas_c.h`) | not checked | deferred |
| [tinyexr](https://github.com/syoyo/tinyexr) | v3.2.0 (2026-07-08) | BSD-3-Clause | 2026-09-07 | yes (`extern "C"` in `tinyexr.h`) | not checked | deferred |
| [OpenEXR](https://github.com/AcademySoftwareFoundation/openexr) | v3.5.1 (2026-09-26) | BSD-3-Clause | 2026-10-02 | yes (OpenEXRCore) | not checked | deferred |
| [Draco](https://github.com/google/draco) | 1.5.7 (2024-01-17) | Apache-2.0 | 2026-09-24 | plugin entry points only | not checked | deferred |
| [msdf-atlas-gen](https://github.com/Chlumsky/msdf-atlas-gen) | v1.4 (2026-03-05) | MIT | 2026-05-16 | no, C++ | not checked | deferred |

"Not checked" marks libraries rejected or deferred for other reasons. Their platform support was not
investigated.

## Sources

All read on 2026-10-03.

- **S1** Dawn `src/dawn/dawn.json` at tag `v20260930.214659`, the version jade_native pins (the staged
  copy has the same git blob, `f53d1a62`): features
  [L2516-L2520](https://github.com/google/dawn/blob/v20260930.214659/src/dawn/dawn.json#L2516-L2520),
  formats [L4423](https://github.com/google/dawn/blob/v20260930.214659/src/dawn/dawn.json#L4423) and
  [L4450-L4499](https://github.com/google/dawn/blob/v20260930.214659/src/dawn/dawn.json#L4450-L4499).
- **S2** miniaudio 0.11.25: built-in decoders,
  [miniaudio.h L2498-L2520](https://github.com/mackron/miniaudio/blob/0.11.25/miniaudio.h#L2498-L2520);
  stb_vorbis hook,
  [L65329-L65331](https://github.com/mackron/miniaudio/blob/0.11.25/miniaudio.h#L65329-L65331);
  Vorbis length,
  [L10080-L10081](https://github.com/mackron/miniaudio/blob/0.11.25/miniaudio.h#L10080-L10081);
  [extras/stb_vorbis.c](https://github.com/mackron/miniaudio/blob/0.11.25/extras/stb_vorbis.c)
  (compared with stb master `2c980bb5`);
  [extras/decoders/libopus](https://github.com/mackron/miniaudio/tree/0.11.25/extras/decoders/libopus).
- **S3** basis_universal v2_50
  [README.md L22-L45](https://github.com/BinomialLLC/basis_universal/blob/v2_50/README.md#L22-L45)
  (codecs, native and WASM builds, the transcoder's only dependency is zstd).
- **S4** basis_universal v2_50 C API:
  [encoder/basisu_wasm_api_common.h L4-L15](https://github.com/BinomialLLC/basis_universal/blob/v2_50/encoder/basisu_wasm_api_common.h#L4-L15)
  (`extern "C"` natively) and
  [L41-L42](https://github.com/BinomialLLC/basis_universal/blob/v2_50/encoder/basisu_wasm_api_common.h#L41-L42)
  (mipmap generation flags);
  [encoder/basisu_wasm_transcoder_api.h](https://github.com/BinomialLLC/basis_universal/blob/v2_50/encoder/basisu_wasm_transcoder_api.h);
  [encoder/basisu_wasm_api.h](https://github.com/BinomialLLC/basis_universal/blob/v2_50/encoder/basisu_wasm_api.h);
  [encoder/basisu_wasm_transcoder_api.cpp L27, L265-L282](https://github.com/BinomialLLC/basis_universal/blob/v2_50/encoder/basisu_wasm_transcoder_api.cpp#L265-L282)
  (`printf` in `bt_get_version`, `bt_ktx2_open`);
  [CMakeLists.txt L20](https://github.com/BinomialLLC/basis_universal/blob/v2_50/CMakeLists.txt#L20)
  (C++17) and
  [L365](https://github.com/BinomialLLC/basis_universal/blob/v2_50/CMakeLists.txt#L365) (native C API
  example).
- **S5** basis_universal v2_50 `transcoder/basisu_transcoder.cpp`: target switches
  [L95-L143](https://github.com/BinomialLLC/basis_universal/blob/v2_50/transcoder/basisu_transcoder.cpp#L95-L143),
  zstd functions used
  [L177](https://github.com/BinomialLLC/basis_universal/blob/v2_50/transcoder/basisu_transcoder.cpp#L177),
  `ktx2_transcoder::init` keeps the data pointer
  [L19813-L19814](https://github.com/BinomialLLC/basis_universal/blob/v2_50/transcoder/basisu_transcoder.cpp#L19813-L19814).
- **S6** KTX-Software [releases](https://github.com/KhronosGroup/KTX-Software/releases); v4.4.2
  [CMakeLists.txt L392](https://github.com/KhronosGroup/KTX-Software/blob/v4.4.2/CMakeLists.txt#L392)
  (zstd compiled in) and
  [external/basisu/encoder/basisu_comp.h L22-L23](https://github.com/KhronosGroup/KTX-Software/blob/v4.4.2/external/basisu/encoder/basisu_comp.h#L22-L23)
  (basisu 1.16); v5.0.0-rc2
  [external/basis_universal/encoder/basisu_comp.h L25-L26](https://github.com/KhronosGroup/KTX-Software/blob/v5.0.0-rc2/external/basis_universal/encoder/basisu_comp.h#L25-L26)
  (basisu 2.10) and
  [lib/CMakeLists.txt L523](https://github.com/KhronosGroup/KTX-Software/blob/v5.0.0-rc2/lib/CMakeLists.txt#L523)
  (`BASISU_ZSTD TRUE`).
- **S7** basis_universal [release v1_50_0](https://github.com/BinomialLLC/basis_universal/releases/tag/v1_50_0)
  (2024-09-10, "the first release supporting UASTC HDR encoding/transcoding").
- **S8** GitHub API `repos/<owner>/<repo>` and `releases/latest` for
  [astc-encoder](https://github.com/ARM-software/astc-encoder/releases/tag/5.7.0),
  [Compressonator](https://github.com/GPUOpen-Tools/compressonator) and
  [ISPC Texture Compressor](https://github.com/GameTechDev/ISPCTextureCompressor) (`archived: true`).
- **S9** zstd v1.5.7
  [README.md L44-L51](https://github.com/facebook/zstd/blob/v1.5.7/README.md#L44-L51) (benchmark).
- **S10** stb [README.md L8](https://github.com/nothings/stb/blob/2c980bb59875b0d32144a71867fbdebb2f77cd20/README.md#L8).
- **S11** Wuffs v0.4.0-alpha.10
  [README.md L226](https://github.com/google/wuffs/blob/v0.4.0-alpha.10/README.md#L226) and
  [release/c/README.md L19](https://github.com/google/wuffs/blob/v0.4.0-alpha.10/release/c/README.md#L19).
- **S12** libspng v0.7.4 [README.md L27-L41](https://github.com/randy408/libspng/blob/v0.7.4/README.md#L27-L41).
- **S13** libjpeg-turbo 3.2.0
  [BUILDING.md L13-L17](https://github.com/libjpeg-turbo/libjpeg-turbo/blob/3.2.0/BUILDING.md#L13-L17)
  and [doc/libjpeg.txt L1659-L1663](https://github.com/libjpeg-turbo/libjpeg-turbo/blob/3.2.0/doc/libjpeg.txt#L1659-L1663).
- **S14** meshoptimizer v1.3
  [src/meshoptimizer.h L931-L955](https://github.com/zeux/meshoptimizer/blob/v1.3/src/meshoptimizer.h#L931-L955)
  and [release notes](https://github.com/zeux/meshoptimizer/releases/tag/v1.3) ("`meshopt_generateTangents`
  ... are now stable").
- **S15** cgltf v1.15 [README.md L89-L123](https://github.com/jkuhlmann/cgltf/blob/v1.15/README.md#L89-L123).
- **S16** ufbx v0.23.1 [README.md L40-L81](https://github.com/ufbx/ufbx/blob/v0.23.1/README.md#L40-L81).
- **S17** fastgltf v0.9.1 [README.md L8-L9](https://github.com/spnda/fastgltf/blob/v0.9.1/README.md#L8-L9)
  and [CMakeLists.txt L25-L27](https://github.com/spnda/fastgltf/blob/v0.9.1/CMakeLists.txt#L25-L27).
- **S18** Draco 1.5.7 [src/draco/unity/draco_unity_plugin.h](https://github.com/google/draco/blob/1.5.7/src/draco/unity/draco_unity_plugin.h)
  (the only general `extern "C"` header besides the Maya plugin).
- **S19** assimp v6.0.5 [include/assimp/cimport.h](https://github.com/assimp/assimp/blob/v6.0.5/include/assimp/cimport.h).
- **S20** Microsoft Learn, [ZstandardStream](https://learn.microsoft.com/dotnet/api/system.io.compression.zstandardstream?view=net-11.0)
  and [What's new in .NET libraries for .NET 11](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-11/libraries#compression-and-archive-formats).
- **S21** msdf-atlas-gen v1.4 [README.md L39](https://github.com/Chlumsky/msdf-atlas-gen/blob/v1.4/README.md#L39).
- **S22** xatlas [source/xatlas/xatlas_c.h](https://github.com/jpcy/xatlas/blob/f700c7790aaa030e794b52ba7791a05c085faf0c/source/xatlas/xatlas_c.h).
- **S23** MikkTSpace [mikktspace.h](https://github.com/mmikk/MikkTSpace/blob/3e895b49d05ea07e4c2133156cfa94369e19e409/mikktspace.h).
- **S24** meshoptimizer v1.3 [README.md](https://github.com/zeux/meshoptimizer/blob/v1.3/README.md)
  and [src/meshoptimizer.h L38](https://github.com/zeux/meshoptimizer/blob/v1.3/src/meshoptimizer.h#L38)
  (`extern "C"`).
