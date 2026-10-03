# Jade

[![CI](https://github.com/AerafalDev/Jade/actions/workflows/ci.yml/badge.svg)](https://github.com/AerafalDev/Jade/actions/workflows/ci.yml)
[![Native](https://github.com/AerafalDev/Jade/actions/workflows/native.yml/badge.svg)](https://github.com/AerafalDev/Jade/actions/workflows/native.yml)
[![CodeQL](https://github.com/AerafalDev/Jade/actions/workflows/codeql.yml/badge.svg)](https://github.com/AerafalDev/Jade/actions/workflows/codeql.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-informational.svg)](https://github.com/AerafalDev/Jade/blob/main/design/adr/0007-target-platforms.md)
[![Status](https://img.shields.io/badge/status-early%20development-orange.svg)](https://github.com/AerafalDev/Jade/blob/main/design/roadmap.md)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AerafalDev/Jade/blob/main/LICENSE)

Jade is a cross-platform **2D** game engine for **.NET 10**, in early development.

The current work is the **interop layer**. A single native library, `jade_native`, bundles
[Dawn](https://github.com/google/dawn) (WebGPU), [SDL3](https://github.com/libsdl-org/SDL) and
[miniaudio](https://github.com/mackron/miniaudio). Public C# bindings are generated over it:
zero-cost, NativeAOT- and trim-compatible, with a .NET feel (methods on handles, span, `in`, `ref`
and `out` overloads). The engine comes on top of that layer once it works on every target.

Nothing is published on nuget.org yet.

## Status

| Area | State |
| --- | --- |
| `jade_native` with SDL3, miniaudio and Dawn | Built in CI for the six desktop RIDs |
| SDL3 bindings (`Jade.Interop.Sdl3`) | Complete public API, checked against the binary's exports |
| WebGPU bindings from Dawn's `dawn.json` (`Jade.Interop.WebGpu`) | Complete API with Dawn's extensions, checked against the header and the binary's exports |
| miniaudio bindings | Planned |
| Android, iOS, browser (WebAssembly) | Planned |
| Dear ImGui and extensions, Box2D, FreeType, HarfBuzz, msdfgen, asset libraries | Planned |
| `Jade` and `Jade.Native` packages | Packed and tested locally, with the JIT and NativeAOT |
| Publishing on nuget.org | Planned |

The [roadmap](https://github.com/AerafalDev/Jade/blob/main/design/roadmap.md) tracks every task,
and the [changelog](https://github.com/AerafalDev/Jade/blob/main/CHANGELOG.md) what has landed.

## Platforms

| RID | Minimum | Dawn backend |
| --- | --- | --- |
| win-x64, win-arm64 | Not pinned yet (D3D12 needs Windows 10) | D3D12 |
| linux-x64, linux-arm64 | glibc 2.28 | Vulkan (X11 and Wayland) |
| osx-x64, osx-arm64 | macOS 12.0 | Metal |
| android-arm64, android-x64 | Planned | Vulkan |
| ios-arm64, iossimulator-arm64, iossimulator-x64 | Planned | Metal |
| browser-wasm | Planned | The browser's WebGPU, through emdawnwebgpu |

Every built RID also gets Dawn's Null backend, which lets tests run without a GPU.

## Packages

| Package | Contents |
| --- | --- |
| `Jade` | The only package to reference: the engine (later) and the `Jade.Interop` bindings |
| `Jade.Native` | The `jade_native` binaries for every RID, pulled in by `Jade` at the same version |

## Building from source

The managed solution needs the .NET 10 SDK, whose minimum version is pinned in
[`global.json`](https://github.com/AerafalDev/Jade/blob/main/global.json):

```sh
dotnet build -c Release
dotnet test -c Release
```

Tests that call into `jade_native` need the native library staged under `artifacts/native/<rid>/`.
Download the one CI built for `main`, which needs an authenticated GitHub CLI (`gh`):

```sh
dotnet scripts/fetch-native.cs --rid linux-x64
```

Or build it yourself. That needs xmake 3.1.1, CMake, Ninja, clang, Python 3 and git, plus Docker for
the Linux baseline container:

```sh
dotnet scripts/build-native.cs                                # host RID
dotnet scripts/build-native.cs --rid linux-x64 --container    # glibc 2.28 build, as CI does
dotnet scripts/smoke-native.cs                                # check the staged library
```

The bindings are regenerated from the staged headers and Dawn's `dawn.json` with
`dotnet scripts/generate-bindings.cs`.

`dotnet pack -c Release -o artifacts/packages` packs `Jade` and `Jade.Native`, the latter with every
RID staged under `artifacts/native/`. `dotnet scripts/test-package.cs` packs them, then runs a
project that references `Jade` from that folder, with `dotnet run` and as a NativeAOT binary.

See [CONTRIBUTING.md](https://github.com/AerafalDev/Jade/blob/main/CONTRIBUTING.md) for conventions,
and [`design/`](https://github.com/AerafalDev/Jade/blob/main/design/architecture.md) for the
architecture and its decision records.

## License

[MIT](https://github.com/AerafalDev/Jade/blob/main/LICENSE) © Aerafal. Bundled third-party libraries
keep their own licenses, listed in
[THIRD-PARTY-NOTICES.md](https://github.com/AerafalDev/Jade/blob/main/THIRD-PARTY-NOTICES.md).
