# 103: Desktop RID matrix in CI

- Depends on: 102, 002, 003
- ADRs: 0003, 0004, 0007

## Goal

A `native.yml` workflow builds `jade_native` for win-x64, win-arm64, linux-x64, linux-arm64,
osx-x64 and osx-arm64. It caches by input hash and uploads one artifact per RID. Linux binaries
run on an old glibc baseline. `scripts/fetch-native.cs` lets managed-only work use those artifacts.

## Context

- Free arm64 runners `windows-11-arm` and `ubuntu-24.04-arm` (also `ubuntu-22.04-arm`) work only on
  public repositories, with about 14 GB of disk. 102 reports Dawn's disk footprint.
- Check the current macOS runner labels. If no Intel macOS runner is available, cross-compile
  osx-x64 from an arm64 runner, or build a universal library and split it. Choose and document.
- The repository is public at github.com/AerafalDev/Jade. Pushing is still the user's call: write
  the workflow and verify everything locally that can be verified.
- From 101:
  - The `jade.bundle` rule (`native/rules/bundle.lua`) raises on Windows: its export control still
    has to be written. A `.def` file has no wildcards, so generate it before linking from the
    archives' external symbols matched against the export list.
  - The macOS path (`-force_load`, `-exported_symbols_list`) is written but untested.
  - The package recipes' `on_install` accepts `linux` only. Other platforms need their own system
    libraries and frameworks.
  - `--runtimes` is not passed yet; ADR-0003 asks for `/MT` on Windows.
  - Release builds are stripped. Keeping debug symbols needs a non-stripping release
    (`build.release.strip`) plus a separate strip step.
  - xmake caches packages under `~/.xmake` (or `XMAKE_PKG_INSTALLDIR`/`XMAKE_PKG_CACHEDIR`). Old
    installs pile up there, one per recipe hash.
  - The host build needs `GLIBC_2.43` and pulls `__isoc23_*` symbols from the host headers: the
    old-glibc container is what fixes this.
  - SDL silently drops a video backend whose development headers are missing. The container needs
    X11 (with Xext, Xcursor, Xi, Xfixes, Xrandr, Xss), Wayland, xkbcommon, libdecor, EGL, D-Bus and
    udev headers (ibus optional). Assert the backends with the video driver line printed by
    `scripts/smoke-native.cs`.
- From 002: the CI job sets `MSBuildTreatWarningsAsErrors`; reuse it. setup-dotnet's NuGet cache
  needs lock files (`RestorePackagesWithLockFile`) or a `cache-dependency-path`.
- From 102:
  - Dawn's build needs Python 3 and git (its code generator and dependency fetch) and the X11 and
    Wayland headers (`DAWN_USE_X11`, `DAWN_USE_WAYLAND`).
  - `XMAKE_PKG_CACHEDIR` holds `dawn-deps/<commit>/` (123 MB). Old Dawn commits pile up there, as
    old installs do.
  - Disk peak: 746 MB in release and 3.4 GB in debug, mostly Dawn's 491 MB source tree.
    xmake's `url_excludes` might skip Dawn's tests and CTS on extraction; not investigated.
  - The host build reaches `GLIBC_2.44` (`cosh`/`sinh` from libm) and
    `_dl_find_object@GLIBC_2.35` (host static libgcc). The baseline container fixes both.
  - macOS has no `--gc-sections` equivalent wired yet (`-dead_strip`, unverified).
  - Windows needs its own Dawn options (D3D12, maybe D3D11, DXC?) and dependency list.
  - The library embeds 250 absolute build paths (`__FILE__` in Dawn and Abseil, under the builder's
    home directory), which also makes its size depend on the cache location. Fix with
    `-ffile-prefix-map` (or `-fmacro-prefix-map`) on the packages, and check with `strings`.
  - Changing `XMAKE_PKG_INSTALLDIR` between runs breaks `--require=y` until `native/.xmake` is
    deleted. CI sets it on every run, so only local switching is affected; document it.
- From 201: the `bindings` job in `ci.yml` builds jade_native itself, which now includes Dawn.
  Switch it to the staged inputs from `native.yml` (through `scripts/fetch-native.cs` or the
  artifact action). Also run the generator on Windows and macOS hosts once. It cannot run on Intel
  macOS: libclang's native package has no osx-x64 build since 18.1.3.
