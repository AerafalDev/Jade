# 0027. Remaining interop mapping rules

- Status: Accepted
- Date: 2026-10-05

## Context

[0009](0009-interop-mapping-conventions.md) lists the first mapping rules and leaves the others to
the generator's design. [0026](0026-binding-generator-pipeline.md) defines the intermediate
representation the rules apply to. This record completes 0009 and does not replace it.

Verified on 2026-10-05 in the sources pinned in `build/versions.json`:

- `dawn.json` names are space-separated words, with acronyms in upper case (`RGBA8 unorm`,
  `shader source WGSL`, `surface source windows HWND`). Only `texture dimension` and
  `texture view dimension` have values that start with a digit (`1D`, `2D`, `2D array`, `3D`).
  Dawn's `api.h` template declares `WGPUFlags` as `uint64_t` and `WGPUBool` as `uint32_t`.
- SDL3 functions are `SDL_` followed by PascalCase. Its flags are integer typedefs whose values are
  macros (`SDL_WindowFlags` is a `Uint64` typedef, and `SDL_WINDOW_FULLSCREEN` is defined with
  `SDL_UINT64_C`). It declares printf-style variadic functions (`SDL_Log`, `SDL_SetError` and
  others) and `SDL_FORCE_INLINE` functions.
- miniaudio names are snake_case with an `ma_` prefix; `ma_bool8` and `ma_bool32` are `ma_uint8`
  and `ma_uint32`; `ma_result` is an enum of `MA_*` codes.
- Neither SDL3 nor miniaudio's header section declares a bit-field (checked in the clang AST of the
  parsed headers).
- With `GenerateDocumentationFile` and warnings as errors, CS1591 requires an XML comment on every
  public member.

## Decision

### Names

- Public names drop the library prefix (`WGPU`/`wgpu`, `SDL_`, `ma_`/`MA_`) and follow .NET casing:
  every `dawn.json` word and every snake_case part becomes a PascalCase word. Two-letter acronyms
  stay in upper case (`IO`); longer ones are Pascal-cased (`Wgsl`, `Rgba8Unorm`, `Hwnd`).
  Exceptions are listed in the library configuration.
- Enum members drop the prefix they share with their enum (`MA_SUCCESS` of `ma_result` becomes
  `Result.Success`).
- An identifier that would start with a digit takes the last word of its type's name as a prefix:
  `TextureDimension.Dimension2D`, `TextureViewDimension.Dimension2DArray`.
- Internal raw declarations keep their C names ([0028](0028-internal-raw-interop-layer.md)).
- Each interop assembly has one namespace: `Jade.Wgpu`, `Jade.Sdl`, `Jade.MiniAudio`.

### Types

- Fixed-width and pointer-sized typedefs are mapped by name, never through their canonical type:
  `int8_t` to `uint64_t` and the libraries' aliases (`Sint8` to `Uint64`, `ma_int8` to `ma_uint64`)
  become `sbyte` to `ulong`; `intptr_t` and `ptrdiff_t` become `nint`; `uintptr_t` and `size_t`
  become `nuint` ([0009](0009-interop-mapping-conventions.md)).
- Integers used as booleans (`WGPUBool`, `ma_bool32`, `ma_bool8`) map to dedicated blittable
  boolean structs of the same size. Only C `bool` (`_Bool`) maps to `bool`.
- `char` maps to `byte`. A NUL-terminated `const char*` is a `byte*` in the raw layer. The idiomatic
  layer accepts UTF-8 spans and strings ([0016](0016-public-api-conventions.md)): a span that ends
  with a NUL byte is passed pinned, anything else is copied with a terminator into the call's arena
  ([0029](0029-descriptors-and-chained-structs.md)). A returned string is a span over the library's
  memory, with its ownership taken from the configuration.
- WebGPU enums have `uint` as underlying type and WebGPU bitmasks are `[Flags]` enums over `ulong`,
  as in `webgpu.h`. A C enum from a header gets `int` when every value fits, `uint` otherwise; an
  enum whose size differs between targets is an error.
- Flags whose values are macros become a `[Flags]` enum over the macros' typedef (`SDL_WindowFlags`
  and `SDL_WINDOW_*` become `WindowFlags : ulong`); the configuration maps macro prefixes to types.
  Other object-like macros become constants only when the configuration selects them. Macro values
  are evaluated by clang, never parsed from tokens by the generator.
- An anonymous nested structure or union is named after its parent and its member:
  `{Parent}{Member}`.
- A function pointer type is a `delegate* unmanaged[Cdecl]<…>` in the raw layer; its typedef name
  is not kept, since C# has no public type alias. The idiomatic layer uses the trampolines of
  [0009](0009-interop-mapping-conventions.md).
- A structure only ever used through pointers, and never defined, maps to a handle
  ([0009](0009-interop-mapping-conventions.md)); its release function comes from the configuration.

### Constructs that are not bound

- Variadic functions and functions that take a `va_list`: `LibraryImport` cannot call them portably
  (Apple arm64 passes variadic arguments differently from fixed ones). Hand-written idiomatic code
  can offer alternatives through non-variadic functions.
- `static inline` functions and function-like macros: the library exports no symbol for them.
  Hand-written partials reimplement the useful ones.
- `wchar_t` (2 bytes on Windows, 4 elsewhere) and `long double`: declarations that use them are
  skipped unless the configuration maps them.
- Bit-fields: an error unless the structure is opaque.
- The generator lists every skipped declaration in its output, so that omissions are reviewed.

### Platform availability

- A declaration that is not available on every platform family gets `[SupportedOSPlatform]` for
  each family that has it, or `[UnsupportedOSPlatform("browser")]` when only the browser lacks it
  ([0016](0016-public-api-conventions.md)). A browser-only declaration of the WebGPU API gets
  `[SupportedOSPlatform("browser")]`.

### Documentation

- Every public generated member has an XML comment: the upstream comment converted when there is one
  (SDL3 and miniaudio Doxygen comments), otherwise a summary that names the C declaration it maps.
  Internal raw declarations have a summary naming their C declaration.

### Generated files

- A generated file holds one C# type. A type whose members come from several C headers, such as the
  raw function class of SDL3, is split into one partial file per header.

## Consequences

- The public API reads like .NET while every member can still be traced to its C declaration.
- Platform analyzers (CA1416) flag calls to platform-specific APIs that are not guarded.
- Variadic logging and inline helpers need hand-written code, a small and visible set.
- Name exceptions and macro groupings live in the configuration, where a reviewer sees them next to
  the generated code.

## Alternatives considered

- **Keep C names in the public API**: no naming rules to maintain, but the API would not read like
  .NET, against [0016](0016-public-api-conventions.md).
- **Map `long`-sized and fixed-width typedefs through their canonical types**: the generated code
  would differ per target, since `int64_t` is `long` on LP64 targets and `long long` elsewhere.
- **Call variadic functions through fixed prototypes**: works on x64 and Windows arm64, but breaks
  on Apple arm64.
