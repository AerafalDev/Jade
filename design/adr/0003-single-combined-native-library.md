# ADR-0003: One combined native library per RID

- Status: Accepted; amended by ADR-0019 (a second library, `jade_tools`, for tools)
- Date: 2026-10-02

## Context

Jade bundles several native libraries: Dawn, SDL3, miniaudio, then ImGui, Box2D/Box3D, FreeType,
HarfBuzz, msdfgen and others. They could ship as one library each or be linked into a single
library. On browser-wasm and possibly iOS, linking is static anyway.

## Decision

- Each RID gets one native library named `jade_native`: `jade_native.dll`, `libjade_native.so` or
  `libjade_native.dylib` on desktop and Android, `jade_native.a` on browser-wasm. Task 104 decides
  the iOS form.
- Every upstream is linked statically into it.
- It exports only the public C API of each bundled library and our `jade_*` shims. Everything
  else, including C++ runtime symbols and upstream internals, stays hidden.
- Every C# import uses the library name `jade_native`.
- The C++ runtime is linked statically where the platform allows it (static libstdc++/libc++ on
  Linux and Android, static CRT `/MT` on Windows). Tasks 102 and 103 confirm this per platform and
  record any exception.

## Consequences

- No clash with a system SDL3 or with other copies loaded in the process. One load, one
  `NativeLibrary` resolver at most.
- Export control is our job. Symbols from static archives are not exported from a shared library
  by default, so each platform needs whole-archive linking plus export macros, a `.def` file, a
  version script or `-exported_symbols_list`. Task 101 sets up the mechanism; each later library
  plugs into it.
- Bumping one upstream relinks the whole library. Per-package build caching keeps that cheap.
