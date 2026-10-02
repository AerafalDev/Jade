# ADR-0007: Target platforms

- Status: Accepted
- Date: 2026-10-02

## Context

Jade targets desktop, mobile and the browser from the start and covers the common cases. Rarer
targets must remain possible without being built now.

## Decision

**Built now**: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`,
`android-arm64`, `android-x64`, `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64`,
`browser-wasm`.

**Deferred**: not built, but nothing in the design may rule them out. These are `maccatalyst-*`,
`tvos-*`, `linux-musl-*`, `android-arm`, `android-x86` and `linux-riscv64`.

**Excluded**: consoles, whose SDKs are under NDA and incompatible with a public repository.

## Consequences

- Linux binaries target an old glibc baseline chosen by task 103 and recorded in an ADR.
- Android and iOS need platform glue: SDL3's Java `SDLActivity` on Android and the app lifecycle on
  iOS. That implies `net10.0-android`/`net10.0-ios` target frameworks and workloads somewhere in
  Jade. Task 104 investigates and proposes where. `Jade.Interop` stays plain `net10.0` if possible.
- On browser-wasm, .NET links natives statically through `NativeFileReference` with the Emscripten
  version bundled by its wasm workload. `jade_native.a` must be built with exactly that version,
  which must also suit emdawnwebgpu (task 105).
- The free arm64 runners require the repository to be public on GitHub.
