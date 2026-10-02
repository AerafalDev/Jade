# 201: Binding generator core

- Depends on: 101
- ADRs: 0005, 0006, 0008, 0010

## Goal

`dotnet scripts/generate-bindings.cs` reads staged C headers through libclang into
the intermediate model and emits deterministic C# following ADR-0006. It is proven on a vertical
slice of SDL3. CI fails when regeneration produces a diff.

## Context

- Inputs are staged by the native build in `artifacts/native/<host-rid>/include/<lib>/` (101).
  `metadata/versions.json` records each upstream's version and build defines (miniaudio's among
  them). The headers in `include/<lib>/` are exactly the public ones to bind.
- `scripts/build-native.cs` and `scripts/smoke-native.cs` (101) are the reference for script layout
  under ADR-0010 and for source-generated JSON.
- ClangSharp `21.1.8.4` on nuget.org (2026-10-02). It needs the libclang and libClangSharp native
  runtimes; find which packages provide them for the dev RIDs (Linux, Windows, macOS).
- `dawn.json` support comes in 202. Design the model so a second reader plugs in without changing
  the emitter.
- From 001: Jade.Interop generates XML docs and CS1591 is an error, so every generated public member
  needs `///` docs. Rule: use the upstream doc when there is one. Otherwise emit a short summary
  naming the native symbol (for example `<c>SDL_CreateWindow</c>`), which also maps .NET names back
  to the C docs. Do not suppress CS1591 for generated files.
- From 001: the generator needs ClangSharp through `#:package`, which makes it the first script to
  settle how `#:package` combines with Central Package Management (ADR-0008), unless 101 already did.
  Record the outcome. Scripts also run AOT and trim analyzers by default (see CLAUDE.md).

## Scope

- Entry point `scripts/generate-bindings.cs`, helpers in `scripts/generate-bindings/`, one type per
  file (ADR-0010). Suggested split: model, clang reader, emitter, naming, per-library config.
