# 0038. Native CI: runners, build environments, triggers and artifacts

- Status: Accepted
- Date: 2026-10-06

## Context

[0010](0010-native-builds-with-xmake.md) has CI build the natives on a matrix of runners and
publish them as artifacts that `scripts/fetch-native.cs` downloads, with provenance attestations
([0037](0037-github-repository-baseline.md)). [0031](0031-native-build-definitions.md) defined the
build and its host build for `linux-x64`; [0022](0022-ci-runners-and-caching.md) left the runners,
the frequency and the caching of the native builds to roadmap task 10, which also settles the
glibc 2.28 environment and the linking of the C++ runtime that
[0024](0024-minimum-os-versions.md) asks for. The layout libraries of
[0036](0036-generated-layout-tests.md) are built for every target as test-only artifacts; running
the layout tests there is roadmap task 18.

Verified on 2026-10-05 and 2026-10-06:

- GitHub documentation: standard GitHub-hosted runners are free for public repositories, Linux
  and Windows arm64 included (`ubuntu-24.04-arm`, `windows-11-arm`: 4 vCPUs, 16 GB); `macos-26` is
  arm64 with 3 vCPUs and 7 GB. Artifacts are kept 90 days by default, and 1 to 90 days in a public
  repository. A job skipped by its condition reports success and does not block a required check,
  while a workflow skipped by path filtering leaves its required checks pending. Events triggered
  with `GITHUB_TOKEN` start no workflow run, except `workflow_dispatch` and `repository_dispatch`,
  and pull request events wait for an approval.
- Runner images (`actions/runner-images` and `actions/partner-runner-images` READMEs):
  `ubuntu-24.04` has NDK `28.2.13676358` under `$ANDROID_HOME/ndk/` (its default NDK is another
  one), Docker and Ninja; `ubuntu-24.04-arm` has Docker; `windows-2025` and `windows-11-arm` have
  Visual Studio 2022 17.14 with the ARM64 tools, the Windows SDK 10.0.26100, Ninja, Python 3, git
  and jq; `macos-26` has Xcode 26.6 by default, with the iOS 26.5 SDK.
- `quay.io/pypa/manylinux_2_28_x86_64` and `_aarch64`, tag `2026.10.03-1`, the newest: AlmaLinux
  8.10, glibc 2.28, GCC 14.2.1 from `gcc-toolset-14`, no Ninja. Its repositories hold the
  development packages of every SDL3 feature that `build/sdl/xmake.lua` requires on Linux except
  two: `libdecor-devel` does not exist, and liburing is 1.0.7 while SDL3 looks for
  `liburing-ffi` (`CMakeLists.txt` and `cmake/sdlchecks.cmake` at `release-3.4.18`); SDL3 loads
  both at runtime and needs their headers, pkg-config files and SONAME to build.
- xmake 3.1.1: the release has no Linux arm64 binary. Its macOS bundle keeps the scripts inside
  the executable, and xmake's Xcode toolchain uses `scripts/gas-preprocessor.pl` as the iOS device
  assembler, a program that must exist as a file: the `ios-arm64` build failed with "cannot get
  program for as" (`core/tool/tool.lua`), and the source archive builds and installs on Linux and
  macOS. `xmake config` stores the build directory relative to the project directory, and with
  `--project` xmake reads it back relative to the working directory (`config.builddir` in
  `core/project/config.lua`), which put the build outside the repository. `package.tools.cmake`
  selects the simulator SDK for `x86_64` only, and gives every CMake build on Windows one
  directory for the compilers' PDB files: DXC compiles its Release build with `/Zi` unless
  `CMAKE_MSVC_DEBUG_INFORMATION_FORMAT` is set (`cmake/modules/HandleLLVMOptions.cmake` at
  `9757d44`), and its parallel `cl.exe` processes failed on the shared `vc140.pdb` (C1041), with
  `/FS` as well. Dawn and DXC require CMake versions older than CMP0141, so CMake adds no flag of its
  own for that variable. A file's
  `sourcekind` of `mm` does not compile a `.c` file as Objective-C: xmake only adds `-x` for C and
  C++ (`add_sourceflags` in `modules/core/tools/gcc.lua`).
