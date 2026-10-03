# Changelog

All notable changes to Jade are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html), taken from `vX.Y.Z` tags by MinVer.
Until 1.0.0, any release may break the public API.

## [Unreleased]

### Added

- `jade_native`, one native library per RID that bundles SDL 3.4.16, miniaudio 0.11.25 and Dawn
  `v20260930.214659`, for win-x64, win-arm64, linux-x64, linux-arm64, osx-x64 and osx-arm64. Linux
  builds need glibc 2.28 or later, macOS builds macOS 12.0 or later.
- Dawn backends: D3D12 on Windows, Vulkan on Linux (X11 and Wayland surfaces), Metal on macOS, and
  Dawn's Null backend everywhere. Shaders are WGSL only.
- `Jade.Interop.Sdl3`: generated bindings for the public SDL3 API (982 functions, 101 enums,
  89 structs and 27 handles), with instance methods on handles and span, `in`, `ref` and `out`
  overloads. SDL's audio, GPU and 2D renderer APIs are left out: miniaudio and Dawn cover them.
- The `Jade` and `Jade.Native` packages. `Jade` carries `Jade.Interop` and depends on `Jade.Native`
  at exactly its own version. `Jade.Native` carries `jade_native` under `runtimes/<rid>/native/` for
  the six desktop RIDs, and `THIRD-PARTY-NOTICES.md`. Applications run with `dotnet run` and publish
  with NativeAOT, which places `jade_native` next to the executable. Both packages can be packed
  locally; nothing is published on nuget.org yet.

[Unreleased]: https://github.com/AerafalDev/Jade/commits/main
