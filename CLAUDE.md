# Jade

Jade is a 2D game engine for .NET targeting desktop (Windows, Linux, macOS), mobile (Android, iOS)
and the browser (WebAssembly). The current milestone is the interop foundation: an in-house binding
generator for WebGPU (Dawn), SDL3 and miniaudio, producing an idiomatic and fast C# API, with the
native libraries built and packaged for every target. The 2D renderer, game loop, scene model and
tools are out of scope for now.

## Start of every session

1. Read this file, [`docs/architecture.md`](docs/architecture.md), [`docs/roadmap.md`](docs/roadmap.md)
   and the ADRs relevant to the task ([index](docs/adr/README.md)).
2. Inspect the actual state of the repository (`git log`, `git status`, the files involved). Never
   assume anything from a previous session: the repository is the only memory.
3. Work on exactly one roadmap task. Never start the next task in the same session.

## Role and working rules

- Act as lead developer and architect: responsible for the overall architecture and consistency of
  the project, not only for producing code.
- Speak French with the maintainer. Everything in the repository is in English: code, identifiers,
  comments, XML docs, commits, pull requests, issues, documentation.
- Local, reversible decisions: make them and mention them. Important or hard-to-reverse decisions:
  analyse, research, compare the options, give a recommendation and let the maintainer decide.
  Every validated important decision gets an ADR (see [0001](docs/adr/0001-record-architecture-decisions.md)).
- Say so when an idea is bad, premature or creates technical debt. Do not ask unnecessary
  questions. No off-topic refactoring.
- Prefer simplicity, performance, maintainability, testability and architectural consistency.
- Facts about `webgpu.h`, `dawn.json`, SDL3, miniaudio or the .NET WebAssembly toolchain are checked
  in the pinned sources, never assumed from memory. Package versions come from `dotnet add package`
  or NuGet tooling, never from memory.
- A task is done when it is actually validated: code, required tests, documentation, and benchmarks
  or specific checks when relevant, with build and tests green. Quote the command and its result.
  Anything that cannot be verified locally (another OS, a device, CI) is reported as not verified.
- Code comments explain why (invariants, constraints, pitfalls), never what the code does.

## End of every session

1. Update `CLAUDE.md`, `docs/architecture.md`, `docs/roadmap.md` and the relevant ADRs.
2. Report to the maintainer: what was done, decisions taken, remaining problems and risks, project
   state, next recommended task, and a self-contained prompt for the next session (role, task and
   scope, files to read first, verifiable completion criteria; it assumes no context from the
   current session).

## Git and GitHub

- Repository: <https://github.com/AerafalDev/Jade>. `main` is protected by a ruleset: every change
  goes through a pull request, squash-merged; the pull request title becomes the commit subject.