- The workload's Emscripten, run outside MSBuild: the pack's `.emscripten` reads
  `DOTNET_EMSCRIPTEN_LLVM_ROOT`, `DOTNET_EMSCRIPTEN_BINARYEN_ROOT` and `DOTNET_EMSCRIPTEN_NODE_JS`,
  as `BrowserWasmApp.targets` sets them; the Cache pack's `sanity.txt` names the LLVM directory of
  the machine that built it, and `check_sanity` in `tools/shared.py` erases a writable cache whose
  sanity file differs, unless `EM_IGNORE_SANITY` is set. With them, `emcc` 6.0.3 built
  Emdawnwebgpu, SDL3, miniaudio and the layout libraries, locally and in CI. The workload set
  `11.0.100-rc.1.26460.1` selects the same Emscripten manifest as the SDK's own.
- Dawn `b1236a9`: Emdawnwebgpu is the CMake target `emdawnwebgpu_c` of a build with Emscripten's
  toolchain file, and its link needs four JavaScript libraries and the Closure externs
  (`src/emdawnwebgpu/CMakeLists.txt`); its archive also holds 46 C++-mangled functions, such as
  `wgpuAdapterSetLabel`. `DAWN_USE_BUILT_DXC` builds `dxcompiler.dll` but the install rules leave
  it in the build directory. Windows needs the DXC entry of `DEPS` without its submodules, as
  Dawn's own fetch script clones it, and `third_party/directx-headers` only serves other hosts. The
  longest path of the pinned DXC is 133 characters, past `MAX_PATH` from the source cache. Abseil
  replaces `CMAKE_MSVC_RUNTIME_LIBRARY` with the DLL runtime unless `ABSL_MSVC_STATIC_RUNTIME` is
  on (`third_party/abseil-cpp/CMakeLists.txt`); DXC's LLVM takes its CRT from the Release flags.
- In the glibc 2.28 container, the libraries require at most `GLIBC_2.27`, on both
  architectures; only Dawn depends on `libstdc++.so.6` (`GLIBCXX_3.4.22`), and the layout
  libraries on nothing. The tests pass on CachyOS with the `linux-x64` ones. Run as the
  container's PID 1, .NET receives the build's orphaned processes, and its reaping of its own
  children failed with `ECHILD` ("Error while reaping child. errno = 10") on `linux-arm64` and
  hung on `linux-x64`; `docker run --init` avoids it. The runner's Ninja 1.13 rejects the deps
  log of the container's Ninja 1.8.2.
- SDL3 versions its ELF symbols (`SDL_Init@@SDL3_0.0.0`), as `llvm-nm --dynamic` lists them.
- Durations on the standard runners, Dawn included: the browser about 2 minutes, macOS, iOS,
  Linux and Android 13 to 21 minutes per job, Windows about an hour, DXC doubling the steps of
  Dawn's build (2,446 against 1,269 on Linux).

## Decision

### Runners

| Runtime identifiers | Runner | How |
| --- | --- | --- |
| `linux-x64`, `linux-arm64` | `ubuntu-24.04`, `ubuntu-24.04-arm` | In the glibc 2.28 container of `build/linux/Dockerfile` |
| `win-x64`, `win-arm64` | `windows-2025`, `windows-11-arm` | Natively, with MSVC; DXC built for its own architecture |
| `osx-arm64`, `osx-x64` | `macos-26` | One job per architecture, then `lipo` into universal libraries |
| `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64` | `macos-26` | One job per slice, then `lipo` for the simulator and one xcframework per library |
| `android-arm64`, `android-x64` | `ubuntu-24.04` | The image's NDK, checked by revision |
| `browser-wasm` | `ubuntu-24.04` | The workload's Emscripten, with the workload set of `build/versions.json` |

- Only standard GitHub-hosted runners, with pinned images ([0022](0022-ci-runners-and-caching.md));
  Xcode is pinned with `DEVELOPER_DIR`.
- `build-native.cs --rid <rid>` builds any target its host can build: Linux and Windows on their
  own platform and architecture, the Apple targets on macOS, Android and the browser anywhere
  their toolchains run. Its default stays the host's runtime identifier.

### Build environments and tools

- The Linux libraries are built in `build/linux/Dockerfile`: the `manylinux_2_28` image pinned by
  digest for each architecture, the development packages of SDL3's features, and libdecor 0.2.5
  and liburing 2.15 built from their release commits, for SDL3's build only. The container runs as
  the runner's user with `--init`, so xmake never runs as root and .NET is never PID 1, and
  mounts the runner's .NET SDK.
