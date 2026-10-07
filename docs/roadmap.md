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
| 9 | Generated layout tests on the host | 6, 7, 8 | done |
| 10 | Native CI matrix for every RID | 3, 6 | done |
| 11 | `Jade.Native.*` packaging | 10 | done |
| 12 | WebGPU idiomatic layer | 7 | done |
| 13 | SDL3 and miniaudio idiomatic layers | 8 | next |
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

### 9. Generated layout tests on the host (done, 2026-10-05)

The generator writes, for each interop library, `build/layout/<name>.g.c`, which reports the size
and alignment of every generated structure and the size and offset of every member as the C
compiler sees them, and `tests/<project>.Tests/Generated/LayoutTests.g.cs`, which measures the C#
layout and compares: 195 structures for `Jade.Wgpu`, 125 for `Jade.Sdl`, 205 and `ma_vec3f` (as
`System.Numerics.Vector3`) for `Jade.MiniAudio`, unions, anonymous records and the first element of
every array member included. The intermediate representation records how C names each record and
where an anonymous one lies in its parent, and keeps the members of structures mapped to .NET
types. `build/layout/xmake.lua` builds the sources into test-only libraries
(`jade_<name>_layout`, xmake group `layout`), which `scripts/build-native.cs` installs into
`artifacts/native/test/<rid>/`; the test projects copy them. The tests pass on `linux-x64` and fail,
listing every difference, when a C# field type, a field order, an inline array length or a mapped
.NET type is wrong. The CI regeneration check covers `build/layout/` and `tests/`. Decision:
[0036](adr/0036-generated-layout-tests.md) (what is compared, the generated C source, the layout
libraries, how the tests load them).

### 10. Native CI matrix for every RID (done, 2026-10-06)

`scripts/build-native.cs` builds any of the 12 runtime identifiers its host can build (`--rid`),
installs the pinned xmake and CMake from SHA-256-pinned archives (`--install-tools`, xmake built
from source on Linux and macOS), checks the NDK's revision and the workload's Emscripten versions,
and checks the exports of libraries it cannot load with `nm`; xmake runs in `build/`, which fixes
the build directory that landed outside the repository. `build/` gained the definitions of every
platform: Dawn shared (static on iOS) with the built DXC on Windows, Emdawnwebgpu with its
JavaScript libraries in the browser, SDL3 with a list of required features per platform, miniaudio
as Objective-C on iOS, static archives and module names on iOS and in the browser, the static CRT
on Windows and `c++_static` on Android; `build/dawn/deps.json` gained per-platform entries;
`build/linux/Dockerfile` is the glibc 2.28 environment (`manylinux_2_28` by digest, SDL3's
development packages, libdecor and liburing built from their release commits).
`.github/workflows/native.yml` builds every runtime identifier on GitHub-hosted runners, merges
the Apple builds into universal libraries and xcframeworks, uploads the natives and the layout
libraries as separate artifacts, attests every file, lists the compiled source directories for
`THIRD-PARTY-NOTICES.md`, and reports through the required `natives` check; a `changes` job skips
the builds when the native build inputs are unchanged. `scripts/fetch-native.cs` installs the
attested artifacts of the newest matching run of `main` (or of `--run`), verified with
`gh attestation verify`. CodeQL analyzes the C shim (`analyze (c-cpp)`), and the CI `build` jobs
build both native scripts with warnings as errors. Decision:
[0038](adr/0038-native-ci.md) (runners, build environments and tools, compile targets and
runtimes, triggers without cache, artifacts and attestations, `fetch-native.cs`, CodeQL); the
OpenSSF Scorecard workflow was removed on the way ([0037](adr/0037-github-repository-baseline.md),
supersedes 0017).

### 11. `Jade.Native.*` packaging (done, 2026-10-07)

