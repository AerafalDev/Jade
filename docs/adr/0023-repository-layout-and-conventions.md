# 0023. Repository layout and content conventions

- Status: Accepted
- Date: 2026-10-05

## Context

[0020](0020-repository-layout-and-conventions.md) put the generated interop projects
(`Jade.Wgpu`, `Jade.Sdl`, `Jade.MiniAudio`, `Jade.Emscripten`) in `src/`, next to the engine and the
Roslyn components. The maintainer chose to give them a top-level `interop/` directory, as `native/`
already does for the packaging projects: the interop projects are mostly generated code, written by
the binding generator rather than by hand, and they map one to one to the native packages. This
record restates the whole layout so that it can be read on its own, and supersedes 0020.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`: no MSBuild file depends on the location
of the interop projects (`Directory.Build.props` and `Directory.Build.targets` only use project
properties, and the package README is found from the root), so moving them only changes
`Jade.slnx`, the project references of `Jade` and the labeler paths. Build with warnings as errors,
tests, pack with package validation and the format check pass after the move.

## Decision

```text
.github/                     workflows, issue/PR templates, dependabot, CODEOWNERS, labeler, labels
build/                       versions.json, xmake package definitions, C shims
docs/
  architecture.md
  roadmap.md
  adr/                       one decision per file
  assets/                    social preview image, package icon
interop/                     generated interop projects
  Jade.Wgpu/  Jade.Sdl/  Jade.MiniAudio/  Jade.Emscripten/
native/                      packaging projects: runtimes/ + buildTransitive/
  Jade.Native.Wgpu/  Jade.Native.Sdl/  Jade.Native.MiniAudio/
scripts/                     .NET file-based apps
  binding-generator.cs
  binding-generator/         files included with #:include
  build-native.cs
  fetch-native.cs
src/
  README.md                  README shipped in every package
  Jade/
  Jade.SourceGenerators/
  Jade.Analyzers/
samples/                     Desktop, Android, iOS, Browser
tests/                       one test project per tested assembly
  Jade.Tests/
CLAUDE.md  README.md  LICENSE  CONTRIBUTING.md  CODE_OF_CONDUCT.md  SECURITY.md
CHANGELOG.md  THIRD-PARTY-NOTICES.md  .gitignore  .gitattributes
Jade.slnx  global.json  Directory.Build.props  Directory.Build.targets
Directory.Packages.props  .editorconfig
```

- The paths `native/versions.json` and `native/` (xmake package definitions and C shims) used in
  [0004](0004-webgpu-via-dawn.md), [0006](0006-miniaudio-for-audio.md),
  [0007](0007-in-house-binding-generator.md), [0010](0010-native-builds-with-xmake.md),
  [0012](0012-supported-targets.md) and [0013](0013-browser-native-toolchain.md) mean
  `build/versions.json` and `build/`. Those records are not rewritten
  ([0001](0001-record-architecture-decisions.md)).
- `Jade.slnx` has one solution folder per top-level directory that holds projects (`interop`,
  `native`, `src`, `tests`, later `samples`).
- Repository scripts are .NET file-based apps in `scripts/`. They inherit the repository's MSBuild
  and package settings: `#:package` directives carry no version.
- `Jade.SourceGenerators` and `Jade.Analyzers` exist from the start.
- In each interop project, generated code goes to `Generated/*.g.cs`, next to the hand-written
  partials and the generator configuration.
- `samples/` holds one sample per platform family (Desktop, Android, iOS, Browser). They validate
  the interop and the natives on each target.
- Everything in the repository is written in English: code, identifiers, comments, XML
  documentation, commit messages, pull requests, issues and documentation.
- MSBuild files (`.csproj`, `.props`, `.targets`) and `Jade.slnx` contain no comments.

## Consequences

- `src/` holds the hand-written engine and its Roslyn components; `interop/` holds the generated
  bindings and their hand-written idiomatic layers; `native/` ships the natives built from `build/`.
- The package README stays in `src/README.md` and is shipped by every package, interop packages
  included.
- Native build inputs (`build/`) and the packages that ship their outputs (`native/`) are separate:
  the first is read by `scripts/build-native.cs` and CI, the second only by `dotnet pack`.
  `.gitignore` ignores `.xmake/` and `/build/build/`, never a bare `build/` pattern, which would
  also hide the `build/` and `buildTransitive/` folders of the native packages.
- Repository tooling needs only the .NET SDK, plus the native toolchains for native work.
- Scripts are analysed like the libraries (`AnalysisLevel` `latest-all`, nullable, warnings as
  errors in CI) and are AOT-analysed because file-based apps default to `PublishAot`; a script
  whose dependencies are not AOT-compatible sets `#:property PublishAot=false`.
- Adding a package used by a script means adding its `PackageVersion` to
  `Directory.Packages.props`.
- Explanations that would otherwise sit in MSBuild comments belong in `docs/` or in the relevant
  ADR.
