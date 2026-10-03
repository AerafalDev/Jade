# 204: miniaudio bindings

- Depends on: 201, 003
- ADRs: 0005, 0006

## Goal

miniaudio's public API, as compiled into jade_native, generated into
`src/Jade.Interop/Generated/Miniaudio/`. Platform-dependent struct layouts are handled explicitly.

## Context

- miniaudio is a single header with implementation behind `MINIAUDIO_IMPLEMENTATION`. Its public
  structs (`ma_device`, `ma_context`, ...) embed backend-specific members under `#ifdef`, so their
  size differs between platforms. This is the main test of 201's layout-variance handling.
- The defines used in 101 change which APIs and members exist. Parse with exactly those defines;
  they are recorded in `artifacts/native/<rid>/metadata/versions.json`.

## Scope

- Config for the public API.
- Variant structs: per ADR-0006, choose opaque handle plus accessor shim (in `native/shims/`), or
  per-platform definitions, case by case. Prefer the low-level device API plus the high-level
  engine API (`ma_engine`, `ma_sound`) as the main surfaces. Users allocate them through sized
  allocation helpers in a shim (`jade_ma_sizeof_*` or similar), so C# never relies on a
  per-platform `sizeof`.
- Export cross-check like 202.

## Out of scope

- The engine's audio API.

## Acceptance criteria

- [ ] Deterministic output; the build is clean.
- [ ] The layout-variance report is clean or fully handled by the config, with nothing silently
      exposed.
- [ ] Tests: version string; a decoder over an in-memory WAV produced by the test; and an
      `ma_engine` initialized with the null backend (no audio device in CI).

## Verification

Generator run, build, variance report, test run.

## Pitfalls

- From 103: `ma_atomic_global_lock` is a variable, not an API, yet it leaks into the exports on Linux
  and Windows. Do not bind it, and remove it from the export list if the package allows it.
- From 201: miniaudio's headers include more system headers than SDL3's slice. Add the missing
  stubs to `scripts/generate-bindings/sysroot/`; the parse names the missing header.
- ADR-0012: `ma_bool8` and `ma_bool32` are integer typedefs, not C `bool`. Keep their width.
- Callbacks from miniaudio's audio thread run outside managed context unless entered through
  `UnmanagedCallersOnly`. Document the constraints in the generated XML docs.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
