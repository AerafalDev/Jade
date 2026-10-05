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
- No workload is needed for the managed build. The native toolchains (xmake, CMake, Emscripten,
  Android NDK) are only needed for native work and will be documented with it.

## Build and test

Run the commands from the repository root; `global.json` selects the SDK and the test runner.

| Purpose | Command |
| --- | --- |
| Restore | `dotnet restore` |
| Build as CI does | `dotnet build -c Release -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true` |
| Test | `dotnet test -c Release` |
| Check formatting | `dotnet format --verify-no-changes` |
| Pack, with package validation | `dotnet pack -c Release -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true` |

Outputs go to `artifacts/`. Tests use MSTest on Microsoft.Testing.Platform
([0018](docs/adr/0018-test-framework.md)).

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
- The repository layout is described in [0020](docs/adr/0020-repository-layout-and-conventions.md),
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