- `build-native.cs --install-tools` installs the xmake and CMake of `build/versions.json` into
  `artifacts/native/tools/`, from release archives whose SHA-256 the file pins, and puts them first
  on `PATH`. Linux and macOS build xmake from its source archive. CI always installs them; a local
  build may use its own, whose versions are checked either way.
- xmake runs in the project directory, so that its build directory stays under
  `artifacts/native/obj/<rid>/`.
- The browser build finds the workload's packs in the .NET installation that runs it, checks that
  their names and `emcc --version` match `toolchains.emscripten`, and builds with a writable copy
  of the Cache pack and `EM_IGNORE_SANITY`. It uses the pack's own `.emscripten` through the
  `DOTNET_EMSCRIPTEN_*` variables rather than the separate `EM_CONFIG` that
  [0025](0025-browser-natives-with-workload-emscripten.md) foresaw. CI installs the workload with
  the pinned `workloadVersion`.
- Android: the NDK is found by the revision in its `source.properties`, never by its path.
- `build-native.cs` checks the exports of the libraries it can load in its own process, and reads
  the others with `nm` (the NDK's or the workload's `llvm-nm`, `xcrun nm`), symbol versions
  stripped. It lists the source directories that Ninja recorded for the CMake builds, with the
  Ninja that ran them, for the check of `THIRD-PARTY-NOTICES.md`; a failed package build shows
  xmake's install log in CI.

### Compile targets and runtimes

- Minimum OS versions of [0024](0024-minimum-os-versions.md): `_WIN32_WINNT` 0x0A00 for the
  targets of the project (Dawn and DXC keep the Windows SDK's default, and SDL3 sets 0x0A00 itself
  with D3D12), the macOS and iOS deployment targets through xmake's `--target_minver` and
  `CMAKE_OSX_DEPLOYMENT_TARGET`, `ANDROID_PLATFORM` 26 through `--ndk_sdkver`, and glibc 2.28 as
  the container's own.
- Nothing has to be installed beside the libraries: Windows uses the static CRT (`/MT`) for Dawn,
  DXC, SDL3 and miniaudio, abseil included, and DXC writes no PDB while compiling; Linux links the system's `libstdc++` through `gcc-toolset-14`, as
  manylinux does, and .NET depends on it already; Android links `c++_static` into Dawn, the only
  C++ library, whose API is C; Apple platforms use the system's libc++; in the browser the
  application's link brings Emscripten's.
- iOS and the browser get static archives: one xcframework per library on iOS
  (`webgpu_dawn.xcframework`, `SDL3.xcframework`, `miniaudio.xcframework`) with the device slice
  and a universal simulator slice; archives named after the imported module in the browser
  ([0025](0025-browser-natives-with-workload-emscripten.md)), with Emdawnwebgpu's JavaScript
  libraries and externs beside `webgpu_dawn.a`. miniaudio is compiled as Objective-C for iOS
  (`-xobjective-c`). The layout libraries follow the same forms.
- Warnings stay errors everywhere except in one third-party file: with MSVC, `miniaudio.c` is
  compiled with `/WX-`, since MSVC warns in miniaudio's embedded dr_wav (C4244, an implicit
  `ma_uint64` to `ma_uint32` in `miniaudio.h`) where GCC and Clang do not. Its warnings stay in the
  log; the shim keeps `/WX`, and the other targets keep `-Werror` on both files (maintainer's
  decision).
- Per-platform lists of required SDL3 features in `build/sdl/xmake.lua`: each holds the features
  that depend on a detection there (development packages, SDK headers or frameworks).

### Workflow, frequency and caching

- `.github/workflows/native.yml` runs on every pull request and push to `main`, monthly, and on
  demand. A `changes` job compares the native build inputs (`build/`, `scripts/build-native.cs`,
  `scripts/build-native/`, `global.json` and the workflow itself) with the base; the builds run
  only when they differ, or on a schedule or a manual run. The `natives` job reports the outcome
  and is a required check: skipped builds pass it, a failed or cancelled one fails it.
