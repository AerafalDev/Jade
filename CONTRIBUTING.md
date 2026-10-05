# Contributing to Jade

Thanks for your interest in Jade. The project is in early development: the current milestone is the
interop foundation, generated C# bindings for WebGPU (through Dawn), SDL3 and miniaudio, with the
native libraries built and packaged for every target. Read the [architecture](docs/architecture.md)
and the [roadmap](docs/roadmap.md) first, and open an issue before starting significant work so
that we can agree on the approach.

By taking part, you agree to follow the [code of conduct](CODE_OF_CONDUCT.md). Report security
issues as described in [SECURITY.md](SECURITY.md), never in a public issue.

## Prerequisites

- The .NET SDK version pinned in [`global.json`](global.json), exactly: `rollForward` is
  `disable`, so any other SDK version fails. The pin also fixes the browser workload's Emscripten
  version and the analyzer set ([0021](docs/adr/0021-build-and-packaging-conventions.md)).
- No workload is needed for the managed build. The native toolchains are only needed to build the
  native libraries (see [Native libraries](#native-libraries)); their versions are pinned in
  [`build/versions.json`](build/versions.json).
- The binding generator (`scripts/binding-generator.cs`) only needs the SDK, plus network access
  the first time it fetches a pinned source. Its libclang comes from a NuGet package that exists for
  Linux and Windows (x64 and arm64) and Apple silicon Macs, not for Intel Macs.

## Build and test

Run the commands from the repository root; `global.json` selects the SDK and the test runner.

| Purpose | Command |
| --- | --- |
| Restore | `dotnet restore` |
| Build as CI does | `dotnet build -c Release -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true` |
| Test | `dotnet test -c Release` |
| Check formatting | `dotnet format --verify-no-changes` |
| Pack, with package validation | `dotnet pack -c Release -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true` |
| Run the binding generator | `dotnet run scripts/binding-generator.cs` |

Outputs go to `artifacts/`. Tests use MSTest on Microsoft.Testing.Platform
([0018](docs/adr/0018-test-framework.md)).

## Native libraries

`dotnet run scripts/build-native.cs` builds Dawn, SDL3 and miniaudio for the machine it runs on into
`artifacts/native/bin/<rid>/`, from the sources pinned in `build/versions.json`
([0031](docs/adr/0031-native-build-definitions.md)). Only `linux-x64` is supported so far; the other
targets come with the native CI. The build needs, besides the SDK:

- xmake and CMake at exactly the versions of `build/versions.json`, which the script checks: the
  distribution's packages when they match, otherwise the release binaries
  `xmake-bundle-v<version>.linux.x86_64` (it needs `libncurses.so.6`, which Arch Linux does not
  have) and `cmake-<version>-linux-x86_64.tar.gz`. xmake refuses to run as root.
- git, Ninja, Python 3, and GCC or Clang with C++20 support (GCC 13.3 and 16.2 were tested).
- The development files of the system libraries that Dawn and SDL3 compile against. SDL3's build
  fails when one of the features Jade requires is missing, rather than leaving it out. On Ubuntu
  24.04:

  ```bash
  sudo apt-get install build-essential git python3 ninja-build pkg-config libncurses6 libx11-dev libx11-xcb-dev libxext-dev libxrandr-dev libxcursor-dev libxfixes-dev libxi-dev libxss-dev libxtst-dev libxkbcommon-dev libwayland-dev libdecor-0-dev libdrm-dev libgbm-dev libgl-dev libegl-dev libgles-dev libdbus-1-dev libibus-1.0-dev libudev-dev libusb-1.0-0-dev liburing-dev libfribidi-dev libthai-dev
  ```

  On Arch Linux, with its `xmake` and `cmake` packages:

  ```bash
  sudo pacman -S --needed base-devel git python ninja pkgconf xmake cmake libx11 libxext libxrandr libxcursor libxfixes libxi libxss libxtst libxkbcommon wayland libdecor libdrm mesa libglvnd dbus ibus systemd-libs libusb liburing fribidi libthai
  ```

  Both lists were checked in fresh `ubuntu:24.04` and `archlinux` containers.

The first build fetches about 1 GB of sources into `artifacts/native/sources/` and takes a few
minutes; later builds reuse them and only rebuild a library whose pinned commit or definition in
`build/` changed. The host build links against the host's C runtime and is meant for local work.

## Conventions

- Everything in the repository is written in English: code, comments, documentation, commits,
  pull requests and issues.
- The build runs every analyzer (`AnalysisLevel` `latest-all`) with nullable reference types, and
  CI treats warnings as errors.
- Comments explain why (invariants, constraints, pitfalls), not what the code does.
- Public API changes go into the project's `PublicAPI.Unshipped.txt`; the analyzers report any
  change that is missing from it.
- Generated code lives in `Generated/*.g.cs` and is never edited by hand: change the generator or
  its configuration and regenerate.
- MSBuild files and `Jade.slnx` contain no comments; assembly- and module-level attributes go in
  `Properties/AssemblyInfo.cs`.
- Package versions live only in `Directory.Packages.props` (central package management, exact
  versions). Dependabot proposes updates weekly.
- Repository scripts are .NET file-based apps in `scripts/`.
- The repository layout is described in [0023](docs/adr/0023-repository-layout-and-conventions.md),
  the interop rules in [0009](docs/adr/0009-interop-mapping-conventions.md) and the public API rules
  in [0016](docs/adr/0016-public-api-conventions.md).
- Important or hard-to-reverse decisions are recorded as decision records in
  [`docs/adr`](docs/adr/README.md), starting from the template.

## Pull requests

- Work on a branch named `<type>/<short-slug>`, for example `fix/surface-resume`.
- Commits and pull request titles follow [Conventional Commits](https://www.conventionalcommits.org/):
  `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `build`, `ci`, `chore` or `style`. Pull
  requests are squash-merged, and the title becomes the commit subject on `main`.
- Keep a pull request to one change, and say how you tested it, including what you could not
  verify (another OS, a device, the browser).
- `main` only accepts pull requests. Before merging, the branch must be up to date with `main` and
  these checks must pass: `format`, `build (linux)`, `build (windows)`, `build (macos)`,
  `analyze (csharp)` and `analyze (actions)`.
- Labels are applied from the paths a pull request changes; the label set is defined in
  [`.github/labels.yml`](.github/labels.yml).

## License

Jade is licensed under the [MIT license](LICENSE). By contributing, you agree that your
contributions are licensed under the same terms.
