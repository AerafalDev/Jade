# 0020. Repository layout and content conventions

- Status: Accepted
- Date: 2026-10-05

## Context

[0014](0014-repository-layout-and-conventions.md) put the xmake package definitions, the C shims
and `versions.json` in `native/`, and the `Jade.Native.*` packaging projects in `src/`. While
scaffolding the solution, the maintainer chose to group the three packaging projects in a
`native/` directory of their own, next to `src/`, and to move the native build definitions to
`build/`. This record restates the whole layout so that it can be read on its own, and supersedes
0014.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`, with a throwaway file-based app in
`scripts/`:

- File-based apps import the repository's `Directory.Build.props`, `Directory.Build.targets` and
  `Directory.Packages.props` (`DirectoryBuildPropsPath` and `DirectoryPackagesPropsPath` point to
  the root files), build into `artifacts/`, and set `FileBasedProgram` to `true` and
  `MSBuildProjectName` to the file name (`probe.cs`). `PublishAot` is `true` by default.
- With central package management, `#:package Name@Version` fails with NU1008; `#:package Name`
  takes its version from `Directory.Packages.props`.

Verified the same day with xmake 3.1.1: a project whose `xmake.lua` sits in `build/` writes its
cache to `build/.xmake/` and its outputs to `build/build/`.

## Decision

```text
.github/                     workflows, issue/PR templates, dependabot, CODEOWNERS, labeler, labels
build/                       versions.json, xmake package definitions, C shims
docs/
  architecture.md
  roadmap.md
  adr/                       one decision per file
  assets/                    social preview image, package icon
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
  Jade.Wgpu/  Jade.Sdl/  Jade.MiniAudio/  Jade.Emscripten/      generated interop
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
  [0012](0012-supported-targets.md) and [0013](0013-browser-native-toolchain.md) now mean
  `build/versions.json` and `build/`. Those records are not rewritten
  ([0001](0001-record-architecture-decisions.md)).
- `Jade.slnx` has one solution folder per top-level directory that holds projects (`native`,
  `src`, `tests`, later `samples`).
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
