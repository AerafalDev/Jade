# 0039. Packing the Jade.Native packages from the attested natives

- Status: Accepted
- Date: 2026-10-07

## Context

[0011](0011-native-package-layout.md) gives the layout of the `Jade.Native.*` packages:
`runtimes/{rid}/native/` for Windows, Linux and Android, `runtimes/osx/native/` for the universal
macOS libraries, and `buildTransitive/` targets for iOS and the browser, which link natives
statically. [0038](0038-native-ci.md) builds every runtime identifier in the native workflow and
attests every file of its artifacts. [0024](0024-minimum-os-versions.md) sets minimums above
.NET 11's floors on iOS (14.0 against 13.0) and Android (API 26 against 24), and asks the packages
to surface them. Roadmap task 11 packs the natives and leaves three questions: where the packaging
takes the natives from, how the packages surface the minimums, and the exact form of the
`buildTransitive/` targets.

Verified on 2026-10-06 and 2026-10-07 with SDK `11.0.100-rc.1.26425.128`:

- NuGet pack (the SDK's `NuGet.Build.Tasks.Pack.targets`): targets in `buildTransitive/net11.0/`
  without `lib/` or `ref/` raise NU5127, which asks for a placeholder `lib/net11.0/_._`; with it,
  `SuppressDependenciesWhenPacking` raises NU5128, which asks for a `net11.0` dependency group.
  Targets at the root of `buildTransitive/` raise neither. `TargetsForTfmSpecificContentInPackage`
  adds files with any `PackagePath`; package validation (`RunPackageValidation`) runs on these
  packages.
- `NuGetFramework.ParseFolder` of the SDK's `NuGet.Frameworks` reads `ios` and `browser-wasm` as
  unsupported frameworks, compatible with no project, `net11.0-ios` included, so folders of that
  name under `buildTransitive/` cannot be taken for framework-specific build files.
- A `net11.0` application restoring the packages imports `buildTransitive/<id>.targets` and
  resolves `runtimes/{rid}/native/` per runtime identifier: a throwaway `linux-x64` console application loaded the CI's `libSDL3.so`,
  `libminiaudio.so` and `libwebgpu_dawn.so` from the packages, framework-dependent and published
  with `-r linux-x64`.
- .NET for iOS (`dotnet/macios` at `dotnet-11.0.1xx-rc1-12193`):
  - `NativeReference` metadata (`msbuild/Xamarin.MacDev.Tasks/LinkerOptions.cs`): `Frameworks`
    and `WeakFrameworks` are split on spaces and tabs only; a `Static` reference with `ForceLoad`
    is linked with `-force_load`; `Kind` is required. The native link uses `clang++`
    (`LinkNativeCode.cs`), so libc++ is always linked.
  - An `.xcframework` reference is resolved by `ResolveNativeReferences`, which picks the slice
    by `SupportedPlatform`, `SupportedPlatformVariant` and the requested architectures, copies the
    item's metadata, and sets `Kind` from the library's extension (`.a` is `Static`).
  - Native assets from `runtimes/<rid>/native/` are classified by `ComputeBundleLocation`: an
    `.a` is linked as a static reference without `ForceLoad`, a `.dylib` is linked and copied
    into the bundle, files inside an `.xcframework` are taken as frameworks
    (`dotnet/targets/Xamarin.Shared.Sdk.targets`). The binding resource package
    (`<assembly>.resources`) is only created by binding projects and needs a managed assembly.
  - `xamarin_pinvoke_override` (`runtime/runtime.m`) resolves with `dlsym` only `__Internal` and
    the runtime's own libraries, the exported symbols list only keeps the `__Internal` P/Invokes
    (`tools/linker/MonoTouch.Tuner/ListExportedSymbols.cs`, `_ExportSymbolsExplicitly` defaults to
    `true`), and `ReferenceNativeSymbol` items keep more (`docs/building-apps/build-items.md`).
    `UseMonoRuntime` defaults to `false`: CoreCLR is the default runtime. A P/Invoke such as
    `[LibraryImport("webgpu_dawn")]` therefore does not reach a statically linked library.
  - `SupportedOSPlatformVersion` becomes `MinimumOSVersion` in `Info.plist`, and a value below
    `MinSupportedOSPlatformVersion` (13.0) fails with E7126 (`CompileAppManifest.cs`). An
    application keeps `OutputType` `Exe`.
- .NET for Android (workload `37.0.0-rc.1.2257`): `_IncludeNativeSystemLibraries` takes the
  `.so` files of `ResolvedFileToPublish` and drops those of `runtimes/` folders other than
  `android*` and `linux-bionic*`; `RuntimeIdentifiers` defaults to `android-arm64;android-x64`;
  `SupportedOSPlatformVersion` defaults to `AndroidMinimumSupportedApiLevel` (24) and is
  normalized to `24.0`; an `Exe` project becomes a `Library` with `AndroidApplication` `true`
  (`Microsoft.Android.Sdk.DefaultProperties.targets`).
- The browser (WebAssembly packs `11.0.0-rc.1.26425.128`):
  - `Microsoft.NET.Sdk.WebAssembly` sets `RuntimeIdentifier` to `browser-wasm` in its `Sdk.props`
    and `UseMonoRuntime` to `true`.
  - `NativeFileReference` items turn `WasmBuildNative` on and are passed to the `emcc` link; the
    P/Invoke modules are the file names of the linked files (`WasmApp.Common.targets`).
  - `EmccExtraLDFlags`, a documented property, is appended to the link's response file, which
    `emcc` splits with `shlex`; `--closure-args` is split again the same way. The .NET link runs
    no Closure Compiler. `DEFAULT_TO_CXX` is `true` in the workload's Emscripten
    (`src/settings.js`), so `emcc` links libc++, which Emdawnwebgpu needs.
  - Dawn `b1236a9` links `emdawnwebgpu_c` with `--js-library` for
    `library_webgpu_enum_tables.js`, `library_webgpu_generated_sig_info.js`,
    `library_webgpu_generated_struct_info.js` and `library_webgpu.js`, in that order, and
    `--closure-args=--externs=` for `webgpu-externs.js` (`src/emdawnwebgpu/CMakeLists.txt`).
  - A throwaway `wasmbrowser` application restoring the packages linked: the link response file
    holds the three archives, the four JavaScript libraries and the externs, the P/Invoke table
    holds `SDL_GetVersion`, `ma_version_string` and `wgpuCreateInstance`, and
    `dotnet.native.js` holds Emdawnwebgpu's JavaScript.
- The frameworks the iOS archives need, from the pinned sources and the CI's archives (`llvm-nm`
  of the device slices):
  - Dawn's CMake links Foundation, IOSurface, QuartzCore and Metal on iOS, plus Cocoa and IOKit
    on macOS only; the archive also references CoreFoundation functions.
  - SDL3's CMake and the framework headers its iOS sources import give Foundation, CoreVideo and
    CoreMedia, AVFoundation (camera), CoreMotion, GameController and CoreHaptics (joystick),
    CoreBluetooth (HIDAPI's `hid.m`), CoreGraphics, QuartzCore and UIKit (video), OpenGLES and
    Metal; with its audio off, no audio framework. The archive references CoreFoundation too.
  - miniaudio on iOS includes AVFoundation and AudioToolbox, not CoreAudio, which is for macOS
    (`miniaudio.h`); the archive references CoreFoundation and Foundation.
- `gh attestation verify --source-digest` fails for the artifacts of a `pull_request` run: their
  certificates carry the merge commit (`refs/pull/12/merge`), not the run's `head_sha`. Every
  certificate carries the run's invocation URI.

## Decision

### Package content

| Package path | Content |
| --- | --- |
| `runtimes/{rid}/native/` | The shared libraries of `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `android-arm64` and `android-x64`, and `dxcompiler.dll` beside `webgpu_dawn.dll` on Windows ([0030](0030-d3d12-shader-compilers.md)) |
| `runtimes/osx/native/` | The universal macOS libraries (`osx-arm64`, `osx-x64`) |
| `buildTransitive/ios/<module>.xcframework/` | The device slice and the universal simulator slice (`ios-arm64`, `iossimulator-arm64`, `iossimulator-x64`) |
| `buildTransitive/browser-wasm/` | The archive named after the module, and, for `Jade.Native.Wgpu`, Emdawnwebgpu's four JavaScript libraries and `webgpu-externs.js` |
| `buildTransitive/<id>.targets` | The targets below |
| `THIRD-PARTY-NOTICES.md`, `README.md`, `icon.png` | The repository's notices, which cover the three packages section by section, and the common metadata |

- The layout libraries are never packed ([0036](0036-generated-layout-tests.md)).
- The iOS and browser files sit beside the targets that reference them, never under `runtimes/`,
  where .NET for iOS would link them a second time without `-force_load` and the browser build
  would copy them as runtime files.
- The packages declare no target framework and no dependency
  ([0021](0021-build-and-packaging-conventions.md)): neither a `lib/` placeholder nor a
  framework folder under `buildTransitive/` (maintainer's decision).

### Where the natives come from

- The packages are packed only from the attested artifacts of the native workflow, never from a
  local `build-native.cs` output, whose macOS libraries are single-architecture, whose iOS
  libraries are not xcframeworks and whose Linux libraries depend on the host's glibc.
- `dotnet run scripts/fetch-native.cs --package [--run <id>]` downloads the shipped artifacts of
  the 12 runtime identifiers (nine artifacts, without the layout libraries) into
  `artifacts/native/package/<name>/`, where `<name>` is the artifact's name without `native-`,
  with the run selection and the checks of [0038](0038-native-ci.md).
- Every fetched file is verified with `gh attestation verify` for the native workflow and
  GitHub-hosted runners, and must have an attestation whose certificate's run invocation URI
  names the run it is fetched from, which also holds for pull request runs. This replaces the
  check of the run's `head_sha`, for every fetch.
- Each packaging project lists its files per artifact (`JadeNativeFile` items); the shared
  `native/Directory.Build.targets` maps them to the package paths above, fails the pack with the
  list of missing files, and makes the projects packable only when `artifacts/native/package/`
  exists, so that a `dotnet pack` without fetched natives, as in the CI `build` jobs, skips them.
- The `package` job of `native.yml` fetches the natives that its own run built, or those of the
  newest run of `main` with the checkout's native build inputs when the run built none, packs
  the three packages with warnings as errors and package validation, and lists their content. The
  `natives` check depends on it. Pull requests from forks that build natives get no attestation
  and are not packed. The packages are neither uploaded nor attested until roadmap task 19.

### `buildTransitive/` targets

Each package has one targets file, `buildTransitive/<id>.targets`, with no shared import:

- iOS, when `TargetPlatformIdentifier` is `ios`: a `NativeReference` to the package's xcframework
  with `ForceLoad` `true` and the frameworks below, space-separated. libc++ comes from the
  `clang++` link; `Kind` comes from the xcframework.

  | Package | Frameworks |
  | --- | --- |
  | `Jade.Native.Wgpu` | CoreFoundation, Foundation, IOSurface, Metal, QuartzCore |
  | `Jade.Native.Sdl` | AVFoundation, CoreBluetooth, CoreFoundation, CoreGraphics, CoreHaptics, CoreMedia, CoreMotion, CoreVideo, Foundation, GameController, Metal, OpenGLES, QuartzCore, UIKit |
  | `Jade.Native.MiniAudio` | AudioToolbox, AVFoundation, CoreFoundation, Foundation |

- The browser, when `RuntimeIdentifier` is `browser-wasm`: a `NativeFileReference` to the
  package's archive. `Jade.Native.Wgpu` appends to `EmccExtraLDFlags` the four `--js-library`
  options in Dawn's order and `--closure-args="--externs='<path>'"`, each path quoted for the
  response file.
- Minimum OS: each targets file holds the iOS and Android minimums of
  [0024](0024-minimum-os-versions.md) (`14.0`, `26.0`), which `Jade.Tests` compares with
  `build/versions.json`. A target that runs before `PrepareForBuild` fails with `JADENATIVE001`
  when `SupportedOSPlatformVersion` is lower, in an iOS application (`OutputType` `Exe`) or an
  Android application (`AndroidApplication` `true`). Libraries, other targets and a project
  without `SupportedOSPlatformVersion` are not checked.
- Windows, Linux, macOS and Android need no targets: NuGet's runtime assets place the libraries.

### Left to roadmap task 17

Resolving the P/Invokes of the interop assemblies against the statically linked iOS libraries
(exported symbols, `__Internal` or a resolver to the main program, `DirectPInvoke` under
NativeAOT) is not decided here (maintainer's decision, 2026-10-07): it touches the generator and
the interop projects and needs an iOS build to verify.

## Consequences

- The packages hold the 12 runtime identifiers. Declaring no target framework, they count as
  compatible with every framework; only .NET 11 applications use the interop that loads them. At `0.0.0-dev`, `Jade.Native.Wgpu` weighs about
  85 MB, `Jade.Native.Sdl` 16 MB and `Jade.Native.MiniAudio` 4 MB.
- Every run of the native workflow packs the packages, on every pull request: the packaging is
  validated without native builds when the inputs are unchanged, at the cost of a few minutes
  that depend on the artifacts of `main`.
- The `package` job lives in `native.yml`, a native build input: changing it rebuilds every
  runtime identifier, as any change to the workflow does.
- An iOS application links the natives, but the interop's P/Invokes do not reach them until task
  17; the framework lists are not verified by a link either. The browser link is verified, not its
  run in a browser (task 15). Android consumption follows the workload's sources and the minimum
  check was run against the workload's targets, but no APK was built here.
- An application below a minimum fails to build with a message that names the property to raise;
  an iOS project that sets its minimum only in `Info.plist` is not checked.
- Each package carries the notices of the three packages. Updating `THIRD-PARTY-NOTICES.md`
  updates all three.
- SDL3's macOS library ships as `libSDL3.dylib` with the install name `@rpath/libSDL3.0.dylib`
  (`llvm-objdump --macho --dylib-id`). .NET loads it by path, which ignores the install name; only
  an executable linked against it, such as a `net11.0-macos` application, which is not a target of
  [0012](0012-supported-targets.md), would look for `libSDL3.0.dylib`. Loading on macOS is not
  verified here.
- A file built identically by many runs has one attestation per run, and the verification reads
  the newest 100: an old run fetched with `--run` may fail to verify, the newest and the current
  ones do not.
- The interop packages do not depend on the native packages yet: the desktop sample (task 14),
  which runs from the packages, wires them.

## Alternatives considered

- **Packing from a local `build-native.cs` output**: no artifact to fetch, but single-architecture
  macOS libraries, no xcframework and the host's glibc.
- **A packaging job in `ci.yml`**: no native rebuild when it changes, but it cannot use the
  natives a pull request builds, so a pull request that changes the native inputs could not pack.
  **A separate workflow triggered by `workflow_run`**: not a pull request check.
- **Static archives and xcframeworks under `runtimes/`**: .NET for iOS would link them without
  `-force_load`, beside the `NativeReference`, and the browser build would treat them as runtime
  files. **A binding resource package**: it needs a managed assembly and a binding project.
- **Targets in `buildTransitive/net11.0/` with `lib/net11.0/_._`**, as NU5127 then asks: the
  packages would be restricted to .NET 11, at the cost of a placeholder file and of an empty
  `net11.0` dependency group (NU5128) instead of no dependency group.
- **Surfacing the minimums through a target framework with a platform version** (such as
  `net11.0-ios14.0`): that version is the SDK version an application compiles against
  (`TargetPlatformVersion`), not its minimum. **A warning** rather than an error: an application
  that ignores it fails at run time on the devices between the floors. **Raising
  `SupportedOSPlatformVersion` from the targets**: it would silently change the application's
  manifest. **Documentation only**: nothing would catch the mistake.
- **One notices file per package**, split from the root one: smaller, but three files to keep in
  step with `build/versions.json` and the build options instead of one.
- **Keeping `--source-digest` with the run's `head_sha`**: it rejects every pull request run, which
  the error message of `fetch-native.cs` recommended.
