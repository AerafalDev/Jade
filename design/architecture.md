# Architecture

This document describes the target shape of the repository and its pipelines. The ADRs in
[adr/](adr/) record why. Task briefs in [tasks/](tasks/) build it piece by piece.

## Layers

```text
┌─────────────────────────────────────────────────────────────┐
│ Jade (engine, later)                    namespace Jade.*    │  idiomatic, safe, owns resources
├─────────────────────────────────────────────────────────────┤
│ Jade.Interop (public)                   Jade.Interop.<Lib>  │  generated, unsafe, zero-cost
│   Generated/<Lib>/*.g.cs   +   <Lib>/ hand-written helpers  │
├─────────────────────────────────────────────────────────────┤
│ jade_native (one library per RID)                           │  Dawn, SDL3, miniaudio, ... + shims
└─────────────────────────────────────────────────────────────┘
```

- `jade_native` statically links every upstream library. It exports only their public C APIs and
  our `jade_*` shims ([ADR-0003](adr/0003-single-combined-native-library.md)).
- `Jade.Interop` is the C# view of those exports: public, generated, allocation-free
  ([ADR-0005](adr/0005-in-house-binding-generator.md), [ADR-0006](adr/0006-binding-api-shape.md)).
- `Jade` is the engine. It is out of scope until the interop layer is green on every RID.

## Packages

```text
Jade.nupkg                         ← the only package users reference
├─ lib/net10.0/Jade.dll
├─ lib/net10.0/Jade.Interop.dll    (project packed into Jade, not published separately)
├─ analyzers/dotnet/cs/...         (later: Jade.Analyzers, Jade.SourceGenerators)
└─ dependency: Jade.Native [same version]

Jade.Native.nupkg                  ← implementation detail, split further if it nears 250 MB
├─ runtimes/<rid>/native/(lib)jade_native.(dll|so|dylib|a)
├─ buildTransitive/Jade.Native.targets   (static-link wiring: browser-wasm, iOS)
└─ THIRD-PARTY-NOTICES.md
```

See [ADR-0002](adr/0002-package-layout.md). Every package ships the same MinVer version.

## Repository layout

```text
.github/workflows/      ci.yml, codeql.yml (002), native.yml (103), release pipeline (107)
design/                 this folder
native/
  xmake.lua             jade_native target and the `bundled` list
  packages/<l>/<name>/  local xmake package repository, our pinned definitions
  modules/stage.lua     called by each package's on_install: exports, headers, licenses, metadata
  rules/bundle.lua      whole-archive linking and export control per platform
  shims/                C shims for C++-only libraries and jade_* helpers
scripts/                entry point scripts/<name>.cs, helpers in scripts/<name>/ (ADR-0010)
  build-native.cs       RID → xmake mapping, stages outputs into artifacts/native/<rid>/
  smoke-native.cs       loads the staged library and checks versions and exports
  generate-bindings.cs  the binding generator (201)
  fetch-native.cs       downloads CI-built natives for managed-only work (103)
src/Jade/  src/Jade.Interop/  src/Jade.Native/
tests/                  Jade.Interop.Tests, package and AOT smoke tests
samples/                hello-triangle and later samples
artifacts/              gitignored outputs
  native/<rid>/         lib/, include/<lib>/, metadata/ (dawn.json, versions.json, licenses)
  packages/             local NuGet feed produced by pack
```

## Pipelines

### Native

```text
native/packages (pinned sources + sha256)
        │  xmake, per RID, on that RID's native OS (CI matrix, 103/104/105)
        ▼
artifacts/native/<rid>/lib/(lib)jade_native.*      + include/ + metadata/
        │  CI artifact, cached by hash of native/**, scripts/build-native.cs and scripts/build-native/**
        ▼
src/Jade.Native (pack) → Jade.Native.nupkg
```

A local developer builds only their host RID (`scripts/build-native.cs`). They can also download
CI artifacts for the other RIDs (`scripts/fetch-native.cs`).

