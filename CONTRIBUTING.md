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
| Regenerate the bindings | `dotnet run scripts/binding-generator.cs` |
| Declare generated public APIs | `dotnet format analyzers interop/<project>/<project>.csproj --diagnostics RS0016 --severity info --include-generated` |

Outputs go to `artifacts/`. Tests use MSTest on Microsoft.Testing.Platform
([0018](docs/adr/0018-test-framework.md)). The export and smoke tests of the interop projects
(`tests/Jade.Wgpu.Tests`, `tests/Jade.Sdl.Tests`, `tests/Jade.MiniAudio.Tests`) call the native
libraries of `artifacts/native/bin/<rid>/`, and their generated layout tests the layout libraries
of `artifacts/native/test/<rid>/` ([0036](docs/adr/0036-generated-layout-tests.md)), all built as
described below; without them they are reported as skipped. Rebuild the natives after changing
`build/` or regenerating: the export tests fail on a stale library, and the layout tests on a stale
layout library.

## Native libraries

The tests use the native libraries of `artifacts/native/bin/<rid>/` and the layout libraries of
`artifacts/native/test/<rid>/` when they exist, and skip the native tests otherwise. There are two
ways to get them.

### Fetching the CI's natives

`dotnet run scripts/fetch-native.cs` downloads the libraries that the native workflow built and
attested for the machine it runs on, from the newest run of `main` that has them and whose native
build inputs (`build/`, the native scripts, `global.json`, the workflow) match the checkout, and
checks every file with `gh attestation verify` before installing it
([0038](docs/adr/0038-native-ci.md)). It needs the GitHub CLI, signed in (`gh auth login`), since
downloading an artifact requires authentication. `--rid <rid>`, repeatable, selects other runtime
identifiers; `--run <id>` takes the artifacts of a given run, such as a pull request's, under the
same condition on the inputs. A branch that changes the inputs builds its natives locally or through
its pull request's run.

### Building them

`dotnet run scripts/build-native.cs` builds Dawn, SDL3 and miniaudio into
`artifacts/native/bin/<rid>/`, from the sources pinned in `build/versions.json`
([0031](docs/adr/0031-native-build-definitions.md)), and the layout libraries of the tests into
`artifacts/native/test/<rid>/`. It builds the host's runtime identifier, or the one of
`--rid <rid>`: Linux and Windows on their own platform and architecture, the Apple targets on
macOS, Android with the NDK of `build/versions.json` (found through `ANDROID_HOME` or
`ANDROID_NDK_ROOT`), and the browser with the `wasm-tools` workload
(`dotnet workload install wasm-tools`). `--install-tools` installs the pinned xmake and CMake
into `artifacts/native/tools/` first, as CI does. The build needs, besides the SDK:

- xmake and CMake at exactly the versions of `build/versions.json`, which the script checks:
  `--install-tools`, or the distribution's packages when they match. On Linux and macOS
  `--install-tools` compiles xmake from its sources, which needs `make` and a C compiler. xmake
  refuses to run as root.
- git, Ninja, Python 3 (`python` on Windows), and GCC, Clang or MSVC with C++20 support (GCC 13.3,
  14.2 and 16.2 were tested on Linux).
- On Linux, the development files of the system libraries that Dawn and SDL3 compile against.
  SDL3's build fails when one of the features Jade requires is missing, rather than leaving it
  out. On Ubuntu 24.04:

  ```bash
  sudo apt-get install build-essential git python3 ninja-build pkg-config libx11-dev libx11-xcb-dev libxext-dev libxrandr-dev libxcursor-dev libxfixes-dev libxi-dev libxss-dev libxtst-dev libxkbcommon-dev libwayland-dev libdecor-0-dev libdrm-dev libgbm-dev libgl-dev libegl-dev libgles-dev libdbus-1-dev libibus-1.0-dev libudev-dev libusb-1.0-0-dev liburing-dev libfribidi-dev libthai-dev
  ```

  On Arch Linux, with its `xmake` and `cmake` packages:

  ```bash
  sudo pacman -S --needed base-devel git python ninja pkgconf xmake cmake libx11 libxext libxrandr libxcursor libxfixes libxi libxss libxtst libxkbcommon wayland libdecor libdrm mesa libglvnd dbus ibus systemd-libs libusb liburing fribidi libthai
  ```

  Both lists were checked in fresh `ubuntu:24.04` and `archlinux` containers.

The first build fetches about 1 GB of sources into `artifacts/native/sources/` and takes a few
minutes; later builds reuse them and only rebuild a library whose pinned commit or definition in
`build/` changed. A host build links against the host's C runtime and is meant for local work. The
shipped Linux libraries come from the glibc 2.28 environment of `build/linux/Dockerfile`, which
CI runs as the runner's user with the runner's SDK mounted. The same build runs locally, in a
checkout of its own since the container's paths under `artifacts/` differ from the host's, with:

```bash
docker build --tag jade-native-linux build/linux
```

```bash
docker run --rm --init --user "$(id -u):$(id -g)" --env HOME=/tmp --env DOTNET_ROOT=/opt/dotnet --volume "$(dirname "$(readlink -f "$(command -v dotnet)")"):/opt/dotnet:ro" --volume "$PWD:/work" --workdir /work jade-native-linux bash -c 'PATH=/opt/dotnet:$PATH dotnet run scripts/build-native.cs --install-tools'
```

## Conventions

- Everything in the repository is written in English: code, comments, documentation, commits,
  pull requests and issues.
- The build runs every analyzer (`AnalysisLevel` `latest-all`) with nullable reference types, and
  CI treats warnings as errors.
- Comments explain why (invariants, constraints, pitfalls), not what the code does.
- Public API changes go into the project's `PublicAPI.Unshipped.txt`; the analyzers report any
  change that is missing from it.
- Generated code lives in `Generated/*.g.cs` of the interop and test projects, and in
  `build/layout/*.g.c`, and is never edited by hand: change the generator or its configuration
  (`bindings.json`) and regenerate. CI regenerates the bindings and the layout tests and fails on
  any difference. When the public declarations change, run the command above to declare them, and
  remove the lines the build reports as RS0017
  ([0032](docs/adr/0032-webgpu-raw-layer-generation.md)).
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
