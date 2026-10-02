# Jade

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AerafalDev/Jade/blob/main/LICENSE)

Jade is a cross-platform game engine for **.NET 10**, in early development.

Work currently targets the **interop layer**: a single native library, `jade_native`, that bundles
[Dawn](https://github.com/google/dawn) (WebGPU), [SDL3](https://github.com/libsdl-org/SDL) and
[miniaudio](https://github.com/mackron/miniaudio), plus C# bindings generated over it. Nothing is usable
yet: the packages are empty shells and no release has been published.

## Packages

| Package | Contents |
| --- | --- |
| `Jade` | The only package to reference: the engine (later) and the `Jade.Interop` bindings. |
| `Jade.Native` | The `jade_native` binaries for every platform, pulled in by `Jade` (not built yet). |

## Building

You need the .NET 10 SDK; the minimum version is pinned in [`global.json`](https://github.com/AerafalDev/Jade/blob/main/global.json).

```sh
dotnet build -c Release
dotnet test -c Release
```

See [CONTRIBUTING.md](https://github.com/AerafalDev/Jade/blob/main/CONTRIBUTING.md) for more.

## License

[MIT](https://github.com/AerafalDev/Jade/blob/main/LICENSE) © Aerafal. Bundled third-party libraries keep
their own licenses, listed in
[THIRD-PARTY-NOTICES.md](https://github.com/AerafalDev/Jade/blob/main/THIRD-PARTY-NOTICES.md).
