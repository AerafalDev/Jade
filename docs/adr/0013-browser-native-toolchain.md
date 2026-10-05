# 0013. Browser natives prebuilt with the workload's Emscripten version

- Status: Superseded by [0025](0025-browser-natives-with-workload-emscripten.md)
- Date: 2026-10-05

## Context

In the browser, native code is linked statically into the .NET WebAssembly module by the `emcc`
shipped with the `wasm-tools` workload. Archives built with a different Emscripten version can fail
to link or misbehave at runtime. The workload's Emscripten cache is read-only during
`dotnet build`, so Emscripten ports cannot be fetched or built at that point.

Verified on 2026-10-05: the `microsoft.net.workload.emscripten.current` manifest of SDK
`11.0.100-rc.1.26425.128` references the `Microsoft.NET.Runtime.Emscripten.6.0.2.*` packs, so the
.NET 11 RC1 workload uses Emscripten 6.0.2.

## Decision

- Every browser native archive is built with exactly the Emscripten version of the `wasm-tools`
  workload of the SDK pinned in `global.json` (6.0.2 at the time of writing). The version is
  recorded in `native/versions.json` and re-checked at every SDK update.
- No `--use-port` during `dotnet build`. SDL3, miniaudio and Emdawnwebgpu are prebuilt with a
  standalone emsdk of the same version.
- Archives are named after the module used in the import declarations (`SDL3.a`, not `libSDL3.a`).
- The Emdawnwebgpu JavaScript library is passed to the `emcc` linker by the package's
  `buildTransitive/` targets (see [0011](0011-native-package-layout.md)).
- wasm32 has 4-byte pointers and `size_t`; the generator and the layout tests cover it.
- The main loop never blocks: it is driven by `requestAnimationFrame`. WebGPU initialization is
  asynchronous.

## Consequences

- An SDK update can force a rebuild of every browser archive. CI should compare the workload's
  Emscripten version with `native/versions.json` and fail on a mismatch.
- Engine code cannot assume a blocking main loop or synchronous GPU initialization on any
  platform, since the browser forbids both.
