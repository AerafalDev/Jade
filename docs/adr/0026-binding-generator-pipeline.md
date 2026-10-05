# 0026. Binding generator pipeline and intermediate representation

- Status: Accepted
- Date: 2026-10-05

## Context

[0007](0007-in-house-binding-generator.md) chose an in-house generator that reads `dawn.json` and,
through ClangSharp, the SDL3 and miniaudio headers. It left the generator's intermediate
representation and the remaining mapping rules open. They depend on what the pinned inputs contain
and on what libclang can parse on one machine.

Verified on 2026-10-05 at the commits pinned in `build/versions.json`, with SDK
`11.0.100-rc.1.26425.128` and ClangSharp 21.1.8.4:

- `dawn.json` at Dawn `b1236a9` has 347 entries in ten categories (181 structures, 65 enums, 28
  objects, 18 natives, 14 callback functions, 14 callback infos, 12 constants, 6 bitmasks, 5
  functions, 4 function pointers). Its schema is described in `docs/dawn/codegen.md`, but the file
  differs in places: methods use `args`, not `arguments`, and some keys take two shapes
  (`extensible` is `false`, `"in"` or `"out"`; `default` and `length` are numbers or names; a
  method's `returns` is a type name or an object).
- Dawn generates two `webgpu.h` from it (`generator/dawn_json_generator.py`): its own header with
  the tags `dawn`, `native` and `deprecated`, and Emdawnwebgpu's header for the browser with the tag
  `emscripten`. Untagged items are in both; an item tagged only `art_experimental` (Dawn's Kotlin
  bindings) is in neither. Structure members are never tagged, so a structure has the same layout in
  both variants: only whole entries, methods and enum values differ. A tagged enum value gets a
  fixed offset from its own tags (`0x0005_0000` for `dawn`), whatever the variant. At the pinned
  commit, 345 entries are in Dawn's header and 218 in Emdawnwebgpu's.
- SDL3 and miniaudio declare different things on different platforms. Parsing their headers for
  each RID of [0012](0012-supported-targets.md): SDL3 has 14 declarations only on Android
  (`SDL_GetAndroidJNIEnv`, …), 4 declarations only on iOS (`SDL_SetiOSAnimationCallback`, …), 4
  only on Windows (`SDL_SetWindowsMessageHook`, `SDL_GetDXGIOutputInfo`, …) and 2 only on Linux
  (`SDL_SetLinuxThreadPriority`, …); miniaudio's pthread-based types exist everywhere but on
  Windows, and its `ma_context` has different members there. A parse for the host alone misses the
  API of every other platform.
- The `libclang` package that ClangSharp 21.1.8.4 depends on (21.1.8) selects a runtime package
  through `runtime.json`, for `linux-x64`, `linux-arm64`, `osx-arm64`, `win-x64` and `win-arm64`
  only. The runtime package contains the native library alone, not clang's builtin headers
  (`stddef.h`, `stdint.h`, …), so the system includes of the parsed headers must come from
  somewhere else.
- With `-nostdinc -ffreestanding` and 16 small headers in place of the C runtime (types taken from
  clang's predefined macros such as `__SIZE_TYPE__`, near-empty stubs for the rest), libclang 21.1.8
  parses SDL3 and miniaudio for the 12 target triples without a single diagnostic. clang defines the
  `TARGET_OS_*` macros itself for Apple triples.

## Decision

### Pipeline

1. **Inputs**: `build/versions.json`, one configuration per generated library
   (`interop/<project>/bindings.json`), and the pinned sources. Sources are fetched from GitHub at
   the pinned commit (single files from `raw.githubusercontent.com`, directories from the commit's
   archive) into `artifacts/binding-generator/sources/<dependency>/<commit>/`. A fetch is written
   aside and moved in place once complete. The commit hash in the URL pins the content.
2. **Front-ends**:
   - `dawn.json` is read into a typed model that rejects unknown members and categories, so a schema
     change in a Dawn update fails the generator instead of being ignored. Each item gets its
     variants by Dawn's tag rules.
   - C headers are parsed by libclang through ClangSharp, once per target triple: one triple per RID
     of [0012](0012-supported-targets.md), at the minimum OS versions of `build/versions.json`
     ([0024](0024-minimum-os-versions.md)).
3. **Intermediate representation**: one model per library, described below.
4. **Annotations**: the library configuration adds what the sources cannot express. An annotation
   that matches no declaration is an error, and so is a declaration that needs one and has none.
5. **Projection**: the mapping rules ([0009](0009-interop-mapping-conventions.md),
   [0027](0027-interop-mapping-rules.md)) turn the model into C# declarations for each layer
   ([0008](0008-two-layer-interop.md), [0028](0028-internal-raw-interop-layer.md),
   [0029](0029-descriptors-and-chained-structs.md)).
6. **Emitters**: raw layer, idiomatic layer and layout tests, deterministic as
   [0007](0007-in-house-binding-generator.md) requires.

### Intermediate representation

- A language-neutral model of a C API: constants, enums and bitmasks, structures and unions, opaque
  handles, function pointer types, functions and typedefs. Type references are builtin types by
  their C spelling, named declarations, pointers with their constness, fixed-size arrays and
  function pointers.
- It carries the semantics the generator needs beyond C types: array lengths, optionality, defaults,
  string kinds, ownership, chain roots and directions, callback infos. `dawn.json` provides most of
  them; for C headers they come from the configuration.
- Every declaration, member, method and enum value carries its availability: a set of platform
  families (Windows, Linux, macOS, iOS, Android, browser). For `dawn.json`, Dawn's header gives
  every family but the browser and Emdawnwebgpu's header gives the browser. For C headers, it is the
  set of families whose triples declare it.
- A declaration present on several targets must be identical on all of them: same members, same type
  spellings. A divergence is an error unless the configuration makes the declaration opaque or
  excludes it. Sizes and offsets computed during parsing are never used for emission: the C# layout
  follows from the member types, and the layout tests of
  [0009](0009-interop-mapping-conventions.md) check it against the real compilers.
- Typedef names are kept, so `size_t`, `int64_t` or `Uint32` are mapped by name and never through
  their canonical type, which varies between triples.
- Collections are ordered by C name (ordinal comparison), never by parse order.

### Parsing C headers

- libclang comes only from the ClangSharp package pinned in `Directory.Packages.props`. The
  generator checks the version of the loaded libclang and fails if it is not the package's.
- Arguments: `-x c -std=c17 -ffreestanding -nostdinc -target <triple>`, the generator's
  `scripts/binding-generator/include/` as the only system include directory, then the configured
  include directories and forced includes. No header of the host or of an installed compiler is
  read, so the result depends only on the pinned inputs.
- `scripts/binding-generator/include/` replaces the C runtime for parsing only. Types come from
  clang's predefined macros and are right for every triple; the other headers provide only what the
  parsed libraries use. A library update that includes a new system header fails with a
  missing-header error and gets a new stub there.
- Any clang warning or error fails the generator: it would mean the parse differs from what the
  native build compiles.

### Library configuration

A local decision, recorded here because the generator reads it:

- One `bindings.json` per generated interop project, next to its `Generated/` folder
  ([0023](0023-repository-layout-and-conventions.md)). It is strict JSON, like
  `build/versions.json`, read through System.Text.Json source generation; unknown members are
  errors.
- `dependency` is the key of the library in `build/versions.json`; `sources` lists the paths fetched
  at the pinned commit (a trailing `/` marks a directory); then either `dawnJson`, the path of
  `dawn.json`, or `clang` with `headers`, `includeDirectories` (relative to the sources) and
  `forcedIncludes` (relative to the repository root).
- Annotations are added by roadmap tasks 7 and 8 as entries keyed by C name. An entry that matches
  no declaration is an error, so the configuration cannot drift from the headers.

### miniaudio defines

A local decision: `build/miniaudio/config.h` holds every `MA_*` define. The native build and the
generator both force-include it, so the compiled library and the parsed headers agree by
construction, as [0006](0006-miniaudio-for-audio.md) requires.

## Consequences

- Platform-specific APIs are bound with their platform attributes instead of being missed, and
  platform-dependent layouts are detected at generation time, before any layout test runs.
- The output depends only on the pinned inputs: the same bytes on every machine (two runs of the
  skeleton give identical output).
- Each C library is parsed 12 times; with function bodies skipped, the whole skeleton runs in a few
  seconds once the sources are cached.
- The replacement headers are ours to maintain. Parse-time sizes of structures that embed stubbed
  types (the pthread types) are wrong on purpose; those structures are opaque
  ([0006](0006-miniaudio-for-audio.md)) and never emitted with members.
- The first run for a pinned commit needs network access; later runs use the cache.
- A Dawn update that changes the `dawn.json` schema fails the generator until the model follows it.
- The generator runs on hosts that have a libclang runtime package: not on macOS x64.
- CI does not build or run the generator yet; the regeneration check comes with roadmap task 7.

## Alternatives considered

- **Parse for the host only, with the system headers**: misses the platform-specific APIs listed
  above, and the output depends on the machine's C runtime and libclang.
- **Real sysroots for every target** (Windows SDK, Xcode SDK, Android NDK, Emscripten): exact, but
  they cannot all be installed on one machine, some cannot be redistributed, and regenerating would
  need several hosts.
- **Parse Dawn's generated `webgpu.h` with clang instead of `dawn.json`**: one front-end fewer, but
  it loses lengths, optionality, defaults and chain roots, which the idiomatic layer needs.
- **No intermediate representation, one generator per input format**: duplicates the mapping rules
  and the emitters.
- **clang's builtin headers from an installed clang**: ties the output to whatever compiler the
  machine has.
- **Configuration in C# or YAML**: C# would put generator code next to the interop projects'
  sources; YAML needs a package and a second format besides `build/versions.json`.
