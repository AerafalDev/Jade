# 105: browser-wasm

- Depends on: 103
- ADRs: 0001, 0003, 0007

## Goal

`jade_native.a` for browser-wasm contains SDL3 (Emscripten port), miniaudio (Web Audio) and
emdawnwebgpu. It is built with the exact Emscripten version used by the .NET 10 wasm workload, and a
minimal .NET browser app links it and calls into it.

## Context

- .NET browser-wasm links native code statically through `NativeFileReference`, with the Emscripten
  toolchain the wasm workload bundles. Objects built with a different Emscripten version are not
  guaranteed to link. Find the exact version from the workload packages or manifests; the version is
  not known yet.
- Dawn ships `emdawnwebgpu_pkg-<tag>.zip` and a `remoteport.py` in each release. It implements
  `webgpu.h` over the browser's WebGPU and may require a recent Emscripten.
- Neither the `wasm-tools` workload nor emsdk is installed locally. Ask the user before installing:
  the workload goes into `~/.dotnet`, which is not a system package, but it is still their machine.

## Scope

- Determine the .NET 10 Emscripten version and check emdawnwebgpu's minimum Emscripten. **If they
  are incompatible, stop.** Report the facts and write a Proposed ADR with options (for example an
  older Dawn tag for the web, or a custom link step). Do not build around it silently.
- Otherwise:
  - xmake `wasm` builds of SDL3, miniaudio and emdawnwebgpu into `jade_native.a`, through
    `scripts/build-native.cs --rid browser-wasm`;
  - stage the JS libraries and link flags emdawnwebgpu and SDL3 need, under
    `artifacts/native/browser-wasm/`;
  - pick the threading model (.NET wasm defaults to single-threaded) and record it.
- A minimal browser-wasm test app (under `tests/` or `samples/`) with `NativeFileReference` and
  `[DllImport("jade_native")]` calls to `SDL_GetVersion` and `wgpuCreateInstance`.
- Hand 106 the exact items its `buildTransitive` targets must add for browser-wasm.

## Out of scope

- Generated bindings on the web (202 covers availability), packaging (106).

## Acceptance criteria

- [ ] The Emscripten versions (.NET workload, emdawnwebgpu requirement, version used) are stated with
      sources.
- [ ] Either `jade_native.a` links into the test app, or a Proposed ADR explains why it cannot yet.
- [ ] The test app builds. Running it in a browser with WebGPU cannot be verified headlessly here;
      say what was and was not run.

## Verification

Build commands for the archive and the test app; `emnm` (or `llvm-nm`) on the archive showing the
expected symbols.

## Pitfalls

- From 202:
  - stage emdawnwebgpu's `webgpu.h` and parse it for browser-wasm, instead of Dawn's header with
    `__EMSCRIPTEN__` undefined;
  - emdawnwebgpu's blocking `WaitAny` aborts unless built with ASYNCIFY (`emwgpuWaitAny` sits under
    `#if ASYNCIFY` in `library_webgpu.js`). Decide between an ASYNCIFY build and callback modes
    (`AllowSpontaneous`, `AllowProcessEvents`) for the web, and record it;
  - check a callback round-trip through a callback-info struct and its typed function-pointer field;
  - check whether a pointer to a function pointer in a signature (`SDL_GetMemoryFunctions`) fails
    like a function pointer does; ADR-0016 only covers the latter;
  - check that structs passed by value reach C as it expects on wasm32.
- From 106: add browser-wasm to `JadeNativeRequiredRids` and fill the browser-wasm placeholder in
  `src/Jade.Native/buildTransitive/Jade.Native.targets`. Everything under
  `artifacts/native/browser-wasm/lib/` ships, JavaScript libraries included.
- ADR-0016: function pointer parameters are `nint` in import signatures because browser-wasm does
  not parse function pointer types in P/Invoke signatures (dotnet/runtime#56145). Verify a callback
  round-trip (native code calling an `[UnmanagedCallersOnly]` method passed through such a
  parameter), and check that struct fields of function pointer type are fine. Also check by-value
  struct arguments: 143 ImGui functions take `ImVec2` and similar by value (301).
- From 301: ImGui's WebGPU backend needs Emscripten 4.0.10 or later with emdawnwebgpu. Only 6.0.11
  was tried, through the Docker image `emscripten/emsdk:6.0.11`, which is still on this machine and
  can be reused.
- P/Invokes on browser-wasm resolve at build time from the library name. Check that `jade_native`
  in `DllImport` matches the `NativeFileReference` naming rules.
- emdawnwebgpu's async APIs depend on Emscripten options (Asyncify or JSPI?). Check what it
  requires and whether .NET's wasm build allows it.
- From 101: the static `jade_native.a` needs the upstream archives merged into it.
  `native/rules/bundle.lua` raises for non-shared targets today. Export control has no meaning in a
  static archive, but symbol clashes between upstreams do.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