The three packages hold the natives of the 12 runtime identifiers: `runtimes/{rid}/native/`
(with `dxcompiler.dll` on Windows), `runtimes/osx/native/` for the universal libraries, and under
`buildTransitive/` the iOS xcframeworks, the browser archives with Emdawnwebgpu's four
JavaScript libraries and `webgpu-externs.js`, and one targets file per package: a
`NativeReference` with `ForceLoad` and the frameworks each library needs on iOS (read in the
pinned sources and checked against the undefined symbols of the CI's archives), a
`NativeFileReference` and the `emcc` options in the browser, and a `JADENATIVE001` error for an
iOS or Android application below iOS 14.0 or API 26. They declare no target framework and no
dependency; each carries the repository's `THIRD-PARTY-NOTICES.md`. `scripts/fetch-native.cs --package`
fetches the shipped artifacts of every runtime identifier into `artifacts/native/package/`, and
every fetch now verifies each file against an attestation of the run it comes from (the check of
the run's `head_sha` rejected every pull request run). Each `native/` project lists its files;
`native/Directory.Build.targets` maps them and fails on a missing one. The `package` job of
`native.yml` packs them on every run, inside the `natives` check. `Jade.Tests` compares the
minimums of the targets with `build/versions.json`. A throwaway `linux-x64` application ran the
packaged natives and a throwaway browser application linked them; iOS and Android consumption
are not verified. Decision: [0039](adr/0039-native-packaging.md) (source of the natives, package
content, `buildTransitive/` targets, minimum OS check); the P/Invoke resolution on iOS moves to
task 17 (maintainer's decision).

### 12. WebGPU idiomatic layer (done, 2026-10-07)

The generator projects an idiomatic layer above the raw layer of `Jade.Wgpu`
(`Projection/IdiomaticProjection.cs`, `Emission/IdiomaticLayerEmitter.cs`, `MirrorEmitter.cs`,
`MethodShape.cs`), configured by the `idiomatic` object of `bindings.json`: 261 members on the
28 handles (`{Handle}.Idiomatic.g.cs`), 62 `ref struct` mirrors, 3 element mirrors, 15
snapshots, the extension interfaces of 55 value structures and 4 slot types, 168 files and 870
public declarations. Structures are classified by use; descriptors pin their top-level spans
and lower the rest into a stack arena; input and output extensions are generic overloads, the
extensions of nested roots typed slots; statuses throw `WgpuException<TStatus>`; asynchronous
functions return tasks that `Instance.ProcessEvents` completes; handles are `IDisposable`; text
has UTF-8 and `string` overloads. Hand-written: the arena, `Utf8Text`, the interfaces and
exceptions, `GpuError`, the device callbacks of `DeviceDescriptor`, `Device.PopErrorScopeAsync`
and the mapped ranges of `GpuBuffer`. `tests/Jade.Wgpu.Tests` checks the managed side and, on the
host, drives the GPU through the idiomatic layer only, up to a triangle read back from a texture
and the clearing of an SDL3 window's surface on Wayland; the raw smoke tests now qualify raw
structures with `Raw.`. Decision: [0040](adr/0040-webgpu-idiomatic-layer.md) (supersedes 0029):
snapshots for output structures, typed slots for nested roots, no `required` members, constants
on their types, `GpuBuffer` (maintainer's decisions). Surfaces other than Wayland and the
browser are not verified.

### 13. SDL3 and miniaudio idiomatic layers

- Extend the idiomatic projection of [0040](adr/0040-webgpu-idiomatic-layer.md) to the C header
  front-end, whose functions have no owner or kind yet.
- **Decisions to take:** the public names that collide with framework types (`Thread`, `Mutex`,
  `Semaphore`, `Process`, `Environment`, `DateTime`, `Guid`, `Condition`, `Timer`), as task 12 did
  for `Buffer` (`GpuBuffer`); whether the idiomatic layer exposes SDL3's GPU and 2D renderer APIs, which the raw
  layer binds; how miniaudio's `_w` functions, which take `void*` for `wchar_t*`, are exposed.
- Hand-written alternatives to the variadic logging and formatting functions (`SDL_Log`,
  `SDL_SetError`, `ma_log_postf`) ([0027](adr/0027-interop-mapping-rules.md)).
- Done when the host smoke tests open a window, read input events and play a sound through the
  idiomatic APIs only.

### 14. Desktop sample

- Window, WebGPU clear color, sound playback, on Windows, Linux and macOS; NativeAOT publish.
- The loop pumps `Instance.ProcessEvents`, which completes the tasks of the idiomatic layer, and
  releases a surface and its device before the window, whose display the driver uses to destroy
  the swap chain ([0040](adr/0040-webgpu-idiomatic-layer.md)).
- The interop packages depend on their native package, so that applications get the natives
  transitively ([0011](adr/0011-native-package-layout.md)); the native packages come from the
  `package` job of the native workflow ([0039](adr/0039-native-packaging.md)).
- Done when the sample runs from the packages on the three desktop platforms in CI.

### 15. Browser sample

- `requestAnimationFrame` loop through `Jade.Emscripten` (task 20), asynchronous WebGPU
  initialization.
- Check the browser imports of `Jade.Wgpu` against Emdawnwebgpu's exports: its archive defines 46
  functions with C++ linkage only, such as `wgpuAdapterSetLabel` ([0038](adr/0038-native-ci.md)).
- Check that Emdawnwebgpu delivers the `AllowProcessEvents` callbacks of the idiomatic layer's
  tasks when the loop returns to the browser ([0040](adr/0040-webgpu-idiomatic-layer.md)).
- Done when the sample runs in a WebGPU-capable browser from the packages.

### 16. Android sample

- `SDLActivity`, surface recreation, audio lifecycle. **Decision to take: emulator tests in CI.**
- Done when the sample survives background and foreground cycles on an emulator.

### 17. iOS sample

- xcframework through `buildTransitive/`, lifecycle handling.
- **Decision to take:** how the interop's P/Invokes reach the statically linked libraries. Under
  .NET for iOS, `xamarin_pinvoke_override` resolves only `__Internal` with `dlsym`, and only the
  `__Internal` P/Invokes are exported; CoreCLR is the default runtime
  ([0039](adr/0039-native-packaging.md)). Options: `__Internal` imports on iOS, a
  `DllImportResolver` to the main program with the imported functions kept and exported
  (`ReferenceNativeSymbol` items generated per library), `DirectPInvoke` under NativeAOT, or
  dynamic frameworks (reopens [0038](adr/0038-native-ci.md)).
- Verify the frameworks of the packages' targets by linking the application.
- Done when the sample runs on the simulator; device testing is reported as not verified unless a
  device is available.

### 18. Layout tests on every target in CI

- Run the generated tests of [0036](adr/0036-generated-layout-tests.md) with each target's layout
  libraries. **Decision to take:** how the tests reach `jade_<name>_layout` where the layout
  library is a static archive linked into the test application (iOS, browser) rather than a file
  `NativeLibrary.TryLoad` finds.
- Verify the C# measurements (the generic alignment probe, address differences) under NativeAOT,
  Mono on iOS and Android, and Mono WebAssembly.
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
| Names that collide with framework types; SDL3 GPU and renderer APIs; miniaudio `_w` functions | 13 |
| Source of Emscripten's headers and per-library targets | 20 |
| How the layout tests reach statically linked layout libraries | 18 |
| Emulator tests | 16 |
| P/Invoke resolution against statically linked libraries on iOS | 17 |
| Package versioning and release workflow | 19 |

## Later milestones

Out of scope until milestone 1 is done: 2D renderer, game loop, scene model or ECS, tools.
