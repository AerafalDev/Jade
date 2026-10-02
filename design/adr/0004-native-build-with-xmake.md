# ADR-0004: xmake drives the native build

- Status: Accepted
- Date: 2026-10-02

## Context

`jade_native` must build for about twelve RIDs with MSVC, clang/gcc, the Android NDK, Xcode and
Emscripten. The candidates were plain CMake + Ninja, zig cc cross-compilation and xmake.

- Dawn builds with CMake (and GN). Building it with zig is not an upstream-supported path; Windows
  would end up on the mingw ABI, and Apple targets need the Xcode SDK anyway.
- xmake 3.1.1 (installed locally) has the `windows`, `linux`, `macosx`, `android`, `iphoneos` and
  `wasm` platforms. Its packages can drive a CMake build (`package.tools.cmake`).
- xmake-repo has `libsdl3`, `miniaudio`, `emscripten` and `wgpu-native`, but no Dawn. Its versions
  lag upstream (`libsdl3` 3.4.12 vs 3.4.16 upstream on 2026-10-02).

## Decision

- One xmake project under `native/` defines the `jade_native` target.
- A local package repository under `native/packages/<letter>/<name>/xmake.lua`, using the
  xmake-repo layout, holds our own definitions. Each one pins an exact version or commit plus
  SHA-256. xmake-repo definitions serve as reference, not as source.
- The Dawn package drives Dawn's CMake.
- Each RID builds on its native OS in CI. Locally, a developer builds their host RID.
- `scripts/build-native.cs` is the only entry point. It maps a .NET RID to xmake platform, arch and
  toolchain, and stages outputs into `artifacts/native/<rid>/`. Nobody calls xmake with ad-hoc flags
  in CI or in docs.

## Consequences

- One CLI and one configuration language for every target, with no per-OS build scripts.
- xmake becomes a prerequisite for anyone building natives. Managed-only contributors use the
  natives built by CI (`scripts/fetch-native.cs`).
- Dependabot cannot track xmake packages. Upstream bumps are deliberate tasks, possibly helped by a
  script later.
