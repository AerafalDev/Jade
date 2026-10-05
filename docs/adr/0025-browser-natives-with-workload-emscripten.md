# 0025. Browser natives built with the .NET workload's Emscripten toolchain

- Status: Accepted
- Date: 2026-10-05

## Context

In the browser, native code is linked statically into the .NET WebAssembly module by the `emcc`
shipped with the `wasm-tools` workload. Archives built with a different Emscripten can fail to link
or misbehave at runtime. The workload's Emscripten cache is read-only during `dotnet build`, so
Emscripten ports cannot be fetched or built at that point.
[0013](0013-browser-native-toolchain.md) therefore required every browser archive to be built with
exactly the workload's Emscripten version, using a standalone emsdk of that version. Pinning the
native dependencies (roadmap task 4) re-checked that version.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`:

- The `microsoft.net.workload.emscripten.current` manifest references the
  `Microsoft.NET.Runtime.Emscripten.6.0.2.*` packs (version `11.0.0-rc.1.26425.128`), and
  `eng/Versions.props` of `dotnet/emsdk` sets `EmscriptenVersion` to 6.0.2.
- The `emcc` of the installed `Microsoft.NET.Runtime.Emscripten.6.0.2.Sdk.linux-x64` pack prints
  `6.0.3`. Its `emscripten-version.txt`, which the pack's `Sdk.props` reads into the
  `EmscriptenVersion` MSBuild property, also says 6.0.3.
- The pack is built from the `dotnet/emscripten` fork: `src/emsdk/eng/Version.Details.xml` of the
  VMR tag `v11.0.100-rc.1.26425.128` pins its commit `b958186`, which contains the upstream `6.0.2`
  tag and about 40 of the upstream commits leading to `6.0.3`, plus .NET's own commits. The pack's
  `clang` is `23.1.0-rc2` from `dotnet-llvm-project`.
- No upstream emsdk release is therefore the workload's toolchain: `6.0.2` lacks upstream commits
  the workload has, `6.0.3` has 42 it lacks, and both ship another LLVM build. The "exact version"
  rule of 0013 cannot hold with a standalone emsdk.
- The pack's `emcc` runs outside MSBuild with an `EM_CONFIG` that points at the pack's LLVM and
  Binaryen (`tools/bin`) and at the Node pack (checked with `emcc --version` on Linux). The pack
  also ships `emcmake` and `cmake/Modules/Platform/Emscripten.cmake`.
- Emdawnwebgpu, at the Dawn commit pinned in `build/versions.json`, needs Emscripten 4.0.10 or
  later, and the Dawn release notes say it was tested with emsdk 5.0.6.

This record restates 0013 with the toolchain changed, and supersedes it.

## Decision

- Every browser native archive (SDL3, miniaudio and its shim, Emdawnwebgpu) is compiled with the
  Emscripten toolchain of the `wasm-tools` workload of the SDK pinned in `global.json`: its `emcc`,
  LLVM and Binaryen, invoked outside `dotnet build` with our own `EM_CONFIG` and a writable cache.
  No standalone emsdk is installed.
- `build/versions.json` records, under `toolchains.emscripten`, the version that the workload's
  `emcc` reports (6.0.3) and the version in the pack names (6.0.2). Both are re-checked at every
  SDK update, and CI fails when the workload differs from them.
- Browser archives are rebuilt whenever the pinned SDK changes, even if the recorded versions do
  not: a new SDK can ship another fork commit under the same version.
- No `--use-port` during `dotnet build`. SDL3, miniaudio and Emdawnwebgpu are prebuilt archives.
- Archives are named after the module used in the import declarations (`SDL3.a`, not `libSDL3.a`).
- The Emdawnwebgpu JavaScript library is passed to the `emcc` linker by the package's
  `buildTransitive/` targets (see [0011](0011-native-package-layout.md)).
- wasm32 has 4-byte pointers and `size_t`; the generator and the layout tests cover it.
- The main loop never blocks: it is driven by `requestAnimationFrame`. WebGPU initialization is
  asynchronous.
- The emsdk named in [0010](0010-native-builds-with-xmake.md) means this workload toolchain; 0010 is
  not rewritten ([0001](0001-record-architecture-decisions.md)).

## Consequences

- The archives and the application's final link use the same compiler, LLVM and system headers by
  construction.
- Machines that build browser archives need the pinned SDK and `dotnet workload install
  wasm-tools`; nothing else is downloaded for Emscripten.
- Using the workload's toolchain outside MSBuild is not a documented .NET scenario, and the pack
  layout can change between SDK versions. The native CI (roadmap task 10) validates it; if it
  breaks, the fallback is a standalone emsdk of the closest upstream version, with the mismatch
  described above, which would be a new decision.
- Emdawnwebgpu is tested upstream with Emscripten 5.0.6, not 6.x; the first browser build validates
  it with the workload's toolchain.
- Engine code cannot assume a blocking main loop or synchronous GPU initialization on any
  platform, since the browser forbids both.

## Alternatives considered

- **Standalone emsdk 6.0.3**, the version the workload's `emcc` reports: keeps 0013, but carries 42
  upstream commits the workload lacks and another LLVM build.
- **Standalone emsdk 6.0.2**, the version in the pack names: keeps 0013 to the letter, but lacks
  about 40 upstream commits the workload has, with another LLVM build.
- **Building inside `dotnet build` with `--use-port`**: the workload's cache is read-only there.