- From 102 (CI fix): the `bindings` job now builds Dawn on every run, about 18 minutes, cold, on
  `ubuntu-latest`. Cutting that is the first deliverable of this task: an xmake package cache, or
  the `bindings` job consuming `native.yml` artifacts. Ninja is a prerequisite: xmake 3.1.1 builds
  CMake packages with it. Dawn needs `X11/Xlib-xcb.h`, from `libx11-xcb-dev` on Ubuntu. Reuse the
  job's step that prints and uploads `installdir.failed/logs/*.txt` on failure: xmake prints only
  the first 17 lines of a failed package install.
- From 003: scripts are compiled and style-checked only on `ubuntu-latest` (the `style` job), but
  they must run on Windows and macOS too (ADR-0008). The native jobs of this task run
  `build-native.cs` on every desktop OS. Also compile every script there.
- Docs-only PRs (only `design/**` or `*.md`) currently run the whole CI, Dawn included. Skip the
  native and bindings jobs for them (`paths` filters, or a changes check if required checks must
  still report), so orchestrator PRs do not cost 18 minutes.
- From 201: the `bindings` job installs SDL3's Linux build dependencies (the list in SDL's
  `docs/README-linux.md` at the pinned tag) plus `python3`. The old-glibc container needs the
  equivalent packages for its distribution, or SDL loses video backends. Keep one source for that
  list instead of two copies drifting apart.

## Scope

- `.github/workflows/native.yml`: matrix over the six desktop RIDs, each running
  `dotnet scripts/build-native.cs --rid <rid>`. The cache key hashes `native/**`,
  `scripts/build-native.cs`, `scripts/build-native/**` and toolchain identifiers. The workflow uploads
  `artifacts/native/<rid>/` and keeps debug symbols as a separate artifact.
- Linux glibc baseline: choose the oldest glibc on which Dawn builds with a reasonable toolchain,
  inside a container. Write a new ADR (Proposed) with the choice and the reason. Verify it with the
  highest `GLIBC_` version in `objdump -T`.
- Windows: static CRT (`/MT`) if Dawn and SDL3 allow it. Export mechanism finished for MSVC (from
  101). D3D12 backend; D3D11 only if Dawn needs it as a fallback.
- macOS: deployment target chosen and recorded; Metal backend; export list finished.
- `scripts/fetch-native.cs`: downloads the latest successful `native.yml` artifacts for given RIDs
  into `artifacts/native/` with `gh run download`. It fails clearly when `gh` is missing or
  unauthenticated.
- `versions.json` (from 101) also records toolchain versions.

## Out of scope

- Mobile (104), browser (105), packaging (106).

## Acceptance criteria

- [ ] linux-x64 builds locally through the same container path CI uses, and the glibc check passes.
- [ ] The workflow is complete for all six RIDs, and the local runs that are possible are done.
- [ ] The Outcome states plainly which RIDs were built and verified, and which only have a
      workflow written.
- [ ] The glibc baseline ADR is written as Proposed.

## Verification

Run the local container build for linux-x64, then `objdump -T ... | grep -o 'GLIBC_[0-9.]*' | sort -V
| tail -1`, then `dotnet scripts/smoke-native.cs`. Run `actionlint` (installed) on the workflow.

## Pitfalls

- The container toolchain must support Dawn's C++ standard on an old glibc. A newer clang on an old
  distribution is the usual answer.
- On arm64 runners, the 14 GB disk can run out with Dawn sources, the build tree and the xmake
  cache together. Clean intermediate trees in the job if 102's numbers are close.

## Outcome

- Summary: `.github/workflows/native.yml` builds jade_native for the six desktop RIDs, each on a
  runner of its own OS and architecture, caches xmake's package builds by input hash, and uploads
  `jade-native-<rid>` (the `artifacts/native/<rid>/` layout) and `jade-native-symbols-<rid>`. Linux
  RIDs build inside an AlmaLinux 8 container, glibc 2.28 (`native/linux/Dockerfile`, ADR-0013,
  Proposed), through `dotnet scripts/build-native.cs --rid <rid> --container`. Windows exports
  through a generated `.def` with the static CRT and D3D12; macOS targets 12.0 with Metal. The
  bindings drift check and the tests against jade_native moved from `ci.yml` into `native.yml`, on
  Linux, Windows and macOS hosts. `scripts/fetch-native.cs` downloads CI's artifacts.
  `versions.json` records the toolchain. All six RIDs are built and verified in CI (run
  37087761604, commit `68e136f`); only linux-x64 was also built locally.