- Per-library config format (C# or JSON; choose and justify) covering:
  - prefix stripping and naming overrides;
  - handle types;
  - pointer+count pairs and out-parameter annotations;
  - exclusions;
  - layout-variance decisions;
  - target triples to parse;
  - platform availability.
- Multi-triple parsing: parse each library for every target triple of the ADR-0007 RIDs that
  libclang can handle without the full SDK (document the ones that cannot). Diff type sizes and
  layouts across triples, and fail with a report on unhandled variance.
- Emitter covering ADR-0006 for the slice:
  - raw 1:1 functions;
  - enums and `[Flags]`;
  - structs with explicit layout checks;
  - handles with instance methods;
  - `in`/`ref`/`out`/`Span` and UTF-8 overloads;
  - `delegate* unmanaged` callbacks;
  - XML docs from header comments.
- Import style: blittable signatures with `DisableRuntimeMarshalling`. Choose `[LibraryImport]` vs
  `[DllImport]`, measuring build time on the slice, and record the choice. Both must name
  `jade_native`.
- SDL3 vertical slice: `SDL_init.h`, `SDL_version.h`, `SDL_error.h`, `SDL_video.h` (window functions
  only) and `SDL_events.h` (enough to poll a quit event). Output goes to
  `src/Jade.Interop/Generated/Sdl3/`.
- Determinism: stable ordering, no timestamps, LF endings, and one header line pointing at the
  generator.
- CI: add a job to `ci.yml` that regenerates and runs `git diff --exit-code`. If the native build is
  too heavy for that job, get the staged inputs from the native workflow (103) and document how.
- Update **Commands** in `CLAUDE.md`.

## Out of scope

- Full SDL3 (203), WebGPU (202), miniaudio (204).

## Acceptance criteria

- [x] Running the generator twice gives byte-identical output.
- [x] The solution builds with the generated slice, with 0 warnings.
- [x] A test in `tests/Jade.Interop.Tests` calls the generated `SDL_GetVersion` and one window-less
      function (`SDL_Init` with no subsystems or video, then `SDL_Quit`) against the 101 library and
      passes. Video needs a display, so document how it behaves headless.
- [x] The layout-variance report runs and is clean for the slice, or lists what the config handles.

## Verification

Generator run (twice, compare hashes), build, test run, and a short excerpt of generated code for
one function, one enum, one struct and one handle method.

## Pitfalls

- C `long` is 32-bit on Windows and 64-bit on Linux and macOS.
- `const char*` parameters expect NUL-terminated strings. A `ReadOnlySpan<byte>` overload must
  guarantee the terminator without a GC allocation.
- SDL macros (`SDL_DECLSPEC`, `SDLCALL`) and inline functions: decide what is bindable; inline
  functions are not exported.

## Outcome

- Summary: `dotnet scripts/generate-bindings.cs` (entry point, 41 one-type files in
  `scripts/generate-bindings/`, stub headers in `scripts/generate-bindings/sysroot/`) parses the staged SDL3
  headers through ClangSharp for the 12 ADR-0007 triples. It checks that every triple yields the same
  declarations and that every generated struct has clang's layout on every triple, then writes
  `src/Jade.Interop/Generated/Sdl3/` (24 files: 93 functions in 5 groups, 8 enums, 10 structs of which 1 opaque,
  1 handle) and `tests/Jade.Interop.Tests/Generated/Sdl3/Sdl3LayoutTests.g.cs`. Hand-written support:
  `src/Jade.Interop/NulTerminatedUtf8.cs`, `tests/Jade.Interop.Tests/{HostRid,Sdl3Tests}.cs`, and the test
  project copies the staged `jade_native`. `ci.yml` has a `bindings` job. CLAUDE.md has the command and the
  `#:package` rule.
- Verification (commands and results), on linux-x64 (CachyOS, .NET SDK 10.0.401, staged SDL 3.4.16):
  - Generator: `dotnet scripts/generate-bindings.cs` runs in 0.5 to 0.6 s for the 12 targets. Deleting both
    `Generated` folders and regenerating, twice: SHA-256 over all 25 `*.g.cs` files is
    `8c126d4c…3d76c1f` both times. No CR and no BOM in the output (`grep`).
  - Layout report printed by the generator (no unhandled variance):

    ```text
    Sdl3: layout report: these structs differ between targets, and the generated definitions match each target:
      SDL_DisplayMode: size 40, align 8, offsets 0 4 8 12 16 20 24 28 32 on 11 targets; size 36, align 4, offsets 0 4 8 12 16 20 24 28 32 on browser-wasm
      SDL_Surface: size 48, align 8, offsets 0 4 8 12 16 24 32 40 on 11 targets; size 32, align 4, offsets 0 4 8 12 16 20 24 28 on browser-wasm
    ```

    Both differ only through pointer fields, which follow the runtime's pointer size.
  - `dotnet build -c Release --no-incremental`: `0 Avertissement(s)`, `0 Erreur(s)`, CS1591 included (no
    suppression). `dotnet build scripts/generate-bindings.cs --no-incremental -v n`: 0 warnings with the
    repository, AOT and trim analyzers.
  - `dotnet test -c Release`: total 19, failed 0, succeeded 19, skipped 0. Besides the 001 test: 9 generated
    layout tests (size and every field offset against clang's value for the host RID), and `Sdl3Tests`:
    `SDL_GetVersion` equals the staged `versions.json` (3004016) and `SDL_GetRevision` contains
    `release-3.4.16`; `SDL_Init(0)` then `SDL_Quit` leaves `SDL_WasInit(0) == 0`; a quit event pushed with the
    `ref` overload comes back through the `out` overload of `SDL_PollEvent`; `SDL_ClearError`; UTF-8 overloads
    with 10, 255, 256 and 4000 bytes (stack and native-memory paths); a hidden window created with
    `"Jade"u8`, retitled, sized and found by ID.
  - Headless video (window test): with `DISPLAY`, `WAYLAND_DISPLAY` and `XDG_RUNTIME_DIR` unset,
    `SDL_Init(SDL_INIT_VIDEO)` fails with `No available video device` and the test is skipped with that
    message (18 passed, 1 skipped). `XDG_RUNTIME_DIR` matters: without it set but with the session's
    `wayland-0` socket reachable, libwayland still connects. With `SDL_VIDEO_DRIVER=dummy` and no display,
    all 19 pass. On macOS the test skips by design (see Decisions).
  - Negative checks, each reverted afterwards: changing one expected offset in a generated layout test
    makes it fail; dropping `padding` from the `SDL_Event` member list fails generation with
    `SDL_Event on <rid>: native size 128 ..., generated size 32 ...` for every target; removing the
    `SDL_SetError` exclusion fails with `SDL_SetError is variadic ...`; a `PerPlatform` layout decision fails
    as not implemented; making `SDL_Surface` opaque reports `SDL_SurfaceFlags` as a stale config entry;
    `UnsupportedPlatforms` on `SDL_SetWindowTitle` adds `[UnsupportedOSPlatform("browser")]` to the raw,
    friendly and instance forms, and `Jade.Interop` still builds with 0 warnings.
  - Import style, `dotnet build src/Jade.Interop -c Release --no-incremental -clp:PerformanceSummary`, warm
    build server, 6 runs after a discarded one: `Csc` 66 to 81 ms with `[LibraryImport]` against 47 to 57 ms
    with `[DllImport]`; total elapsed 0.67 to 0.69 s against 0.65 to 0.67 s. With
    `EmitCompilerGeneratedFiles`, the LibraryImport generator emits, for each method, only an
    `extern partial` carrying `[DllImport("jade_native", EntryPoint = ..., ExactSpelling = true)]`: with
    blittable signatures there is nothing to marshal.
  - `actionlint .github/workflows/ci.yml`: no findings.
  - Not verified: the `bindings` CI job has not run (it needs a push); that ubuntu-latest has `clang` and
    `cmake` on PATH for `--toolchain=clang` is assumed from the runner image, not checked. The generator ran
    only on linux-x64; Windows and macOS hosts are untested. NativeAOT publish of the bindings is task 205.
  - Excerpts:

    ```csharp
    // Function (Sdl.Video.g.cs): raw import and UTF-8 overload.
    [LibraryImport("jade_native", EntryPoint = "SDL_CreateWindow")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Window CreateWindow(byte* title, int w, int h, WindowFlags flags);

    public static Window CreateWindow(ReadOnlySpan<byte> title, int w, int h, WindowFlags flags)
    {
        using var __titleUtf8 = new NulTerminatedUtf8(title, stackalloc byte[NulTerminatedUtf8.StackLength]);
        fixed (byte* __title = __titleUtf8)
        {
            return CreateWindow(__title, w, h, flags);
        }
    }

    // Enum (InitFlags.g.cs), from `typedef Uint32 SDL_InitFlags` and its #defines.
    [Flags]
    public enum InitFlags : uint
    {
        /// <summary><c>SDL_INIT_VIDEO</c> implies <c>SDL_INIT_EVENTS</c>, should be initialized on the main thread</summary>
        Video = 0x00000020,
        // ...
    }

    // Struct (Rect.g.cs).
    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct Rect
    {
        public int X;
        public int Y;
        public int W;
        public int H;
    }

    // Handle methods (Window.g.cs), for SDL_GetWindowSize(SDL_Window*, int*, int*).
    public byte GetSize(int* w, int* h) => Sdl.GetWindowSize(this, w, h);
    public byte GetSize(out int w, out int h) => Sdl.GetWindowSize(this, out w, out h);
    ```

- Decisions taken (and ADRs added):
  - ClangSharp `21.1.8.4` (still the latest on nuget.org, checked with the `nuget` MCP). It depends on
    ClangSharp.Interop 21.1.8.4, libClang 21.1.8 and libClangSharp 21.1.8.2. The last two are meta packages
    whose `runtime.json` maps RIDs to `libclang.runtime.<rid>` and `libClangSharp.runtime.<rid>`, published for
    linux-x64, linux-arm64, osx-arm64, win-x64 and win-arm64 (osx-x64 stopped at 18.1.3, so the generator
    cannot run on Intel Macs). The package holds `libclang.so` only, not clang's builtin headers.
  - `#:package` with Central Package Management (open since ADR-0008): `#:package ClangSharp` without a
    version, and `<PackageVersion Include="ClangSharp" Version="21.1.8.4" />` in `Directory.Packages.props`.
    A versioned `#:package ClangSharp@21.1.8.4` fails with NU1008 (checked). Native assets need
    `#:property RuntimeIdentifier=$(NETCoreSdkRuntimeIdentifier)`: without it, restore skipped them and the
    script silently loaded the system libclang 23.1.1. The generator now refuses any libclang whose
    `clang_getClangVersion` does not start with the version ClangSharp was built for (21.1.8). Rule added to
    CLAUDE.md's Scripts section. No `PublishAot=false` was needed.
  - Per-library config in C# (`LibraryConfig`, built by `Sdl3Config.cs`), not JSON: rules are typed and
    checked by the compiler (`ParameterRule.Span("numrects")`, `LayoutDecision`), reasons sit next to each
    entry, patterns are regexes, and there is no schema or serializer to keep AOT-clean. It covers prefixes,
    renames, word casing, handles, opaque structs, flag typedefs and flag enums, parameter rules (in, out,
    ref, span with its count, raw), exclusions with reasons, union member subsets, layout decisions, target
    triples, the C `bool` type and platform availability. Every entry must match something: a stale or
    misspelled one fails generation.
  - Parsing without SDKs: `-x c -std=c11 --target=<triple> -nostdinc -isystem sysroot -I include` plus the
    upstream's `versions.json` defines, with `SkipFunctionBodies` and `DetailedPreprocessingRecord`.
    `sysroot/` holds stub headers (`stddef.h`, `stdint.h`, `stdarg.h`, `stdbool.h`, `endian.h`,
    `TargetConditionals.h`, `AvailabilityMacros.h`, `winapifamily.h`, `sal.h`, and empty `string.h`,
    `wchar.h`, `intrin.h`, ...) defined from clang's per-target predefined macros. All 12 triples parse with 0
    diagnostics, so no triple is left out and every host parses identically. Triples: `x86_64`/`aarch64-pc-windows-msvc`,
    `x86_64`/`aarch64-unknown-linux-gnu`, `x86_64`/`arm64-apple-macosx11.0`, `aarch64`/`x86_64-linux-android`,
    `arm64-apple-ios`, `arm64`/`x86_64-apple-ios-simulator`, `wasm32-unknown-emscripten`. macOS carries 11.0
    only because `SDL_platform_defines.h` rejects anything below 10.7 (the system clang 23 defaults an
    unversioned macOS triple to 10.4).
  - Model and readers: the reader resolves everything target- and source-specific (C to C# names, docs,
    parameter rules, flag values) into `LibraryModel`; `TypeRef` is target-independent; `CSharpEmitter` only
    formats. A `dawn.json` reader (202) produces the same `LibraryModel`.
  - Variance check: each target's model is emitted and the files compared with the first target's;
    then, per target, the CLR layout of each struct (`LayoutCalculator`: sequential with natural alignment,
    unions at offset 0) is compared with clang's size, alignment and offsets. A difference is an error unless
    the config records a `LayoutDecision` (only `Opaque` is implemented; `AccessorShim` and `PerPlatform`
    fail as not implemented). Generated layout tests check `LayoutCalculator` against the real runtime.
  - Type mapping: fixed-width typedefs (`intN_t`, `uintN_t`, `size_t`, `intptr_t`, ...) map by name before
    any desugaring, since `uint64_t` is `unsigned long` on Linux and `unsigned long long` on Windows. `long`
    maps to `CLong`. `wchar_t`, `va_list`, `long double`, bit-fields, anonymous members, flexible arrays and
    arrays of non-scalars are rejected with a message. Every builtin's clang size is checked against its
    managed size on each target.
  - C `bool` maps to `byte` for SDL (config `Bool`). A 1-byte wrapper struct would read better, but its ABI
    then depends on whether each platform passes a 1-byte struct exactly like a 1-byte integer (for example
    the extension of small arguments on Apple arm64), which cannot be verified here.
  - Enums: the underlying type has clang's size; it is signed if a value is negative or, for non-flags, if
    every value fits the signed type (so `EventType` is `int` while `SDL_Event.type` is `uint`). Flag typedefs
    (`SDL_InitFlags`, `SDL_WindowFlags`, `SDL_SurfaceFlags`) take their typedef's type, their members are the
    object-like macros with the configured prefix in the typedef's header, values evaluated by libclang
    through probe variables, docs from the trailing `/**< */` comments.
  - Names: prefix stripped, all-caps words capitalized (`WINDOW_SHOWN` to `WindowShown`, with `Words`
    overrides such as `OpenGL`), PascalCase C names kept (`GetWindowID`), camelCase parameters with keywords
    escaped (`@event`). A handle's instance method drops the handle name after the leading verb
    (`SDL_GetWindowSize` to `window.GetSize`, `SDL_DestroyWindow` to `Destroy`, `SDL_WindowHasSurface` to
    `HasSurface`, but `GetDisplayForWindow` unchanged).
  - Handles: `readonly partial struct Window : IEquatable<Window>` with `nint Handle`, `IsNull` and pointer
    equality, no ownership. Every function whose first parameter is the handle gets instance forms of its raw
    and friendly overloads.
  - Friendly overloads: one per function, converting every annotated parameter. `const char*` parameters get
    a `ReadOnlySpan<byte>` form automatically; out, in, ref and span forms come from config rules taken from
    each function's documentation. `out` values are zeroed before the call. `NulTerminatedUtf8` passes a span
    already ending in NUL as is, copies shorter than 256 bytes to a `stackalloc` buffer, and longer ones to
    `NativeMemory` freed on `Dispose`: no GC allocation at all, rather than ADR-0006's "pooled" (an
    `ArrayPool` rents GC arrays when empty). Generated code contains no `throw`.
  - Callbacks are written inline as `delegate* unmanaged[Cdecl]<...>`; the callback typedef's docs are not
    carried over.
  - Docs: a Doxygen parser for SDL's style (summary, paragraphs, `-` lists, fenced code, `\param`,
    `\returns`, `\threadsafety`, `\since`, `\sa`, other commands kept as labelled paragraphs) feeds XML docs
    wrapped at 120 columns. Functions and types also get `Binds <c>SDL_X</c>.` (as the summary when upstream
    has none); every C# parameter gets a `<param>`, so CS1572 and CS1573 cannot fire.
  - Bindable functions: declarations in a bound header without body, `inline` or `static`. Variadic and
    `va_list` functions must be listed in `Exclusions` (SDL_SetError, SDL_SetErrorV here).
  - Import style: `[LibraryImport]` with `[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]`, at the
    user's request (the current .NET interop model). The emitter still supports `[DllImport]`
    (`ImportStyle`), which built faster on the slice (see Verification); both name `jade_native`.
  - Determinism: functions in header order, one file per type in ordinal name order, no timestamps, LF, no
    BOM, one header line pointing at the generator; unchanged files are not rewritten. `.gitattributes`
    gets `*.g.cs text eol=lf` so a Windows checkout matches what the generator writes.
  - Native tests: the test project copies `artifacts/native/$(NETCoreSdkRuntimeIdentifier)/lib/*jade_native*`
    and `versions.json`; tests that need the library skip when it is absent (the build job). All SDL tests
    are in one class, since SDL state is global. The window test skips on macOS, where SDL video needs the
    main thread and xunit runs tests on worker threads.
  - CI: job `bindings` on ubuntu-latest: setup-dotnet, `xmake-io/github-action-setup-xmake@v1` with xmake
    `3.1.1` (the tag is maintained, node24), `dotnet scripts/build-native.cs`, regeneration,
    `git add --intent-to-add` on both `Generated` folders then `git diff --exit-code` (so new files count),
    and `dotnet test -c Release` with `SDL_VIDEO_DRIVER=dummy`. Building SDL3 and miniaudio in the job is
    cheap enough until Dawn arrives.
  - At the user's request, separate commit: `src/Jade.Interop/AssemblyInfo.cs` moved to
    `src/Jade.Interop/Properties/AssemblyInfo.cs` (the only `AssemblyInfo.cs` in the repository).
  - No ADR added.
