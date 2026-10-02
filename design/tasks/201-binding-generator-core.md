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

- [ ] Running the generator twice gives byte-identical output.
- [ ] The solution builds with the generated slice, with 0 warnings.
- [ ] A test in `tests/Jade.Interop.Tests` calls the generated `SDL_GetVersion` and one window-less
      function (`SDL_Init` with no subsystems or video, then `SDL_Quit`) against the 101 library and
      passes. Video needs a display, so document how it behaves headless.
- [ ] The layout-variance report runs and is clean for the slice, or lists what the config handles.

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

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