### Bindings

```text
artifacts/native/<host-rid>/include/**  +  metadata/dawn.json   (staged by the native build)
        │  scripts/generate-bindings.cs  (libclang per target triple + dawn.json reader → model → emitter)
        ▼
src/Jade.Interop/Generated/<Lib>/*.g.cs   (committed; CI regenerates and fails on diff)
tests/Jade.Interop.Tests/Generated/<Lib>/  (generated layout tests: C# size and offsets vs C, per RID)
```

Reading inputs from the native build's staging area guarantees that bindings and binaries come
from the same upstream revision.

### Managed

`dotnet build` / `dotnet test` / `dotnet pack` over `Jade.slnx`, versioned by MinVer. The release
pipeline (107) assembles the CI native artifacts for every RID, packs `Jade.Native` and `Jade`, then
publishes.

## Target RIDs

[ADR-0007](adr/0007-target-platforms.md) holds the decision. Each row below is validated by the task
that builds it.

| RID | Build host (CI) | Toolchain | jade_native form | Task |
| --- | --- | --- | --- | --- |
| win-x64 | windows-latest | MSVC | `jade_native.dll` | 103 |
| win-arm64 | windows-11-arm | MSVC | `jade_native.dll` | 103 |
| linux-x64 | ubuntu-latest + old-glibc container | clang/gcc | `libjade_native.so` | 101, 103 |
| linux-arm64 | ubuntu-24.04-arm + old-glibc container | clang/gcc | `libjade_native.so` | 103 |
| osx-x64 | macOS runner | Xcode clang | `libjade_native.dylib` | 103 |
| osx-arm64 | macOS runner | Xcode clang | `libjade_native.dylib` | 103 |
| android-arm64 | ubuntu + NDK | NDK clang | `libjade_native.so` | 104 |
| android-x64 | ubuntu + NDK | NDK clang | `libjade_native.so` | 104 |
| ios-arm64 | macOS runner | Xcode clang | static or framework (104) | 104 |
| iossimulator-arm64 | macOS runner | Xcode clang | static or framework (104) | 104 |
| iossimulator-x64 | macOS runner | Xcode clang | static or framework (104) | 104 |
| browser-wasm | ubuntu + emsdk (version bundled by .NET) | emcc | `jade_native.a` + JS libraries | 105 |

xmake has these platforms (checked with `xmake f --help`, v3.1.1): `windows` (x64, arm64), `linux`
(x86_64, arm64), `macosx` (x86_64, arm64), `android` (arm64-v8a, x86_64), `iphoneos` (arm64,
x86_64) and `wasm` (wasm32). The iOS simulator selection could only be checked on macOS (task 104).

## Upstream snapshot (2026-10-02)

These are snapshots for orientation. Tasks re-check them before pinning.

| Library | Latest seen | Source |
| --- | --- | --- |
| Dawn | `v20260930.214659` (daily tags) | github.com/google/dawn |
| SDL3 | `release-3.4.16` | github.com/libsdl-org/SDL |
| miniaudio | `0.11.25` | github.com/mackron/miniaudio |
| Dear ImGui | `v1.92.9b-docking` (ADR-0014) | github.com/ocornut/imgui |
| dear_bindings | `v0.24` release for that tag | github.com/dearimgui/dear_bindings |
| Box2D | `v3.1.1` | github.com/erincatto/box2d |
| Box3D | `v0.1.0` (first release, 2026-06-30, MIT) | github.com/erincatto/box3d |
| HarfBuzz | `14.5.1` | github.com/harfbuzz/harfbuzz |
| msdfgen / msdf-atlas-gen | `v1.13` / `v1.4` | github.com/Chlumsky |
| ClangSharp (generator) | `21.1.8.4` | nuget.org |
| xmake | `v3.1.1` | xmake.io |

<!-- CI turnaround measurement for task 004, not for merge. -->
