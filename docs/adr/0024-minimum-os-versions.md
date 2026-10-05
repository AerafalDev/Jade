# 0024. Minimum operating system versions

- Status: Accepted
- Date: 2026-10-05

## Context

Jade ships natives for the RIDs of [0012](0012-supported-targets.md), which asks for minimum OS
versions derived from Dawn's actual requirements. Each family needs one: it is both the compile
target of the natives (deployment target, API level, glibc baseline) and the floor Jade claims to
support. Every component loaded in the process bounds it: Dawn, SDL3, miniaudio and the .NET 11
runtime itself.

Verified on 2026-10-05 in the sources pinned in `build/versions.json` and in .NET 11 RC 1:

- Dawn `v20261002.154047`:
  - `docs/support.md`: Vulkan 1.1 is required; D3D12 needs feature level 11.1, or 11.0 with
    resource binding tier 2; Metal 2.3 is required, macOS is supported from 13.0, iOS from 14.0
    "best effort"; Android is "work in progress"; OpenGL ES 3.1 through EGL is in progress.
  - `.github/workflows/ci.yml`, which builds the GitHub release binaries: macOS desktop with
    `MACOSX_DEPLOYMENT_TARGET` 13.0, the macOS universal static library with deployment target 12.0
    (also the default of `src/cmake/DawnCompilerPlatformFlags.cmake`), iOS with deployment target
    14.0, Android with `ANDROID_PLATFORM` `android-26` and NDK r27d, Linux in the
    `dockcross/manylinux_2_28` container "to ensure ABI compatibility with glibc 2.28", Windows with
    the 10.0.26100.0 SDK. `tools/android/webgpu/build.gradle` sets `minSdkVersion 26`.
  - Newer Apple APIs (macOS 12.0 to 26.0, iOS 16.0 to 26.0) are used behind `@available` checks,
    the D3D12 backend loads the system `d3d12.dll` at runtime, and older Windows 10 builds are
    handled at runtime (`IDXGIFactory6`, `EnqueueMakeResident`), so none of them raises the floor.
- SDL `release-3.4.18`: Windows "back to Windows XP"; macOS 10.13; iOS 11.0; Android API 21
  (`android-project` sets `minSdkVersion 21`). `src/core/windows/SDL_windows.h` targets Windows 10
  when D3D12 is enabled ("For D3D12, 0xA00 is required").
- miniaudio `0.11.25`: WASAPI from Windows Vista, AAudio from Android 8, OpenSL ES from API 16 as
  the fallback, Core Audio and Web Audio. Nothing above the Dawn and SDL3 floors.
- .NET 11 RC 1:
  - `release-notes/11.0/supported-os.md` of `dotnet/core` (2026-09-28): Windows 10 1607 (E) is the
    oldest Windows; glibc 2.27 for x64 and arm64; Red Hat Enterprise Linux 8 and Ubuntu 22.04 are
    the oldest of their distributions; macOS 15, 26 and 27, iOS 18, 26 and 27, and Android 14 to 17
    are the supported versions.
  - Build floors: `AndroidApiLevelMin` 24, `iOSVersionMin` 13.0 and `macOSVersionMin` 14.0 in
    `src/runtime/Directory.Build.props` of the VMR tag `v11.0.100-rc.1.26425.128`;
    `DOTNET_MIN_IOS_SDK_VERSION` 13.0 and `DOTNET_MIN_MACOS_SDK_VERSION` 14.0 in `Make.config` of
    `dotnet/macios` `dotnet-11.0.1xx-rc1-12193`; `AndroidMinimumSupportedApiLevel` 24 in the
    .NET for Android 37.0.0-rc.1.2257 workload.
- `pypa/manylinux` README: `manylinux_2_28` is based on AlmaLinux 8, with GCC 14, and has x86_64
  and aarch64 images.
- Emscripten 6.0.0 changelog: generated code supports Chrome 85, Firefox 79 and Safari 14.1 at
  least.

## Decision

Each minimum is the highest hard floor among Dawn, SDL3, miniaudio and .NET 11:

| Family | RIDs | Minimum | Set by |
| --- | --- | --- | --- |
| Windows | `win-x64`, `win-arm64` | Windows 10 version 1607 (build 14393) | .NET 11 supports nothing older; SDL3 targets Windows 10 when D3D12 is enabled |
| Linux | `linux-x64`, `linux-arm64` | glibc 2.28 | Dawn's release builds (`manylinux_2_28`, based on AlmaLinux 8); covers Red Hat Enterprise Linux 8, which .NET 11 supports |
| macOS | `osx-arm64`, `osx-x64` | macOS 14.0 | .NET 11 runtime |
| iOS | `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64` | iOS 14.0 | Dawn (Metal 2.3, its iOS deployment target) |
| Android | `android-arm64`, `android-x64` | API 26 (Android 8.0) | Dawn's Android build level; AAudio for miniaudio |
| Browser | `browser-wasm` | a browser that exposes WebGPU | no version number: WebGPU availability is the requirement |

- `build/versions.json` holds these values under `minimumOs`. The native builds use them as
  compile targets: `_WIN32_WINNT` 0x0A00 on Windows (the 1607 build is a support floor, not a
  compile setting), glibc 2.28 as the Linux build baseline, the macOS and iOS deployment targets,
  and `ANDROID_PLATFORM` `android-26`.
- GPU requirements are runtime conditions, not OS versions: a core WebGPU adapter needs Vulkan 1.1,
  D3D12 feature level 11.1 (or 11.0 with resource binding tier 2), or Metal 2.3. A machine that
  meets the OS minimum without such a driver gets no core adapter.
- Raising a minimum is a decision: it supersedes this record. A dependency or SDK update that would
  raise one is not a routine bump.

## Consequences

- The minimums are above .NET 11's floors on iOS (14.0 against 13.0) and Android (API 26 against
  24), so applications must set their own minimum at least as high; the samples do, and the
  packages surface it ([0011](0011-native-package-layout.md), roadmap task 11).
- The floors are wider than what can be tested: .NET 11 itself only lists macOS 15, iOS 18 and
  Android 14 onwards, and CI runs what the runners and simulators offer. Versions between a floor
  and the oldest tested version are supported but reported as not verified.
- Jade inherits Dawn's support levels: iOS is "best effort" and Android "work in progress" upstream.
- Linux natives need a glibc 2.28 build environment, a container or a sysroot, chosen with the
  native CI (roadmap task 10), which also settles how the C++ runtime is linked.
- The browser has no version floor to publish; the engine must detect a missing `navigator.gpu`
  at startup, which the asynchronous initialization path already handles
  ([0025](0025-browser-natives-with-workload-emscripten.md)).
- The supported OS list can now be published, once natives ship in the packages (roadmap
  task 19).

## Alternatives considered

- **The versions .NET 11 supports** (macOS 15, iOS 18, Android 14 / API 34): Jade would only claim
  what .NET tests, but would exclude devices that run every component.
- **.NET 11's own floors** (iOS 13.0, Android API 24): below Dawn's Metal 2.3 floor on iOS and below
  the level Dawn builds for on Android.
- **glibc 2.27**, the .NET floor: it only matters for Ubuntu 18.04, which .NET 11 no longer lists,
  and Dawn's own builds target 2.28.
- **glibc 2.35** (Ubuntu 22.04): an easier build environment, but drops Red Hat Enterprise Linux 8,
  which .NET 11 supports.
- **Dawn's macOS floor (13.0)**: .NET 11 does not run there.