- Work on a branch named `<type>/<short-slug>`. Commit once the work is verified, using
  Conventional Commits (`feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `build`, `ci`, `chore`,
  `style`).
- Push, open or merge pull requests, and create tags or releases only when the maintainer asks.
- Repository settings live outside git. They are changed with `gh` and recorded in
  [0017](docs/adr/0017-github-repository-baseline.md) and in the section below.

## Decisions in force

| Area | Summary | ADR |
| --- | --- | --- |
| Process | One ADR per important decision; accepted ADRs are superseded, not rewritten | [0001](docs/adr/0001-record-architecture-decisions.md) |
| Identity | MIT; public texts are generic and never name the underlying libraries | [0002](docs/adr/0002-license-and-public-identity.md) |
| Language and runtime | C# 15, .NET 11, SDK pinned in `global.json`, no preview features; Roslyn components on `netstandard2.0` | [0003](docs/adr/0003-csharp-15-and-dotnet-11.md) |
| Targeting | `Jade.Emscripten` is `net11.0` without RID, browser-only through `[SupportedOSPlatform("browser")]`; Roslyn components set `LangVersion` 15.0 and reference a Roslyn no newer than the SDK's compiler | [0019](docs/adr/0019-browser-and-roslyn-component-targeting.md) |
| GPU | WebGPU through Dawn at one pinned commit; Emdawnwebgpu from the same commit; projects keep the "Wgpu" name | [0004](docs/adr/0004-webgpu-via-dawn.md) |
| Platform | SDL3; its audio subsystem is never initialized | [0005](docs/adr/0005-sdl3-platform-layer.md) |
| Audio | miniaudio; platform-dependent structs are opaque and allocated by a C shim | [0006](docs/adr/0006-miniaudio-for-audio.md) |
| Generator | In-house file-based app; `dawn.json` and libclang (ClangSharp, parser only); output committed and checked by CI | [0007](docs/adr/0007-in-house-binding-generator.md) |
| Interop layers | Raw blittable layer plus idiomatic layer | [0008](docs/adr/0008-two-layer-interop.md) |
| Interop mapping | `LibraryImport`, `CLong`/`nuint`, `InlineArray`, unions at offset 0, function-pointer callbacks, handles, descriptors, layout tests | [0009](docs/adr/0009-interop-mapping-conventions.md) |
| Natives | Built by us with xmake (CMake for Dawn and SDL3); `build/versions.json` is the single source of versions | [0010](docs/adr/0010-native-builds-with-xmake.md), [0020](docs/adr/0020-repository-layout-and-conventions.md) |
| Native packages | `runtimes/{rid}/native`; `buildTransitive/` for iOS and the browser | [0011](docs/adr/0011-native-package-layout.md) |
| Targets | 12 RIDs, universal macOS and iOS simulator binaries, old glibc; adding a RID needs an ADR | [0012](docs/adr/0012-supported-targets.md) |
| Browser | Natives built with the workload's exact Emscripten version; no `--use-port`; non-blocking main loop | [0013](docs/adr/0013-browser-native-toolchain.md) |
| Repository | Layout (`src/`, `native/` packaging projects, `build/` native builds), file-based scripts inheriting the MSBuild settings, `Generated/*.g.cs`, English only, no comments in MSBuild files | [0020](docs/adr/0020-repository-layout-and-conventions.md) |
| Build | Analysis, AOT compatibility, central packages without lock files, exact SDK pin, package validation, public API tracking, Microsoft.Testing.Platform | [0021](docs/adr/0021-build-and-packaging-conventions.md) |
| Tests | MSTest on Microsoft.Testing.Platform, plain packages under central management; `internal sealed` test classes with `DiscoverInternals` | [0018](docs/adr/0018-test-framework.md) |
| Public API | `params ReadOnlySpan<T>`, UTF-8 plus `string` overloads, extension members, platform attributes, feature switches | [0016](docs/adr/0016-public-api-conventions.md) |
| GitHub | Squash only, ruleset on `main`, secret scanning, Dependabot, CodeQL, Scorecard, SHA-pinned actions | [0017](docs/adr/0017-github-repository-baseline.md) |
| CI | GitHub-hosted standard runners with pinned images (`ubuntu-24.04`, `windows-2025`, `macos-26`; `ubuntu-slim` for API-only jobs), check names without runner labels, no cache | [0022](docs/adr/0022-ci-runners-and-caching.md) |

Open decisions and the order of the next tasks are in the [roadmap](docs/roadmap.md).

## Conventions

- MSBuild files (`.csproj`, `.props`, `.targets`) and `Jade.slnx` contain no comments.
- Repository scripts are .NET file-based apps in `scripts/` and start with a `#!` line.
- Generated code goes to `Generated/*.g.cs` in each interop project and is never edited by hand.
- Assembly- and module-level attributes go in `Properties/AssemblyInfo.cs`, never in
  `AssemblyAttribute` items.
- Interop rules: [0009](docs/adr/0009-interop-mapping-conventions.md). Public API rules:
  [0016](docs/adr/0016-public-api-conventions.md). Build rules:
  [0021](docs/adr/0021-build-and-packaging-conventions.md).
- Public texts (repository description, README introduction, NuGet descriptions and tags) never
  name Dawn, WebGPU, SDL3 or miniaudio; only `CONTRIBUTING.md`, `docs/` and
  `THIRD-PARTY-NOTICES.md` do.
- Workflows set `permissions: {}` at the top and explicit permissions per job, check out with
  `persist-credentials: false`, and pin every action to a full commit SHA followed by its version
  in a comment (`# v7.0.1`), taken from the action's release, never from memory. Renaming a
  required job means updating the ruleset in the same pull request.

## Verified toolchain facts

Re-check these at every SDK or dependency update.

| Fact | How it was verified | Date |
| --- | --- | --- |
| The `wasm-tools` workload of SDK `11.0.100-rc.1.26425.128` uses Emscripten 6.0.2 | `microsoft.net.workload.emscripten.current` manifest references `Microsoft.NET.Runtime.Emscripten.6.0.2.*` packs | 2026-10-05 |
| Default `LangVersion` for `net11.0` is 15.0 | `Roslyn/Microsoft.CSharp.Core.targets` of SDK `11.0.100-rc.1.26425.128` | 2026-10-05 |
| File-based apps support `#:include` without preview flags | ran a two-file app with SDK `11.0.100-rc.1.26425.128` | 2026-10-05 |
| CA2266 warns when a file-based entry point does not start with `#!` | same run | 2026-10-05 |
| File-based apps in `scripts/` import the root `Directory.Build.props`, `Directory.Build.targets` and `Directory.Packages.props`, build into `artifacts/`, set `FileBasedProgram=true`, `MSBuildProjectName=<file>.cs` and `PublishAot=true`; `#:property` works | `dotnet build probe.cs -getProperty:...` on a throwaway script | 2026-10-05 |
| Under central package management, `#:package Name@Version` fails with NU1008; `#:package Name` takes the version from `Directory.Packages.props` | same throwaway script | 2026-10-05 |
| `net11.0-browser` is a valid TFM (declared by the `microsoft.net.workload.mono.toolchain.current` manifest); the `wasmbrowser` template targets `net11.0` with an implicit `browser-wasm` RID; `net11.0` cannot reference `net11.0-browser` (NU1201) | manifest `WorkloadManifest.targets`, `dotnet new wasmbrowser`, scratch projects | 2026-10-05 |
| `netstandard2.0` defaults to `LangVersion` 7.3, and `Nullable=enable` then fails with CS8630 | `dotnet msbuild -getProperty:LangVersion` and a build of `Jade.Analyzers` | 2026-10-05 |
| The SDK's compiler is Roslyn `5.11.0-1.26425.128`; Roslyn component packages must not be newer | `csc.dll -version` of SDK `11.0.100-rc.1.26425.128` | 2026-10-05 |
| `dotnet sln add` fails with "already contains a project" when a referenced project is already in the solution; pass `--include-references false` | adding `src/Jade/Jade.csproj` to `Jade.slnx` | 2026-10-05 |
| `global.json` `"test": {"runner": "Microsoft.Testing.Platform"}` makes `dotnet test` run MTP; the MSTest packages import `Microsoft.VisualStudio.TestTools.UnitTesting` globally under `ImplicitUsings` | `dotnet test -c Release` on `tests/Jade.Tests` (6 passed) and IDE0005 on an explicit `using` | 2026-10-05 |
| Microsoft.CodeAnalysis.PublicApiAnalyzers adds `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` as `AdditionalFiles` itself | its `buildTransitive` targets (5.6.0) | 2026-10-05 |
| No MSBuild property exists for `SkipLocalsInit`; it is `[module: SkipLocalsInit]` | search of the SDK's `.props`/`.targets` | 2026-10-05 |
| A packaging project without build output fails to pack a symbol package (NU5017) | `dotnet pack` of `Jade.Native.*` with `IncludeSymbols` | 2026-10-05 |
| xmake 3.1.1 writes `.xmake/` and `build/` next to `xmake.lua` (so `build/.xmake/` and `build/build/` here) | throwaway xmake project | 2026-10-05 |
| The CI runner images (`ubuntu-24.04`, `windows-2025`, `macos-26`) preinstall .NET SDKs up to 10.0 only; `actions/setup-dotnet` 6.0.0 installs the exact `global.json` version for prerelease SDKs, and its `cache` input requires `packages.lock.json` | `actions/runner-images` READMEs; `setup-dotnet` README at `v6.0.0` | 2026-10-05 |
| `dotnet format --verify-no-changes` run from the root finds `Jade.slnx` and exits with code 2 on a whitespace violation; `dotnet format --help` without a workspace fails in RC 1 (missing `dotnet-format.dll` path) | injected violation in a test file | 2026-10-05 |
| CodeQL officially supports C# up to 14 and .NET up to 10, so C# 15 code is analyzed outside its supported range | `docs/codeql/reusables/supported-versions-compilers.rst` in `github/codeql` | 2026-10-05 |
| OpenSSF Scorecard's Pinned-Dependencies check counts `dotnet restore` without `--locked-mode` as an unpinned dependency | `checks/raw/shell_download_validate.go` in `ossf/scorecard` | 2026-10-05 |
| Dependabot's NuGet updater supports `.slnx` and central package management, and installs the `global.json` SDK with `dotnet-install --version` | `nuget/` in `dependabot/dependabot-core` | 2026-10-05 |
| The ruleset's extra approval for unattributed pull requests only applies to pull requests Copilot opens under its own identity and has no effect with zero required approvals | GitHub docs, "Available rules for rulesets" | 2026-10-05 |

## GitHub repository state

Applied on 2026-10-05 (both phases of [0017](docs/adr/0017-github-repository-baseline.md), which
records every settings change):

- Public repository `AerafalDev/Jade`, default branch `main`, description and topics set, no
  homepage yet.
- Squash merge only (subject: PR title, body: blank), "update branch" enabled, head branches
  deleted after merge, auto-merge disabled; wiki, projects and discussions disabled.
- Labels synchronized from `.github/labels.yml` by the `labels.yml` workflow on every change to
  `main` (dry run on pull requests).
- Private vulnerability reporting, Dependabot alerts and security updates, secret scanning and
  push protection enabled; Dependabot version updates from `.github/dependabot.yml`.
- Actions: default `GITHUB_TOKEN` read-only and unable to approve pull requests; full-SHA pinning
  required; all actions allowed.
- Ruleset `main` (id `24485772`) on the default branch, no bypass: pull request required
  (0 approvals, squash only), no deletion, no force push, linear history, required status checks
  `format`, `build (linux)`, `build (windows)`, `build (macos)`, `analyze (csharp)` and
  `analyze (actions)` from GitHub Actions (app id `15368`), branch up to date with `main`.
- The ruleset API turned on `require_extra_approval_for_unattributed_changes` by default: a pull
  request opened by Copilot under its own identity needs one extra approval from someone with
  write access. It stays enabled; with zero required approvals it has no effect, Dependabot pull
  requests included. Declare it explicitly in every ruleset update, since an omitted value is
  reset to `true`. A ruleset update is a `PUT` of the whole ruleset with `gh api`.

Pending: provenance attestations (tasks 10 and 19) and CodeQL for C/C++ (task 10). The social
preview image (`docs/assets/social-preview.jpg`) is uploaded by hand in the repository settings;
the REST API has no endpoint for it.

## Commands

Run from the repository root; `global.json` selects the SDK and the test runner.

| Purpose | Command |
| --- | --- |
| Restore | `dotnet restore` |
| Build as CI does | `dotnet build -c Release -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true` |
| Test (MSTest on MTP) | `dotnet test -c Release` |
| Check formatting | `dotnet format --verify-no-changes` |
| Pack (with package validation) | `dotnet pack -c Release -p:TreatWarningsAsErrors=true -p:ContinuousIntegrationBuild=true` |
| Run a script | `dotnet run scripts/<name>.cs` |

- Outputs go to `artifacts/` (`bin/`, `obj/`, `package/release/`, `test/`).
- SDK RC 1 bug: `dotnet test` with a relative project path can fail to load the project
  (dotnet/sdk#56196); pass an absolute path or run it from the root without a path.
- New projects go into `Jade.slnx` with `dotnet sln Jade.slnx add --include-references false
  <path>`.
- Package versions are added with `dotnet add <project> package <id> --version <version>` (the
  version checked on nuget.org first), then moved to the right file if the reference belongs in
  `Directory.Build.targets`.
