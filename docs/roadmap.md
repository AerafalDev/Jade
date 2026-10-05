# Roadmap

Ordered list of the next tasks. Each task is done in its own work session and ends with
`CLAUDE.md`, `docs/architecture.md`, this file and the relevant ADRs up to date. A task can start
once all the tasks it depends on are done; tasks without a mutual dependency can run in any order.

Status: `done`, `next`, `planned`.

## Milestone 1: interop foundation

| # | Task | Depends on | Status |
| --- | --- | --- | --- |
| 1 | Project charter and GitHub repository | none | done |
| 2 | Solution scaffolding | 1 | next |
| 3 | CI baseline and deferred GitHub settings | 2 | planned |
| 4 | Pin native dependencies and minimum OS versions | 1 | planned |
| 5 | Binding generator design | 4 | planned |
| 6 | Native build for the host platform | 2, 4 | planned |
| 7 | Generator: WebGPU raw layer | 2, 5 | planned |
| 8 | Generator: SDL3 and miniaudio raw layers | 5, 6 | planned |
| 9 | Generated layout tests on the host | 6, 7, 8 | planned |
| 10 | Native CI matrix for every RID | 3, 6 | planned |
| 11 | `Jade.Native.*` packaging | 10 | planned |
| 12 | WebGPU idiomatic layer | 7 | planned |
| 13 | SDL3 and miniaudio idiomatic layers | 8 | planned |
| 14 | Desktop sample | 9, 11, 12, 13 | planned |
| 15 | Browser sample | 14 | planned |
| 16 | Android sample | 14 | planned |
| 17 | iOS sample | 14 | planned |
| 18 | Layout tests on every target in CI | 9, 15, 16, 17 | planned |
| 19 | NuGet publication | 3, 11 | planned |

## Tasks

### 1. Project charter and GitHub repository (done, 2026-10-05)

`CLAUDE.md`, `docs/architecture.md`, ADRs 0001 to 0017, this roadmap, `LICENSE`, `README.md`,
`SECURITY.md`, `.gitattributes`, `.gitignore`, `.github/labels.yml`, the social preview image, and
the GitHub repository with the settings of the first phase of
[0017](adr/0017-github-repository-baseline.md).

### 2. Solution scaffolding

- `global.json` pinning the .NET 11 RC SDK, `Directory.Build.props`, `Directory.Build.targets`,
  `Directory.Packages.props` (central package management, transitive pinning, lock files, audit),
  `.editorconfig`, `Jade.slnx`.
- Empty projects for everything under `src/`, wired as described in
  [0015](adr/0015-build-and-packaging-conventions.md), with `PublicAPI.*.txt` files and NuGet
  metadata ([0002](adr/0002-license-and-public-identity.md)).
- A first test project on Microsoft.Testing.Platform. **Decision to take: the test framework**
  (ADR).
- The package icon.
- Done when `dotnet build`, `dotnet test` and `dotnet pack` succeed with `TreatWarningsAsErrors`
  and package validation enabled.

### 3. CI baseline and deferred GitHub settings

