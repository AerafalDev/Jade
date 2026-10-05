# Roadmap

Ordered list of the next tasks. Each task is done in its own work session and ends with
`CLAUDE.md`, `docs/architecture.md`, this file and the relevant ADRs up to date. A task can start
once all the tasks it depends on are done; tasks without a mutual dependency can run in any order.

Status: `done`, `next`, `planned`.

## Milestone 1: interop foundation

| # | Task | Depends on | Status |
| --- | --- | --- | --- |
| 1 | Project charter and GitHub repository | none | done |
| 2 | Solution scaffolding | 1 | done |
| 3 | CI baseline and deferred GitHub settings | 2 | done |
| 4 | Pin native dependencies and minimum OS versions | 1 | done |
| 5 | Binding generator design | 4 | done |
| 6 | Native build for the host platform | 2, 4 | done |
| 7 | Generator: WebGPU raw layer | 2, 5, 6 | done |
| 8 | Generator: SDL3 and miniaudio raw layers | 5, 6 | done |
| 9 | Generated layout tests on the host | 6, 7, 8 | next |
| 10 | Native CI matrix for every RID | 3, 6 | planned |
| 11 | `Jade.Native.*` packaging | 10 | planned |
| 12 | WebGPU idiomatic layer | 7 | planned |
| 13 | SDL3 and miniaudio idiomatic layers | 8 | planned |
| 14 | Desktop sample | 9, 11, 12, 13 | planned |
| 15 | Browser sample | 14, 20 | planned |
| 16 | Android sample | 14 | planned |
| 17 | iOS sample | 14 | planned |
| 18 | Layout tests on every target in CI | 9, 15, 16, 17 | planned |
| 19 | NuGet publication | 3, 11 | planned |
| 20 | Generator: Emscripten raw layer | 8 | planned |

## Tasks

### 1. Project charter and GitHub repository (done, 2026-10-05)

`CLAUDE.md`, `docs/architecture.md`, ADRs 0001 to 0017, this roadmap, `LICENSE`, `README.md`,
`SECURITY.md`, `.gitattributes`, `.gitignore`, `.github/labels.yml`, the social preview image, and
the GitHub repository with the settings of the first phase of
[0017](adr/0017-github-repository-baseline.md).

### 2. Solution scaffolding (done, 2026-10-05)

`global.json` (exact RC 1 SDK, MTP test runner), `Directory.Build.props`,
`Directory.Build.targets`, `Directory.Packages.props`, `.editorconfig`, `Jade.slnx`; empty projects
in `src/` and `native/`; `tests/Jade.Tests`; the package README and icon. Decisions:
[0018](adr/0018-test-framework.md) (MSTest),
[0019](adr/0019-browser-and-roslyn-component-targeting.md) (browser and Roslyn component
targeting), [0020](adr/0020-repository-layout-and-conventions.md) (layout with `native/` and
`build/`, supersedes 0014), [0021](adr/0021-build-and-packaging-conventions.md) (no lock files,
supersedes 0015). The interop projects later moved from `src/` to `interop/`
([0023](adr/0023-repository-layout-and-conventions.md), supersedes 0020).

### 3. CI baseline and deferred GitHub settings (done, 2026-10-05)

