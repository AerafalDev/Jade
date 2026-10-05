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
| GPU | WebGPU through Dawn at one pinned commit; Emdawnwebgpu from the same commit; projects keep the "Wgpu" name | [0004](docs/adr/0004-webgpu-via-dawn.md) |
| Platform | SDL3; its audio subsystem is never initialized | [0005](docs/adr/0005-sdl3-platform-layer.md) |
| Audio | miniaudio; platform-dependent structs are opaque and allocated by a C shim | [0006](docs/adr/0006-miniaudio-for-audio.md) |
| Generator | In-house file-based app; `dawn.json` and libclang (ClangSharp, parser only); output committed and checked by CI | [0007](docs/adr/0007-in-house-binding-generator.md) |
| Interop layers | Raw blittable layer plus idiomatic layer | [0008](docs/adr/0008-two-layer-interop.md) |
| Interop mapping | `LibraryImport`, `CLong`/`nuint`, `InlineArray`, unions at offset 0, function-pointer callbacks, handles, descriptors, layout tests | [0009](docs/adr/0009-interop-mapping-conventions.md) |
| Natives | Built by us with xmake (CMake for Dawn and SDL3); `native/versions.json` is the single source of versions | [0010](docs/adr/0010-native-builds-with-xmake.md) |
| Native packages | `runtimes/{rid}/native`; `buildTransitive/` for iOS and the browser | [0011](docs/adr/0011-native-package-layout.md) |
| Targets | 12 RIDs, universal macOS and iOS simulator binaries, old glibc; adding a RID needs an ADR | [0012](docs/adr/0012-supported-targets.md) |
| Browser | Natives built with the workload's exact Emscripten version; no `--use-port`; non-blocking main loop | [0013](docs/adr/0013-browser-native-toolchain.md) |
| Repository | Layout, file-based scripts, `Generated/*.g.cs`, English only, no comments in MSBuild files | [0014](docs/adr/0014-repository-layout-and-conventions.md) |
| Build | Analysis, AOT compatibility, central packages with lock files, package validation, public API tracking, Microsoft.Testing.Platform | [0015](docs/adr/0015-build-and-packaging-conventions.md) |
| Public API | `params ReadOnlySpan<T>`, UTF-8 plus `string` overloads, extension members, platform attributes, feature switches | [0016](docs/adr/0016-public-api-conventions.md) |
| GitHub | Squash only, ruleset on `main`, secret scanning, Dependabot, CodeQL, Scorecard, SHA-pinned actions | [0017](docs/adr/0017-github-repository-baseline.md) |

Open decisions and the order of the next tasks are in the [roadmap](docs/roadmap.md).

## Conventions

- MSBuild files (`.csproj`, `.props`, `.targets`) and `Jade.slnx` contain no comments.
- Repository scripts are .NET file-based apps in `scripts/` and start with a `#!` line.
- Generated code goes to `Generated/*.g.cs` in each interop project and is never edited by hand.
- Interop rules: [0009](docs/adr/0009-interop-mapping-conventions.md). Public API rules:
  [0016](docs/adr/0016-public-api-conventions.md). Build rules:
  [0015](docs/adr/0015-build-and-packaging-conventions.md).
- Public texts (repository description, README introduction, NuGet descriptions and tags) never
  name Dawn, WebGPU, SDL3 or miniaudio; only `CONTRIBUTING.md`, `docs/` and
  `THIRD-PARTY-NOTICES.md` do.

## Verified toolchain facts

Re-check these at every SDK or dependency update.

| Fact | How it was verified | Date |
| --- | --- | --- |
| The `wasm-tools` workload of SDK `11.0.100-rc.1.26425.128` uses Emscripten 6.0.2 | `microsoft.net.workload.emscripten.current` manifest references `Microsoft.NET.Runtime.Emscripten.6.0.2.*` packs | 2026-10-05 |
| Default `LangVersion` for `net11.0` is 15.0 | `Roslyn/Microsoft.CSharp.Core.targets` of SDK `11.0.100-rc.1.26425.128` | 2026-10-05 |
| File-based apps support `#:include` without preview flags | ran a two-file app with SDK `11.0.100-rc.1.26425.128` | 2026-10-05 |
| CA2266 warns when a file-based entry point does not start with `#!` | same run | 2026-10-05 |

## GitHub repository state

Applied on 2026-10-05 (first phase of [0017](docs/adr/0017-github-repository-baseline.md)):

- Public repository `AerafalDev/Jade`, default branch `main`, description and topics set, no
  homepage yet.
- Squash merge only (subject: PR title, body: blank), "update branch" enabled, head branches
  deleted after merge, auto-merge disabled; wiki, projects and discussions disabled.
- Labels synchronized once from `.github/labels.yml` with `gh`.
- Private vulnerability reporting, Dependabot alerts and security updates, secret scanning and
  push protection enabled.
- Actions: default `GITHUB_TOKEN` read-only and unable to approve pull requests; full-SHA pinning
  required.
- Ruleset `main` on the default branch, no bypass: pull request required (0 approvals, squash
  only), no deletion, no force push, linear history.

Pending, for the CI setup task (roadmap task 3): `dependabot.yml`, CodeQL, required checks in the
ruleset, OpenSSF Scorecard, attestations, label synchronization workflow, labeler, CI/CodeQL/
Scorecard badges. The social preview image (`docs/assets/social-preview.jpg`) is uploaded by hand
in the repository settings; the REST API has no endpoint for it.

## Commands

No project exists yet; build and test commands are added with the solution scaffolding (roadmap
task 2).