- Verification (commands and results):
  - Local, linux-x64 through the container (CachyOS host, 16 cores, Docker 29.8.2):
    - `dotnet scripts/build-native.cs --rid linux-x64 --container --prune-packages`, image already
      built, empty package cache: 166.4 s, staging after 151.8 s. Building the image takes about
      3 minutes (3 min 08 s for the final Dockerfile); `docker images` reports 3.08 GB. A rerun
      without changes: 1.4 s, xmake `build ok, spent 0.033s`.
    - `objdump -T artifacts/native/linux-x64/lib/libjade_native.so | grep -o 'GLIBC_[0-9.]*' |
      sort -V | tail -1`: `GLIBC_2.27` (also in CI, x64 and arm64). `readelf -d`: NEEDED
      `librt.so.1`, `libdl.so.2`, `libpthread.so.0`, `libm.so.6`, `libc.so.6`,
      `ld-linux-x86-64.so.2`; no libstdc++ or libgcc_s.
    - `nm -D --defined-only`: 1270 `SDL_*`, 1179 `ma_*`, 276 `wgpu*`, 1 `jade_*`, as in 102.
    - `strings -n 8` finds no absolute build path (0 matches for `/jade`, `/home/`, `xmake`); Dawn's
      logs now cite `dawn/src/dawn/native/vulkan/BackendVk.cpp:368`.
    - Symbols: `artifacts/native-symbols/linux-x64/jade_native.sym` (5,478,000 bytes), and the
      11,879,608-byte library has a `.gnu_debuglink`.
    - `dotnet scripts/smoke-native.cs` passes on the host (glibc 2.44, Vulkan adapter `AMD Radeon RX
      7900 XTX`) and inside the container (glibc 2.28: no Vulkan there, so the default request
      completes with the Null adapter). The video driver line, now checked, is `wayland, x11, kmsdrm,
      offscreen, dummy, evdev`. The binary names the runtime-loaded `libdecor-0.so.0`,
      `libwayland-client.so.0`, `libxkbcommon.so.0`, `libX11.so.6`, Xext, Xcursor, Xi, Xfixes, Xrandr,
      Xss, `libEGL.so.1`, `libdbus-1.so.3` and `libudev.so.1`, and contains the IBus support that the
      CachyOS host build of 101 lacked.
    - Pruning: a copy of the SDL3 install under another hash was removed by `--prune-packages`; the
      three used installs stayed.
    - The host path still works: `dotnet scripts/build-native.cs` rebuilt the edited recipes in 95 s,
      `GLIBC_2.44` (host glibc, as expected), smoke check passing, no absolute path.
    - `dotnet build -c Release`: 0 warnings. `SDL_VIDEO_DRIVER=dummy dotnet test -c Release`: 19/19
      against the container build, and 19/19 against the library fetched from CI.
    - Scripts: `dotnet build scripts/<name>.cs` for all four, 0 warnings; the style check of
      CLAUDE.md on the converted `build-native`, `smoke-native` and `fetch-native` reports nothing.
      `actionlint` passes on both workflows.
    - `dotnet scripts/fetch-native.cs --run 37087761604 --rid linux-x64 --rid win-x64` downloaded both
      in 6.9 s; the smoke check passes on the fetched linux-x64. Errors, exit code 1: gh not on PATH
      (`cannot start gh ... Install the GitHub CLI`), not signed in (`gh is not authenticated`), no
      `native.yml` on the default branch yet (gh's HTTP 404, the case without `--run` until this
      merges), unknown artifact (`no artifact matches`).
  - CI, run 37087761604 (all six green). Staged sizes: linux-x64 11601 KiB, linux-arm64 10677 KiB,
    win-x64 9903 KiB, win-arm64 10127 KiB, osx-x64 11564 KiB, osx-arm64 10574 KiB.
    - Every RID: smoke check on its own OS, with the expected video drivers (`windows, offscreen,
      dummy` and `cocoa, offscreen, dummy` elsewhere) and a default adapter: Null on the Linux runners
      (no Vulkan driver), `Microsoft Basic Render Driver (D3D12)` on both Windows,
      `Apple Paravirtual device (Metal)` on both macOS.
    - linux-x64 and linux-arm64: `GLIBC_2.27` check passes. win-x64 and osx-arm64 hosts: the
      generator produced the committed bindings (first run of it on Windows and macOS), and
      `dotnet test -c Release` passed 19/19 on Windows and 18/19 on macOS (the window test skips there
      by design). linux-x64 did the same with 19/19.
    - Exports: macOS 1270 `SDL_`, 1178 `ma_`, 276 `wgpu`, 1 `jade_` (`nm -gU`, both RIDs);
      win-arm64 (`llvm-readobj --coff-exports` on the artifact) 1270, 1182, 276, 1. Windows adds the
      Windows-only `ma_strcmp_WCHAR`, `ma_strcpy_s_WCHAR` and `ma_strlen_WCHAR`; macOS lacks
      `ma_atomic_global_lock` (see Follow-ups). The Windows DLL imports only system DLLs (ADVAPI32,
      GDI32, IMM32, KERNEL32, ole32, OLEAUT32, SETUPAPI, SHELL32, USER32, VERSION, WINMM and two
      `api-ms-win-core` sets): no MSVC runtime DLL. The macOS dylib loads system frameworks,
      `libc++.1`, `libz.1`, `libiconv.2` and `libSystem.B`. Both macOS libraries report `minos 12.0`.
    - Symbols: `jade_native.pdb` 18.5 MB (win-x64) and 17.9 MB (win-arm64), `jade_native.dSYM`
      3.3 MB (osx-arm64), `jade_native.sym` 5.5 MB (linux-x64) and 13.9 MB (linux-arm64).
    - Toolchains, from `versions.json`: clang 21.1.8 and glibc 2.28 (Linux); MSVC toolset 14.51.36231
      (`cl` 19.51.36260, Visual Studio 2026 as xmake finds it, although the runner documentation
      lists 2022 for both images) and Windows SDK 10.0.26100.0; Xcode 26.6 (17F113), SDK 26.5,
      Apple clang 21.0.0.
    - Times. Cold (empty package cache): linux-x64 671 s for the build step (image included),
      linux-arm64 716 s, win-x64 1010 s, win-arm64 952 s, osx-arm64 470 s, osx-x64 1501 s (plus
      293 s to build xmake). Warm (only `rules/bundle.lua` changed): whole jobs of 118 s (win-x64),
      133 s (win-arm64), 171 s (linux-arm64) and 288 s (linux-x64, bindings and tests included),
      where building the image takes most of the Linux time. The `bindings` job this replaces took
      about 18 minutes cold on every run.
    - Partial restore (run 37084574252, after a recipe edit): win-x64 restored the previous key and
      reinstalled only `sdl3` and `dawn`; `miniaudio` was reused.
    - `ci.yml` (build, test and pack on three OSes, code style) and CodeQL (C# and Actions) pass.
- Decisions taken (and ADRs added):
  - ADR-0013 (Proposed): Linux RIDs build in `almalinux:8.10` (pinned by digest), glibc 2.28, just
    above .NET 10's own floor of 2.27. Toolchain pinned in the Dockerfile: clang 21.1.8, which RHEL's
    packaging points at gcc-toolset-15 (GCC 15.2.1's libstdc++ with a complete `libstdc++.a`, binutils
    2.44), CMake 3.26.5, Ninja 1.8.2. Headers: SDL's Fedora list as EL8 provides it, plus libdecor
    0.2.5 built from its release archive (SHA-256 pinned). xmake 3.1.1 is built from its release
    archive (SHA-256 pinned; there is no Linux arm64 binary), and the .NET SDK comes from
    `dotnet-install.sh` at global.json's version. The ADR lists what SDL loses against Wayland 1.21,
    xkbcommon 0.9.1 and PipeWire 0.3.6.
  - Container path: `build-native.cs --container` tags the image with a hash of the Dockerfile, the
    SDK version and the platform, builds it only when missing, then runs `dotnet
    scripts/build-native.cs` inside with the repository at `/jade`. The container's build tree, xmake
    configuration and home (xmake packages, NuGet) live in `native/build/container/<rid>/`, mounted
    over `native/build`, `native/.xmake` and `/home/jade`, so container and host builds never share
    packages, and the inner run is an ordinary build. It runs as the caller's uid:gid (rootful Docker).
  - Runners: `ubuntu-24.04`, `ubuntu-24.04-arm`, `windows-2025`, `windows-11-arm`, `macos-26-intel`
    and `macos-26`. Intel macOS runners still exist, so osx-x64 builds natively: no cross-compilation
    and no universal library.
  - Cache: `actions/cache` restore and save of xmake's package installs. Key: RID, toolchain identity
    (`container`, whose toolchain the Dockerfile pins; `msvc-<VCToolsVersion>-sdk-<version>`;
    `xcode-<version>-<build>`), hash of `scripts/build-native/Rids.cs`, then the hash of `native/**`
    (minus `native/build` and `native/.xmake`), `build-native.cs` and `build-native/**`. Restore keys
    stop before the last hash. `Rids.cs` sits in the prefix because xmake's package build hash ignores
    flags such as `--target_minver`. The cache is saved right after a successful build, before the
    checks. `--prune-packages` runs `xmake require --clean --clean_modes=package` so restored caches do
    not accumulate old installs.
  - Windows export control: in `jade.bundle`, exact names go to the `.def`; each `*` pattern is
    expanded against the defined globals of its own package's archives with xmake's
    `core.base.binutils.readsyms`, with `DATA` for non-code symbols, and fails when it matches nothing.
    Plus `/WHOLEARCHIVE:` per archive, and `/OPT:REF /OPT:ICF` in release (`/DEBUG` disables them).
    The shims export through `JADE_API`.
  - Static CRT: `build-native.cs` passes `--runtimes=MT` (`MTd` in debug) on Windows; xmake hands it to
    every package. Abseil forces the DLL runtime unless `ABSL_MSVC_STATIC_RUNTIME=ON`, which the Dawn
    recipe sets.
  - Dawn per platform: Vulkan, X11 and Wayland on Linux; D3D12 with the HLSL writer on Windows (D3D11
    off: not needed as a fallback; Windows UI off; built DXC and Agility SDK off;
    `DAWN_FORCE_SYSTEM_COMPONENT_LOAD=OFF`, so FXC's `d3dcompiler_47.dll` loads from the library's
    directory, then the system's); Metal with the MSL writer and `DAWN_TARGET_MACOS=ON` on macOS. Null
    everywhere. Windows and macOS skip the SPIRV-Tools and Vulkan dependencies and stage only Abseil's
    license among the dependencies. Windows links `user32`, `onecore_apiset` and `dxguid`; macOS the
    frameworks of Dawn's CMake plus CoreFoundation for Abseil.
  - SDL3 links its Windows system libraries (with `dinput8`) and its macOS frameworks with Core Audio
    and Audio Toolbox left out (audio is off). miniaudio needs no system library on either: it loads
    its backends at runtime.
  - macOS: deployment target 12.0, the value .NET 10's own native code uses (`eng/native/
    configurecompiler.cmake` in dotnet/runtime, release/10.0), passed as `--target_minver=12.0` and,
    for the CMake packages, `CMAKE_OSX_DEPLOYMENT_TARGET`. No `--toolchain` for macOS: packages inherit
    an explicit `xcode`, which xmake does not count as a host toolchain, so it configured CMake as a
    cross build with `CMAKE_OSX_SYSROOT=""` and `CMAKE_FIND_USE_CMAKE_SYSTEM_PATH=0`
    (`modules/package/tools/cmake.lua`); Tint's `find_library(CoreGraphics)` failed. SDL's optional
    `find_library` calls for CoreMedia, GameController and CoreHaptics would have failed the same way,
    silently (read in SDL's CMakeLists.txt; its configure never got that far in CI).
    `-dead_strip` plays the part of `--gc-sections`. Patterns are expanded to exact names as on
    Windows, because `_ma_*` in `-exported_symbols_list` left `ma_version_string` unexported with
    Xcode 26's linker; `_jade_*` stays a pattern. The C++ runtime is the system's `libc++.1.dylib`,
    since Apple has no static libc++ to link (ADR-0003's "where the platform allows").
  - Debug symbols: release builds compile the shims with debug information, link unstripped, and
    xmake's `utils.symbols.extract` splits the symbols off (`objcopy --only-keep-debug`, strip, debug
    link on Linux; `dsymutil` then `strip -S` on macOS); the linker writes the PDB on Windows. The
    packages have no debug information, so the files hold the full symbol table plus the shims' DWARF
    or PDB records. `build-native.cs` stages them into `artifacts/native-symbols/<rid>/`, apart from
    what 106 will pack.
  - Build paths: the Dawn recipe passes `-ffile-prefix-map` for its source tree and its dependencies
    (both to `dawn`) on clang and GCC. MSVC has no documented equivalent, so Windows keeps them.
  - `versions.json` gains `toolchain`, a sorted map from role to display string (`cc`, `cxx`, `sh`,
    `cmake`, `ninja`, `xmake`, plus `libc` on Linux, `vs`, `vs_toolset`, `vs_sdkver` on Windows and
    `xcode_sdkver`, `target_minver` on macOS), written by the `jade.manifest` rule.
  - `smoke-native.cs` compares SDL's video driver list with the expected one per OS, so a backend lost
    to missing headers fails the job.
  - Docs-only changes: `native.yml` has `paths-ignore: ['design/**', '**.md']`. `main` has no branch
    protection or ruleset (checked with `gh api`), so skipped checks block nothing.
  - `fetch-native.cs` takes the latest run of `native.yml` on a branch whose every job succeeded, so a
    fetched set always comes from one commit.
- Deviations from the brief:
  - The bindings check and the native tests moved into `native.yml` itself (linux-x64, win-x64 and
    osx-arm64 jobs, right after their own build) instead of a `ci.yml` job downloading artifacts.
    Cross-workflow artifacts would need `workflow_run`, which does not report on the PR. `ci.yml` lost
    its `bindings` job and its apt list; the Dockerfile is now the only copy of the system package list.
  - `scripts/build-native/Xmake.cs` now delegates to a new `Command.cs`, shared with the docker calls.
  - The CI fixes came as separate commits on the branch, and this Outcome lands last, after the
    Windows and macOS jobs, which can only run there.
- Follow-ups:
  - Orchestrator: accept or reject ADR-0013. `design/architecture.md` does not know `native/linux/`,
    `native.yml`, `fetch-native.cs` or `artifacts/native-symbols/`.
  - 104 and 105: `Rids.cs` keeps their mappings unverified. Mobile and browser need their own Dawn
    options and dependency list (the recipe's `vulkan` switch only covers Linux), SDL frameworks for
    iOS and a static `jade.bundle` path. On Apple, `jade.bundle` now expands patterns to exact names,
    which lld on Android needs too.
  - 106: ship `artifacts/native/<rid>/lib` only; symbols are separate. Decide whether to ship the
    Windows SDK's `d3dcompiler_47.dll` next to `jade_native.dll`: Dawn falls back to the system copy,
    which its comments call buggy on older Windows. Dawn's CMake copies the x64 build of that DLL even
    on arm64 (`third_party/CopyWindowsSDKDLL.cmake`).
  - 107: the Windows build links the static MSVC runtime; check whether `THIRD-PARTY-NOTICES.md` should
    say so.
  - 204: miniaudio's `ma_atomic_global_lock` is an implementation global, not API. It leaks through
    `ma_*` on Linux and Windows, and is a common symbol, neither defined nor exported, on macOS. Do not
    bind it. The three `ma_*_WCHAR` functions exist only on Windows.
  - The Linux jobs rebuild the 3 GB image every run (about 2 to 4 minutes, most of a warm job).
    Caching it would make them close to the Windows ones.
  - Builds are not bit-reproducible: two generated Dawn files embed xmake's random `build_<hex>`
    directory (`dawn/build_8756acae/gen/...` locally, `build_ff08b329` in CI), so local and CI
    linux-x64 libraries differ in 39 bytes for the same size. Mapping the build directory would fix it.
    The Windows DLL still holds 247 strings with absolute build paths (244 from Dawn and Abseil,
    3 from SDL).
  - Any edit to `scripts/build-native/Rids.cs`, even cosmetic, invalidates every RID's package cache.
  - Rootless Docker or Podman would need the container to run as root (with `XMAKE_ROOT=y`); not
    supported yet.