- The monthly run keeps artifacts on `main` younger than their 90-day retention.
- No cache: a build runs only when its inputs change, and every attested file is compiled in the
  run that attests it.

### Artifacts and attestations

- Each target has two artifacts, the shipped libraries (`native-<name>`) and the layout libraries
  (`native-test-<name>`), where the name is the runtime identifier, `osx` for both macOS ones and
  `ios` for the three iOS ones. The single-architecture Apple builds are intermediate artifacts
  kept one day and not attested.
- Every file of every artifact gets a SLSA build provenance attestation (`actions/attest`) in the
  job that produces it. Pull requests from forks get no OIDC token and are not attested.
- Built binaries are never committed to the repository: the artifacts of the runs are the store.

### `fetch-native.cs`

- `dotnet run scripts/fetch-native.cs [--rid <rid>]... [--run <id>]` installs the artifacts of a
  runtime identifier, the host's by default, into `artifacts/native/bin/<rid>/` and
  `artifacts/native/test/<rid>/`, where the tests find them.
- Without `--run`, it takes the newest successful run of `main` whose commit has the checkout's
  native build inputs, uncommitted changes included, and that still has the artifacts; with
  `--run`, that run, under the same condition.
- It uses the GitHub CLI, which handles authentication, and verifies every file with
  `gh attestation verify`, for the native workflow, the run's commit and GitHub-hosted runners,
  before it replaces anything.

### CodeQL

- CodeQL analyzes the C shim (`build/miniaudio/jade_miniaudio.c`) as `analyze (c-cpp)`, compiled
  alone against miniaudio fetched outside the checkout at its pinned commit. It is a required
  check, like the other analyses.

## Consequences

- A change to `build/` or to the native scripts costs one native run, about an hour for the
  Windows jobs and 20 minutes for the others, on the pull request and again on `main`; other pull
  requests pay the `changes` job only.
- The ruleset requires `natives` and `analyze (c-cpp)` ([0037](0037-github-repository-baseline.md)).
- Contributors can work against the CI's natives without building them, with the GitHub CLI
  signed in; a branch that changes the inputs builds its own, locally or through its pull
  request's run.
- `build/linux/Dockerfile` pins two image digests, libdecor and liburing; Dependabot does not
  update them. A newer manylinux image or libdecor is a deliberate change of the inputs, which
  rebuilds everything.
- The minimum OS floors of [0024](0024-minimum-os-versions.md) are compile settings, not tested:
  CI runs no test on the other targets until roadmap task 18.
- iOS applications must link libc++ and the frameworks that SDL3, Dawn and miniaudio need, since
  the archives are static (roadmap task 11); Android ships no `libc++_shared.so`.
- Emdawnwebgpu's C++-mangled functions are not callable from C#; the browser part of the
  `Jade.Wgpu` raw layer must be checked against the archive's exports (roadmap tasks 15 and 18).
- Using the workload's Emscripten outside MSBuild depends on the pack layout and on
  `EM_IGNORE_SANITY`; an SDK update re-checks both ([0025](0025-browser-natives-with-workload-emscripten.md)).

## Alternatives considered

- **Cross-compiling Linux and Windows arm64 from x64 runners**: fewer runner types, but a glibc
  2.28 aarch64 sysroot with SDL3's development packages, and DXC's LLVM host tools on Windows.
- **Building on every pull request**: more than an hour per pull request, documentation ones
  included.
- **Building on `main` and on demand only**: a broken native build would be found after its merge.
- **A path-filtered workflow**: simpler, but it cannot be a required check.
- **A compiler cache (ccache, sccache)** or **xmake's installed packages in `actions/cache`**:
  faster rebuilds, at the price of a cache to trust and of attested files that a run did not
  compile.
- **Committing the binaries to `native/`**: several hundred megabytes per update kept forever in
  the history, files over GitHub's 100 MB limit for the iOS archives, and commits from CI that
  start no workflow with `GITHUB_TOKEN`.
- **A GitHub release per set of inputs**: permanent storage, but releases written by CI and a
  cluttered release page.
- **Dropping libdecor and liburing from the Linux features**: a simpler environment, but no client
  decorations on GNOME's Wayland and no io_uring in SDL3.
- **Dynamic frameworks on iOS** and **shared C++ runtimes** (`/MD`, `c++_shared`): each would add
  a file to embed, sign or install with every application.
