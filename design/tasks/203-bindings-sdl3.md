# 203: SDL3 bindings

- Depends on: 201, 003
- ADRs: 0005, 0006

## Goal

The complete public SDL3 API that jade_native exports, generated into
`src/Jade.Interop/Generated/Sdl3/`. Overloads and handles follow ADR-0006, and the config documents
every exclusion.

## Context

- 201 delivered the generator core and a vertical slice of SDL3. This task extends the config to
  every public header staged by 101.
- 101 disabled SDL's audio, GPU and render subsystems, but their functions are still exported (the
  export list is upstream's `SDL_dynapi.sym`) and fail at runtime with "not built with ... support".
  Exclude those headers (`SDL_audio.h`, `SDL_gpu.h`, `SDL_render.h` and whatever depends only on
  them) from the bindings. The export cross-check lists them as exported but intentionally unbound.
- ADR-0012: C `bool` maps to C# `bool`. 201 emitted `byte`. Change the mapping in the generator and
  regenerate. Then update CLAUDE.md's interop bullet if it still says otherwise.
- From 201, gaps to close in the generator or `Sdl3Config.cs`:
  - constants (`SDL_PROP_*` strings, `SDL_WINDOWPOS_*`, version macros);
  - `[InlineArray]` for arrays of structs, anonymous members and bit-fields;
  - callback typedef docs, which are dropped today;
  - `const char*` returns, still `byte*` (a span or UTF-8 helper?);
  - ID typedefs (`SDL_WindowID`, ...), which are plain `uint` (typed IDs?);
  - enum member casing that needs word splitting for `SDL_PixelFormat` (`Index1lsb`,
    `Rgba8888`, ...);
  - `SDL_Event`, bound so far with only `type`, `common`, `display`, `window`, `quit` and padding,
    which needs every member;
  - Emscripten-only functions such as `SDL_SetWindowFillDocument`, which are not marked.
- From 201: on macOS, SDL requires window calls on the main thread, and xunit runs tests on other
  threads, so window tests are skipped there. Keep that explicit.
- From 201: headers that include more system headers may need more stubs in
  `scripts/generate-bindings/sysroot/`. The parse fails with the missing header's name.

## Scope

- Config for all public SDL3 headers: handles (`SDL_Window*`, `SDL_Gamepad*`, ...), properties API,
  events union, callbacks, `SDL_IOStream`, error handling, and out parameters across the API.
- The events union: an exact-layout C# representation, checked by the layout-variance report on
  every triple.
- Inline functions and macros that users need (for example `SDL_BUTTON_MASK`): hand-written helpers
  in `src/Jade.Interop/Sdl3/`, kept to a minimum and listed in the Outcome.
- Export cross-check like 202: every exported `SDL_*` is bound or excluded with a reason.

## Out of scope

- Platform main and app lifecycle glue (104 ADRs), engine input abstractions.

## Acceptance criteria

- [ ] Deterministic output; the build is clean.
- [ ] The export cross-check passes.
- [ ] The layout-variance report is clean on every triple 201 parses.
- [ ] Tests: version, init and quit without video, the properties round-trip, and `SDL_IOStream`
      from memory. Anything needing a display is skipped headless, with a reason.

## Verification

Generator run, build, cross-check output, test run.

## Pitfalls

- SDL3 uses C `bool` (1 byte). Keep it distinct from WebGPU's 32-bit `WGPUBool`.
- Variadic functions (`SDL_Log`, `SDL_SetError`, ...) cannot be called through P/Invoke portably.
  Exclude them or bind a non-variadic sibling, and document which.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