- Build and test workflow on Linux, Windows and macOS; format check; NuGet locked mode.
- CodeQL (C#, GitHub Actions), OpenSSF Scorecard, `dependabot.yml` (`nuget`, `github-actions`),
  label synchronization from `.github/labels.yml`, labeler, issue and PR templates, `CODEOWNERS`,
  `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`.
- Required checks added to the `main` ruleset, declaring
  `require_extra_approval_for_unattributed_changes` explicitly; check whether Dependabot pull
  requests are affected by that rule (third-party reports say app-opened pull requests wait for a
  human approval; GitHub's documentation only mentions Copilot). CI, CodeQL and Scorecard badges
  in the README.
- **Decision to take: CI details** (runners, caching).
- Done when every workflow is green on a pull request and the ruleset requires its checks.

### 4. Pin native dependencies and minimum OS versions

- `native/versions.json`: Dawn commit, SDL3 and miniaudio versions, emsdk (must match the workload,
  [0013](adr/0013-browser-native-toolchain.md)), Android NDK, and the other toolchain versions.
- Minimum OS versions derived from Dawn's actual requirements. **Decision to take** (ADR).
- `THIRD-PARTY-NOTICES.md` written from the license files of the pinned sources.
- Done when every version is pinned with its source and the ADR is accepted.

### 5. Binding generator design

- **Decisions to take** (ADR): the generator's intermediate representation and mapping rules,
  chained structs, descriptor handling (ref struct mirrors copied into an arena on cold paths,
  `fixed` without copy on hot paths), and the visibility of the raw layer (public sub-namespace or
  internal).
- Annotation configuration format, shared `MA_*` defines.
- Done when the ADR is accepted and the generator's skeleton runs on the pinned inputs.

### 6. Native build for the host platform

- xmake package definitions in `native/` (Dawn and SDL3 through CMake, miniaudio and its shim),
  `scripts/build-native.cs`, starting with `linux-x64`.
- Done when `build-native.cs` produces the three libraries from `native/versions.json` on a clean
  machine.

### 7. Generator: WebGPU raw layer

- Raw layer of `Jade.Wgpu` generated from `dawn.json`, deterministic, with a regeneration check.
- Done when the generated code builds, regeneration produces no diff, and a smoke test creates a
  WebGPU instance on the host.

### 8. Generator: SDL3 and miniaudio raw layers

- Raw layers of `Jade.Sdl` and `Jade.MiniAudio` from the headers through ClangSharp, with their
  annotation configurations.
- Done when the generated code builds, regeneration produces no diff, and smoke tests initialize
  SDL3 video and a miniaudio context on the host.

### 9. Generated layout tests on the host

- `sizeof`/`offsetof` comparison between C and C# for every generated struct
  ([0009](adr/0009-interop-mapping-conventions.md)).
- Done when the tests pass on the host and fail when a layout is deliberately broken.

### 10. Native CI matrix for every RID

- Native builds on a runner matrix for every RID of [0012](adr/0012-supported-targets.md): old glibc
  baseline, universal macOS and iOS simulator binaries, Android NDK, emsdk archives; artifacts with
  provenance attestations; `scripts/fetch-native.cs`; CodeQL C/C++ for the shims; a check that the
  workload's Emscripten version matches `native/versions.json`.
- **Decision to take: native build frequency and caching.**
- Done when a workflow run produces attested artifacts for every RID and `fetch-native.cs`
  retrieves them.

### 11. `Jade.Native.*` packaging

- `runtimes/{rid}/native`, `runtimes/osx/native`, `buildTransitive/` targets for iOS and the
  browser ([0011](adr/0011-native-package-layout.md)).
- Done when the packages pass package validation and contain every RID.

### 12. WebGPU idiomatic layer

- Handles, descriptors, chained structs, `Task`-based asynchronous operations with
  `ProcessEvents`, UTF-8 and `string` overloads.
- Done when the desktop smoke test clears a surface through the idiomatic API only.

### 13. SDL3 and miniaudio idiomatic layers

- Done when the host smoke tests open a window, read input events and play a sound through the
  idiomatic APIs only.

### 14. Desktop sample

- Window, WebGPU clear color, sound playback, on Windows, Linux and macOS; NativeAOT publish.
- Done when the sample runs from the packages on the three desktop platforms in CI.

### 15. Browser sample

- `Jade.Emscripten`, `requestAnimationFrame` loop, asynchronous WebGPU initialization.
- Done when the sample runs in a WebGPU-capable browser from the packages.

### 16. Android sample

- `SDLActivity`, surface recreation, audio lifecycle. **Decision to take: emulator tests in CI.**
- Done when the sample survives background and foreground cycles on an emulator.

### 17. iOS sample

- xcframework through `buildTransitive/`, lifecycle handling.
- Done when the sample runs on the simulator; device testing is reported as not verified unless a
  device is available.

### 18. Layout tests on every target in CI

- Done when the generated layout tests pass on every RID family, WebAssembly included.

### 19. NuGet publication

- **Decisions to take** (ADR): versioning, release workflow, trusted publishing, package
  attestations, `CHANGELOG.md` format.
- Final check of the package IDs before the first publication (see
  [0004](adr/0004-webgpu-via-dawn.md)); NuGet badge in the README.

## Open decisions

| Decision | Task |
| --- | --- |
| Test framework (on Microsoft.Testing.Platform) | 2 |
| CI runners and caching | 3, 10 |
| Minimum OS versions | 4 |
| Generator IR and mapping rules, chained structs, descriptors | 5 |
| Raw layer visibility | 5 |
| Native build frequency, emulator tests | 10, 16 |
| Package versioning and release workflow | 19 |

## Later milestones

Out of scope until milestone 1 is done: 2D renderer, game loop, scene model or ECS, tools.