- Deviations from the brief:
  - The generator also writes layout tests to `tests/Jade.Interop.Tests/Generated/Sdl3/`, outside
    `src/Jade.Interop/Generated/Sdl3/`; the CI diff covers both.
  - "Window functions only" is read as `^SDL_(?!GL_|EGL_)\w*Window` in `SDL_video.h` (72 functions);
    "enough to poll a quit event" as `SDL_PumpEvents`, `SDL_PollEvent`, `SDL_WaitEvent`,
    `SDL_WaitEventTimeout`, `SDL_PushEvent` and `SDL_GetWindowFromEvent`, with `SDL_Event` bound with the
    members `type`, `common`, `display`, `window`, `quit` and `padding` (it keeps its 128 bytes).
  - The CI job also runs the tests against the freshly built `jade_native`.
- Follow-ups:
  - 203: no constants yet (`SDL_PROP_*` strings, `SDL_WINDOWPOS_*`, version macros); no `[InlineArray]` for
    arrays of structs, no anonymous members or bit-fields; callback typedef docs are dropped; `const char*`
    returns stay `byte*`; ID typedefs (`SDL_WindowID`, ...) are plain `uint`; enum member casing needs `Words`
    for SDL_PixelFormat (`Index1lsb`, `Rgba8888`, ...); `bool` as `byte` may deserve a friendlier wrapper once
    its ABI is checked on Apple arm64; `SDL_SetWindowFillDocument` only acts on Emscripten but stays callable
    everywhere, so it is not marked.
  - 202: build the `dawn.json` reader into `LibraryModel`; `VarianceCheck` and `LayoutTestEmitter` take any
    list of `TargetModel`. `WGPUBool` is a `uint32_t` typedef, so it maps to `uint` without the `Bool` setting.
  - 102/103: once Dawn makes `build-native.cs` too slow for the `bindings` job, download the staged inputs
    from `native.yml` (`scripts/fetch-native.cs`). Run the generator on Windows and macOS hosts.
  - 204 and later libraries: system headers they include may need more stubs in `sysroot/`; the parse fails
    with the missing header's name.
  - 205: NativeAOT publish of the LibraryImport bindings.
  - 106: replace the test project's copy of the staged library with the `Jade.Native` package.
  - Orchestrator: `design/architecture.md`'s bindings pipeline does not mention the generated layout tests;
    `src/Jade.Interop/Generated/.gitkeep` is now redundant.
