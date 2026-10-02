# 103: Desktop RID matrix in CI

- Depends on: 102, 002
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

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