Workflows `ci.yml` (format check, then build, test and pack on Linux, Windows and macOS),
`codeql.yml` (C#, GitHub Actions), `scorecard.yml`, `labels.yml` and `labeler.yml`;
`dependabot.yml` (`nuget`, `github-actions`, grouped minor and patch updates, seven-day cooldown);
issue forms, pull request template, `CODEOWNERS`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`
(Contributor Covenant 3.0 by reference) and the CI, CodeQL and Scorecard badges. The `main`
ruleset requires the six CI and CodeQL checks on an up-to-date branch; the extra approval for
unattributed pull requests only concerns Copilot and has no effect with zero required approvals
([0017](adr/0017-github-repository-baseline.md)). Decision:
[0022](adr/0022-ci-runners-and-caching.md) (pinned GitHub-hosted runners, no cache). Attestations
and CodeQL for C/C++ move to tasks 10 and 19, when there is something to apply them to.

### 4. Pin native dependencies and minimum OS versions (done, 2026-10-05)

`build/versions.json` (format in the [architecture](architecture.md#buildversionsjson)): Dawn
`v20261002.154047`, SDL3 3.4.18, miniaudio 0.11.25, the workload's Emscripten (6.0.3, packs named
6.0.2), Android NDK r28c, xmake 3.1.1, CMake 4.4.4 and the minimum OS versions, each with its
source; `Jade.Tests` checks the sources and commit hashes. `THIRD-PARTY-NOTICES.md` from the
license files of the pinned sources, Dawn's `DEPS` dependencies included. Decisions:
[0024](adr/0024-minimum-os-versions.md) (Windows 10 1607, glibc 2.28, macOS 14.0, iOS 14.0,
Android API 26, a browser with WebGPU) and
[0025](adr/0025-browser-natives-with-workload-emscripten.md) (browser archives built with the
workload's own Emscripten toolchain, since no upstream emsdk equals it; supersedes 0013).

### 5. Binding generator design (done, 2026-10-05)

`scripts/binding-generator.cs` and `scripts/binding-generator/`: fetches the pinned sources into
`artifacts/binding-generator/sources/`, reads `dawn.json` into a strict typed model with its native
and browser variants, and parses the SDL3 and miniaudio headers with the libclang of ClangSharp
21.1.8.4 for the 12 RID triples, using its own C runtime headers; it reports what it loaded and
emits no C# yet. `interop/Jade.Wgpu`, `interop/Jade.Sdl` and `interop/Jade.MiniAudio` have a
`bindings.json`; `build/miniaudio/config.h` holds the `MA_*` defines (none yet). Decisions:
[0026](adr/0026-binding-generator-pipeline.md) (pipeline, intermediate representation with
per-platform availability, multi-triple parsing, configuration format, `MA_*` defines),
[0027](adr/0027-interop-mapping-rules.md) (remaining mapping rules),
[0028](adr/0028-internal-raw-interop-layer.md) (internal raw layer with C names, superseded by
[0034](adr/0034-raw-layer-with-dotnet-names.md)) and
[0029](adr/0029-descriptors-and-chained-structs.md) (descriptor classification, stack-based arena,
generic chained extensions).

### 6. Native build for the host platform (done, 2026-10-05)

`build/xmake.lua` with the definitions of `build/dawn/` (Dawn's monolithic `webgpu_dawn` through
CMake, and `deps.json`, the `DEPS` entries its build needs), `build/sdl/` (SDL3 through CMake, with
the features each platform must have) and `build/miniaudio/` (miniaudio and the shim that allocates
the opaque structures, `config.h` with the `MA_*` defines); `scripts/build-native.cs` and
`scripts/build-native/`, which check the pinned xmake and CMake, fetch the sources with git at the
pinned commits, run xmake and check the libraries of `artifacts/native/bin/linux-x64/`. Verified in
fresh `ubuntu:24.04` and `archlinux` containers. `THIRD-PARTY-NOTICES.md` was checked against the
files compiled into the `linux-x64` libraries and gained SDL3's Wayland protocols and, for
Windows, DirectXShaderCompiler. Decisions: [0030](adr/0030-d3d12-shader-compilers.md) (DXC built
and shipped, FXC from the system) and [0031](adr/0031-native-build-definitions.md) (build
definitions, sources, SDL3 features, `MA_*` defines, outputs).

### 7. Generator: WebGPU raw layer (done, 2026-10-05)

`scripts/binding-generator/Model/` (intermediate representation), `Dawn/DawnModelBuilder.cs` (the
`dawn.json` front-end, which reproduces Dawn's `api.h`: added members and functions, enum value
offsets, `WGPU_*_INIT` defaults), `Projection/` (.NET names, value structure classification, C#
types) and `Emission/` (one file per type, deterministic, stale files deleted). `Jade.Wgpu` has its
raw layer in `Generated/` (296 files: 71 enums, 28 handles, `Bool32`, 97 public and 98 internal
structures, 12 constants and 276 functions in `NativeMethods`), `Properties/AssemblyInfo.cs` and
1,474 declarations in `PublicAPI.Unshipped.txt`; `bindings.json` gained `library`, `exclude`,
`names` and `words`. The CI `build` jobs build the generator with warnings as errors and check the
regeneration on the three hosts. `tests/Jade.Wgpu.Tests` checks the managed side of the raw layer
and, with the host's natives, creates an instance, lists the instance features and requests an
adapter through a callback. A one-off comparison with Dawn's generated `webgpu.h` compiled by GCC
matched every layout, enum value, function arity and all but one default (a NaN sign bit).
Decision: [0032](adr/0032-webgpu-raw-layer-generation.md) (projection, defaults, emission, native
library search paths, public API files, verification).

### 8. Generator: SDL3 and miniaudio raw layers (done, 2026-10-05)

The C header front-end (`Clang/TargetModelBuilder.cs`, `Clang/ClangModelBuilder.cs`,
`Clang/MacroEvaluator.cs`): each target's parse collected on its own, merged into platform
availability, divergences reported all at once unless opaque or excluded, layouts checked against
clang's, macros selected by regular expressions and evaluated by clang into enums and constants.
`Jade.Sdl` (350 files, 1,177 functions, 2,859 public declarations) is bound from `SDL3/SDL.h` and
`SDL3/SDL_main.h`, without `SDL_audio.h`; `Jade.MiniAudio` (292 files, 955 functions, 871 public
declarations) from `miniaudio.h` and the shim, which now allocates the 13 opaque types. The emitter
gained unions, inline arrays, string constants, `[MarshalAs(UnmanagedType.U1)]` for C `bool` in
imports and one `NativeMethods` file per header. Every raw declaration of the three libraries now
has a .NET name, its internal part in `Jade.<Library>.Raw` (maintainer's decision). `ma_vec3f` maps
to `System.Numerics.Vector3`. `tests/Jade.Sdl.Tests` and `tests/Jade.MiniAudio.Tests` check the
exports of the host's libraries, initialize SDL3 video (the `dummy` driver and the host's display)
and a miniaudio context, and read a `Vector3` returned by value. Decisions:
[0033](adr/0033-c-header-raw-layer-generation.md) (front-end, annotations, SDL3 headers,
miniaudio opaque types), [0034](adr/0034-raw-layer-with-dotnet-names.md) (raw layer with .NET names,
supersedes 0028) and [0035](adr/0035-emscripten-interop-generation.md) (`Jade.Emscripten` will be
generated, roadmap task 20).

### 9. Generated layout tests on the host

- `sizeof`/`offsetof` comparison between C and C# for every generated struct
  ([0009](adr/0009-interop-mapping-conventions.md)), in each interop assembly's test project; the
  layout emitter maps C names to C# names from the projection
  ([0032](adr/0032-webgpu-raw-layer-generation.md), [0034](adr/0034-raw-layer-with-dotnet-names.md)).
- Cover what task 8 added: unions, inline arrays, anonymous records, and the C types mapped to .NET
  types (`ma_vec3f` and `System.Numerics.Vector3`); opaque types have no managed layout to compare.
- Done when the tests pass on the host and fail when a layout is deliberately broken.

### 10. Native CI matrix for every RID

- Native builds on a runner matrix for every RID of [0012](adr/0012-supported-targets.md), at the
  minimum OS versions of [0024](adr/0024-minimum-os-versions.md): a glibc 2.28 environment (Dawn
  uses the `manylinux_2_28` images), universal macOS and iOS simulator binaries, Android NDK,
  browser archives built with the workload's Emscripten toolchain
  ([0025](adr/0025-browser-natives-with-workload-emscripten.md)); artifacts with provenance
  attestations; `scripts/fetch-native.cs`; CodeQL C/C++ for the shims; a check that the workload's
  Emscripten versions match `build/versions.json`.
- Validate Emdawnwebgpu with the workload's Emscripten (Dawn tests it with emsdk 5.0.6), and the
  use of the workload's toolchain outside MSBuild.
- Extend `build/` and `build-native.cs` to every RID ([0031](adr/0031-native-build-definitions.md)):
  SDL3 feature lists per platform, the built DXC and its `DEPS` entry on Windows
  ([0030](adr/0030-d3d12-shader-compilers.md)), `MA_NO_RUNTIME_LINKING` and Objective-C for
  miniaudio on Apple platforms, the glibc 2.28 environment and how the C++ runtime is linked; xmake
  must not run as root in containers. Check `THIRD-PARTY-NOTICES.md` against the files compiled
  for each target, as done for `linux-x64`.
- **Decision to take: native build runners (including Linux and Windows arm64), frequency and
  caching** ([0022](adr/0022-ci-runners-and-caching.md) covers the managed CI only).
- Done when a workflow run produces attested artifacts for every RID and `fetch-native.cs`
  retrieves them.

### 11. `Jade.Native.*` packaging

- `runtimes/{rid}/native`, `runtimes/osx/native`, `buildTransitive/` targets for iOS and the
  browser ([0011](adr/0011-native-package-layout.md)).
- How the packages surface the minimum OS versions of [0024](adr/0024-minimum-os-versions.md),
  which are above .NET 11's floors on iOS and Android.
- Done when the packages pass package validation and contain every RID.

### 12. WebGPU idiomatic layer

- Handles, descriptors, chained structs, `Task`-based asynchronous operations with
  `ProcessEvents`, UTF-8 and `string` overloads.
- Descriptor mirrors, the arena and generic chained extensions of
  [0029](adr/0029-descriptors-and-chained-structs.md). **Decisions to take:** how output structures
  freed by `FreeMembers` are exposed, and how extensions of nested roots (array elements, limits)
  are passed.
- Methods on the type they operate on, from the owner and kind of each function in the model
  ([0032](adr/0032-webgpu-raw-layer-generation.md)). **Decisions to take:** `required` members of
  the public value structures; which constants (`WGPU_WHOLE_SIZE`, the `*_UNDEFINED` sentinels)
  become public; the name of the `Buffer` handle, ambiguous with `System.Buffer` under
  `using System;`.
- Done when the desktop smoke test clears a surface through the idiomatic API only.

### 13. SDL3 and miniaudio idiomatic layers

- **Decisions to take:** the public names that collide with framework types (`Thread`, `Mutex`,
  `Semaphore`, `Process`, `Environment`, `DateTime`, `Guid`, `Condition`, `Timer`), as task 12 does
  for `Buffer`; whether the idiomatic layer exposes SDL3's GPU and 2D renderer APIs, which the raw
  layer binds; how miniaudio's `_w` functions, which take `void*` for `wchar_t*`, are exposed.
- Hand-written alternatives to the variadic logging and formatting functions (`SDL_Log`,
  `SDL_SetError`, `ma_log_postf`) ([0027](adr/0027-interop-mapping-rules.md)).
- Done when the host smoke tests open a window, read input events and play a sound through the
  idiomatic APIs only.

### 14. Desktop sample

- Window, WebGPU clear color, sound playback, on Windows, Linux and macOS; NativeAOT publish.
- Done when the sample runs from the packages on the three desktop platforms in CI.

### 15. Browser sample

- `requestAnimationFrame` loop through `Jade.Emscripten` (task 20), asynchronous WebGPU
  initialization.
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
- Supported OS list in the README, from [0024](adr/0024-minimum-os-versions.md).

### 20. Generator: Emscripten raw layer

- `Jade.Emscripten`'s raw layer generated from `emscripten/emscripten.h` and `emscripten/html5.h`
  through the C header front-end ([0035](adr/0035-emscripten-interop-generation.md)).
- **Decisions to take:** where the headers come from (the `dotnet/emscripten` fork at the commit
  the SDK's VMR pins, or the installed workload pack, which the regenerating CI jobs would then
  install); per-library targets (`browser-wasm` only, no per-member platform attribute under the
  assembly's `[SupportedOSPlatform("browser")]`).
- Verify the import name of Emscripten functions linked statically into the application's module,
  and `[UnmanagedCallersOnly]` callbacks under Mono WebAssembly.
- Done when the generated layer builds, regeneration produces no diff, and a browser test calls
  an Emscripten function.

## Open decisions

| Decision | Task |
| --- | --- |
| Output structures and extensions of nested chain roots | 12 |
| Names that collide with framework types; SDL3 GPU and renderer APIs; miniaudio `_w` functions | 12, 13 |
| Source of Emscripten's headers and per-library targets | 20 |
| `required` members, public constants, the `Buffer` name | 12 |
| Native build runners, frequency and caching; emulator tests | 10, 16 |
| Package versioning and release workflow | 19 |

## Later milestones

Out of scope until milestone 1 is done: 2D renderer, game loop, scene model or ECS, tools.
