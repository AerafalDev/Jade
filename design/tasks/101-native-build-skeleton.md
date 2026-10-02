# 101: Native build skeleton

- Depends on: 001
- ADRs: 0003, 0004, 0008

## Goal

`dotnet scripts/build-native.cs --rid linux-x64` builds `libjade_native.so`. SDL3 and miniaudio are
statically linked into it, from our own pinned xmake packages. It exports only `SDL_*`, `ma_*` and
`jade_*` symbols. Headers and licenses are staged for the generator and packaging.

## Context

- xmake 3.1.1 is installed. Platforms from `xmake f --help`: `windows` (x64, arm64), `linux`
  (x86_64, arm64), `macosx` (x86_64, arm64), `android` (arm64-v8a, x86_64), `iphoneos` (arm64,
  x86_64), `wasm` (wasm32). `--runtimes` selects the CRT or STL (`MT`, `MD`, `c++_static`, ...).
- xmake-repo has `libsdl3` (up to 3.4.12) and `miniaudio`. Use them as reference only. Upstream on
  2026-10-02: SDL `release-3.4.16`, miniaudio `0.11.25`. Re-check.
- Layout target (architecture.md): `native/xmake.lua`, `native/packages/<letter>/<name>/xmake.lua`
  (local repository), `native/shims/`. Outputs go to `artifacts/native/<rid>/{lib,include,metadata}`.

## Scope

- `native/xmake.lua` with target `jade_native`: shared library now; the static variant is only
  needed by 104/105, but do not make it hard to add.
- Local package repository with pinned definitions (version and SHA-256) for SDL3 and miniaudio.
  - SDL3: enable what a game host needs (video, events, keyboard/mouse/gamepad/joystick, haptic,
    sensor, clipboard, dialogs, filesystem, ...). Disable what other Jade libraries cover if SDL's
    options allow it: audio (miniaudio), SDL_GPU and the 2D renderer (Dawn). Ask the user before
    disabling anything else. Record the final option list in the Outcome.
  - miniaudio: compiled once into jade_native, with defines recorded in the package.
- `native/shims/jade_native.c` exposing at least `jade_native_abi_version()`, which the smoke test
  uses.
- An export-control mechanism, per ADR-0003, designed so later libraries plug in with one line.
  Implement and check it on Linux now. Write down how Windows and macOS will do it, and implement
  them only if it is cheap; 103 completes them.
- `scripts/build-native.cs`: `--rid` (defaults to the host RID), `--config release|debug`. It maps
  RIDs to xmake platform, arch and toolchain for every RID in ADR-0007 (an unverified mapping for
  non-host RIDs is fine, but say so), runs xmake, and stages:
  - `lib/`: the library;
  - `include/<lib>/`: public headers, exactly the ones bound;
  - `metadata/`: `versions.json` with every upstream's version, commit and hash, plus `licenses/`.
- Add SDL3 and miniaudio to `THIRD-PARTY-NOTICES.md`.
- Update **Commands** in `CLAUDE.md`.

## Out of scope

- Dawn (102), other RIDs in CI (103), bindings (201).

## Acceptance criteria

- [ ] The build command produces `artifacts/native/linux-x64/lib/libjade_native.so`.
- [ ] `nm -D --defined-only` lists only `SDL_*`, `ma_*` and `jade_*` (plus unavoidable
      linker-defined symbols, listed in the Outcome).
- [ ] `ldd` shows no SDL3 or miniaudio dependency, and only system libraries.
- [ ] A smoke check loads the library and calls `SDL_GetVersion`, `ma_version_string` and
      `jade_native_abi_version`. A tiny file-based app with raw `DllImport`s is enough.
- [ ] Running the build a second time without changes is fast; report the time.
- [ ] Upstream sources are downloaded from pinned URLs and verified by SHA-256. No system SDL3 is
      used, even if installed.

## Verification

Run the build command, `nm`, `ldd` and the smoke check, and paste the relevant lines. Report the
library size, stripped and unstripped.

## Pitfalls

- Symbols from static archives are not exported by default. Whole-archive linking and the export
  macros (`SDL_DECLSPEC`, `MA_API`) or an explicit export list are needed, and hidden visibility for
  the rest.
- SDL3 on Linux needs X11/Wayland development headers at build time, though it loads them
  dynamically at runtime. If headers are missing, give the user the `paru` command; do not install
  anything.
- Do not set the glibc baseline here (103). Avoid anything that would make an old-glibc container
  build impossible.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
