# 0010. Native libraries built by us with xmake

- Status: Accepted
- Date: 2026-10-05

## Context

Jade ships Dawn, SDL3 and miniaudio, plus our own C shims, for twelve runtime identifiers (see
[0012](0012-supported-targets.md)). Prebuilt binaries from upstream do not cover every target, are
not built with the same options, and cannot guarantee that the browser archives match the
Emscripten version of the .NET workload.

## Decision

- We compile every native library ourselves, for every target, with xmake.
- Our xmake package definitions and C shims live in `native/`. They delegate to CMake for Dawn and
  SDL3.
- `native/versions.json` is the single source of the pinned versions (Dawn, SDL3, miniaudio, emsdk,
  Android NDK and others, minimum OS versions once decided). Scripts, the binding generator and CI
  all read it.
- CI builds the natives on a matrix of runners and publishes them as artifacts.
- `scripts/fetch-native.cs` downloads those artifacts for local work; `scripts/build-native.cs`
  builds them locally.
- macOS and the iOS simulator get universal binaries (arm64 and x64 merged with `lipo`).
- Linux binaries are built against an old glibc baseline so that they run on as many
  distributions as possible.

## Consequences

- Every binary is reproducible from a pinned version and our own build definitions.
- We own the toolchain complexity: C++ toolchains, Android NDK, Xcode, emsdk and an old-glibc Linux
  environment.
- Dawn builds are long; CI rebuilds a native only when its inputs change.
- Provenance attestations are attached to the native binaries (see
  [0017](0017-github-repository-baseline.md)).
