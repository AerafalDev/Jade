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
| Generator pipeline | Inputs fetched at the pinned commits, `dawn.json` and clang front-ends, one intermediate representation with per-platform availability, annotations from `interop/<project>/bindings.json`, then projection and emitters; C headers parsed for the 12 RID triples with the ClangSharp package's libclang, `-nostdinc` and the generator's own C runtime headers; `MA_*` defines in `build/miniaudio/config.h` | [0026](docs/adr/0026-binding-generator-pipeline.md) |
| Mapping rules | .NET names without C prefixes, typedefs mapped by name, dedicated integer booleans, macros evaluated by clang, no variadic or inline functions, platform attributes from availability, XML comments on generated members | [0027](docs/adr/0027-interop-mapping-rules.md) |
| Raw layer | Internal with C names; types identical in both layers are public; handles expose their `nint` | [0028](docs/adr/0028-internal-raw-interop-layer.md) |
| Descriptors and chains | Value structures shared and pinned, `ref struct` descriptor mirrors, element mirrors with `ReadOnlyMemory<T>`, stack-based arena for nested data, `IChainedExtension<TSelf, TRoot>` generic overloads | [0029](docs/adr/0029-descriptors-and-chained-structs.md) |
| Natives | Built by us with xmake (CMake for Dawn and SDL3); `build/versions.json` is the single source of versions, each citing its source | [0010](docs/adr/0010-native-builds-with-xmake.md), [0023](docs/adr/0023-repository-layout-and-conventions.md) |
| Native build | `build/xmake.lua` with one definition per dependency; `scripts/build-native.cs` checks the pinned xmake and CMake, fetches sources with git at the pinned commits (Dawn's `DEPS` entries listed in `build/dawn/deps.json`), runs xmake isolated under `artifacts/native/`; SDL3 without audio and with a required feature list per platform; `MA_API` and Apple `MA_NO_RUNTIME_LINKING` only; upstream library names | [0031](docs/adr/0031-native-build-definitions.md) |
| D3D12 compilers | DXC built by Dawn and shipped as `dxcompiler.dll`; `d3dcompiler_47.dll` is the system's, never redistributed | [0030](docs/adr/0030-d3d12-shader-compilers.md) |
| Native packages | `runtimes/{rid}/native`; `buildTransitive/` for iOS and the browser | [0011](docs/adr/0011-native-package-layout.md) |
| Targets | 12 RIDs, universal macOS and iOS simulator binaries, old glibc; adding a RID needs an ADR | [0012](docs/adr/0012-supported-targets.md) |
| Minimum OS | Windows 10 1607, glibc 2.28, macOS 14.0, iOS 14.0, Android API 26, a browser with WebGPU; raising one supersedes the ADR | [0024](docs/adr/0024-minimum-os-versions.md) |
| Browser | Natives built with the `wasm-tools` workload's own Emscripten toolchain, no standalone emsdk; rebuilt at every SDK change; no `--use-port`; non-blocking main loop | [0025](docs/adr/0025-browser-natives-with-workload-emscripten.md) |
| Repository | Layout (`src/` engine and Roslyn components, `interop/` generated interop projects, `native/` packaging projects, `build/` native builds), file-based scripts inheriting the MSBuild settings, `Generated/*.g.cs`, English only, no comments in MSBuild files | [0023](docs/adr/0023-repository-layout-and-conventions.md) |
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
- The binding generator is `scripts/binding-generator.cs`; its code is in
  `scripts/binding-generator/`, one type per file in a folder per concern, each type and member
  with an XML comment. Each generated interop project has a `bindings.json`.
- The native build is `scripts/build-native.cs`, with its code in `scripts/build-native/` under the
  same rules. Each dependency's xmake definition is in `build/<dependency>/`; a change to it or to
  its pinned commit rebuilds that package.
- `build/versions.json`: every entry has a `source` saying where its value was verified, and
  dependencies are pinned to full commit hashes; `Jade.Tests` checks both. `THIRD-PARTY-NOTICES.md`
  follows it.
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
| The `wasm-tools` workload of SDK `11.0.100-rc.1.26425.128` ships packs named `Microsoft.NET.Runtime.Emscripten.6.0.2.*`, but their `emcc` reports 6.0.3: it is built from the `dotnet/emscripten` fork (upstream 6.0.2 plus about 40 commits toward 6.0.3) with clang `23.1.0-rc2` from `dotnet-llvm-project`; no upstream emsdk equals it | manifest; `emcc --version` and `clang --version` of the installed packs; `src/emsdk/eng/Version.Details.xml` of the VMR tag `v11.0.100-rc.1.26425.128`; GitHub compare with upstream tags | 2026-10-05 |
| The workload's `emcc` runs outside MSBuild with an `EM_CONFIG` pointing at the Sdk pack's `tools/bin` and the Node pack; the pack ships `emcmake` and `Emscripten.cmake` | ran `emcc --version` on Linux | 2026-10-05 |
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
| .NET 11 RC 1 build floors: Android API 24, iOS 13.0, macOS 14.0; `supported-os.md` lists Windows 10 1607 (E), glibc 2.27 (x64, arm64), and RHEL 8 and Ubuntu 22.04 as the oldest of their distributions | VMR `src/runtime/Directory.Build.props`, `dotnet/macios` `Make.config` (rc1 tag), the Android workload targets, `dotnet/core` `release-notes/11.0/supported-os.md` | 2026-10-05 |
| The .NET for Android workload `37.0.0-rc.1.2257` uses NDK r28c (`28.2.13676358`) | `Configuration.props` of `dotnet/android` at that tag | 2026-10-05 |
| Dawn publishes `vYYYYMMDD.HHMMSS` releases (binaries, Emdawnwebgpu, tested emsdk version) on the `google/dawn` mirror only; `dawn.googlesource.com` has no such tags | `gh api` releases; `git ls-remote` on both | 2026-10-05 |
| CodeQL officially supports C# up to 14 and .NET up to 10, so C# 15 code is analyzed outside its supported range | `docs/codeql/reusables/supported-versions-compilers.rst` in `github/codeql` | 2026-10-05 |
| OpenSSF Scorecard's Pinned-Dependencies check counts `dotnet restore` without `--locked-mode` as an unpinned dependency | `checks/raw/shell_download_validate.go` in `ossf/scorecard` | 2026-10-05 |
| Dependabot's NuGet updater supports `.slnx` and central package management, and installs the `global.json` SDK with `dotnet-install --version` | `nuget/` in `dependabot/dependabot-core` | 2026-10-05 |
| The ruleset's extra approval for unattributed pull requests only applies to pull requests Copilot opens under its own identity and has no effect with zero required approvals | GitHub docs, "Available rules for rulesets" | 2026-10-05 |
| ClangSharp 21.1.8.4 depends on `libClang` 21.1.8 and `libClangSharp` 21.1.8.2; `libClang` picks a runtime package through `runtime.json` for `linux-x64`, `linux-arm64`, `osx-arm64`, `win-x64` and `win-arm64` only, and that package holds `libclang.so` without clang's builtin headers | nuspecs and package content on nuget.org | 2026-10-05 |
| A file-based app restores RID-specific runtime packages only with a runtime identifier: `#:property UseCurrentRuntimeIdentifier=true` sets it to `NETCoreSdkPortableRuntimeIdentifier`, and `PublishAot` stays `true` | `Microsoft.NET.RuntimeIdentifierInference.targets`; `dotnet build scripts/binding-generator.cs -getProperty:RuntimeIdentifier` | 2026-10-05 |
| `#:include` accepts globs (`binding-generator/*.cs`), which do not match subfolders, and `AppContext.GetData("EntryPointFileDirectoryPath")` gives a file-based app its script directory | throwaway apps and `scripts/binding-generator.cs` | 2026-10-05 |
| `dotnet package add <id> --version <v> --file <app>.cs` adds `#:package <id>` and the `PackageVersion`, but rewrites `Directory.Packages.props` without its blank lines and final newline | adding ClangSharp to the generator | 2026-10-05 |
| `dotnet format` does not restore the `#:package` references of a file-based app (CS0246), so a script's formatting is checked by its build (`EnforceCodeStyleInBuild`, IDE0055) | `dotnet format scripts/binding-generator.cs --verify-no-changes` | 2026-10-05 |
| libclang 21.1.8 with `-nostdinc -ffreestanding` and the generator's headers parses SDL3 and miniaudio for the 12 RID triples without a diagnostic; it defines the `TARGET_OS_*` macros for Apple triples | `dotnet run scripts/binding-generator.cs` | 2026-10-05 |
| `ReadOnlySpan<T>` rejects a `ref struct` element type (CS9244; CS9358 for a collection expression); a public interface can declare an `internal static abstract` member that uses internal types | throwaway apps with SDK `11.0.100-rc.1.26425.128` | 2026-10-05 |
| PublicApiAnalyzers analyzes and reports on generated code, and tracks `[Experimental]` APIs | `DeclarePublicApiAnalyzer.cs` and `DeclarePublicApiAnalyzer.Impl.cs` in `dotnet/roslyn` main | 2026-10-05 |
| xmake 3.1.1: `add_requires` takes a system package first (`sdl` resolved to the system SDL 1.2 through pkg-config) unless `system = false`; an installed package is reused while its configs are unchanged, even when its sources or definition changed | `core/package/package.lua` in `/usr/share/xmake`; prototype builds of `build/` | 2026-10-05 |
| xmake 3.1.1 links shared libraries with the C++ driver (a C library then needs `libstdc++.so.6`), adds no optimization flag to a target without `set_optimize` or a mode rule, and `os.cp` to a missing directory writes a file of that name | `toolchains/gcc/xmake.lua`, `rules/mode/xmake.lua`; `readelf -d` of the outputs | 2026-10-05 |
| xmake refuses to run as root without `--root` or `XMAKE_ROOT=y`; `xmake-bundle-v3.1.1.linux.x86_64` needs `libncurses.so.6` | `core/main.lua`; `ubuntu:24.04` container | 2026-10-05 |
| Dawn reads its commit with `git rev-parse` for the key of its device cache (empty outside a git checkout); `DAWN_FETCH_DEPENDENCIES` clones 19 fixed `DEPS` entries and ignores git failures; its C++20 module check accepts GCC 13, which CMake cannot scan | `generator/dawn_version_generator.py`, `tools/fetch_dawn_dependencies.py`, `src/cmake/DawnCompilerChecks.cmake` at `b1236a9`; GCC 13.3 build | 2026-10-05 |
| SDL3's CMake build silently drops a feature whose development files are missing; with `SDL_AUDIO=OFF` it also skips the PipeWire check (no PipeWire camera) | `cmake/sdlchecks.cmake` and `CMakeLists.txt` at `release-3.4.18`; build without `ibus` headers | 2026-10-05 |
| DirectXShaderCompiler `9757d44` (Dawn's `DEPS`) signs DXIL with its open-source validator inside `dxcompiler`; Dawn never loads `dxil.dll` | `tools/clang/tools/dxcvalidator/dxcvalidator.cpp`; search of Dawn's `src/` | 2026-10-05 |

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
| Build the binding generator as CI will (not in `Jade.slnx`) | `dotnet build scripts/binding-generator.cs -c Release -p:TreatWarningsAsErrors=true` |
| Load the binding generator's pinned inputs | `dotnet run scripts/binding-generator.cs` |
| Build the native build script with warnings as errors (not in `Jade.slnx`) | `dotnet build scripts/build-native.cs -c Release -p:TreatWarningsAsErrors=true` |
| Build the natives for the host (`linux-x64` only; prerequisites in `CONTRIBUTING.md`) | `dotnet run scripts/build-native.cs` |

- Outputs go to `artifacts/` (`bin/`, `obj/`, `package/release/`, `test/`); the binding generator
  caches the pinned sources in `artifacts/binding-generator/sources/`. The native build writes to
  `artifacts/native/`: `sources/` (git checkouts), `xmake/` (xmake's global directory), `obj/<rid>/`
  and `bin/<rid>/` (the libraries).
- SDK RC 1 bug: `dotnet test` with a relative project path can fail to load the project
  (dotnet/sdk#56196); pass an absolute path or run it from the root without a path.
- New projects go into `Jade.slnx` with `dotnet sln Jade.slnx add --include-references false
  <path>`.
- Package versions are added with `dotnet add <project> package <id> --version <version>` (the
  version checked on nuget.org first), then moved to the right file if the reference belongs in
  `Directory.Build.targets`.
