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
- From 001: scripts run with the repo analyzers plus AOT and trim analyzers. Writing
  `versions.json` with reflection-based `System.Text.Json` therefore fails: use a source-generated
  `JsonSerializerContext`, or `#:property PublishAot=false` if justified. If the script needs a NuGet
  package, `#:package` together with Central Package Management is still unsettled (ADR-0008):
  settle it and record how in the Outcome.
- From 001: `.gitignore` already covers `.xmake/`, `native/build/`, the xmake project generators'
  outputs and `compile_commands.json`.

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

- [x] The build command produces `artifacts/native/linux-x64/lib/libjade_native.so`.
- [x] `nm -D --defined-only` lists only `SDL_*`, `ma_*` and `jade_*` (plus unavoidable
      linker-defined symbols, listed in the Outcome).
- [x] `ldd` shows no SDL3 or miniaudio dependency, and only system libraries.
- [x] A smoke check loads the library and calls `SDL_GetVersion`, `ma_version_string` and
      `jade_native_abi_version`. A tiny file-based app with raw `DllImport`s is enough.
- [x] Running the build a second time without changes is fast; report the time.
- [x] Upstream sources are downloaded from pinned URLs and verified by SHA-256. No system SDL3 is
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

- Summary: `dotnet scripts/build-native.cs` builds `libjade_native.so` for linux-x64 from the xmake
  project in `native/`: local package repository (`packages/s/sdl3`, `packages/m/miniaudio`), shim
  `shims/jade_native.c`, rules `rules/bundle.lua` (`jade.bundle`: whole-archive plus export list;
  `jade.manifest`: build manifest) and module `modules/stage.lua` (each package's stage fragment).
  The script stages `artifacts/native/<rid>/{lib,include,metadata}`. `dotnet scripts/smoke-native.cs`
  loads the staged library and checks it. SDL3 and miniaudio are in `THIRD-PARTY-NOTICES.md`;
  CLAUDE.md has the commands.
- Verification (commands and results), on linux-x64 (CachyOS, xmake 3.1.1, clang 23.1.1, GNU ld
  2.47):
  - Cold build, after `rm -rf ~/.xmake native/build native/.xmake artifacts/native`:
    `dotnet scripts/build-native.cs --rid linux-x64` downloads both archives, installs both
    packages and stages in 16.0 s. Disk afterwards: `~/.xmake/packages` 20M, `~/.xmake/cache` 20M,
    `native/build` 3.4M.
  - Second run without changes: 0.50 s, 0.49 s and 0.48 s wall for three runs (`time`), of which
    xmake reports `build ok, spent 0,025s`.
  - `nm -D --defined-only artifacts/native/linux-x64/lib/libjade_native.so`: 2450 symbols: 1270
    `SDL_*`, 1179 `ma_*`, 1 `jade_*` (`jade_native_abi_version`), 0 others. The `SDL_*` set is
    exactly the global names of SDL's own `src/dynapi/SDL_dynapi.sym` (`comm -3` prints nothing).
    No linker-defined symbol is exported: `local: *` in the version script also hides `_init`,
    `_fini`, `_edata`, `_end` and `__bss_start`.
  - `ldd`: `linux-vdso.so.1`, `libm.so.6`, `libc.so.6`, `/usr/lib64/ld-linux-x86-64.so.2`.
    `readelf -d`: NEEDED `libm.so.6` and `libc.so.6` only. None of the undefined symbols names
    SDL, X11, Wayland, ALSA or PulseAudio (`nm -D --undefined-only | grep -ciE ...` gives 0).
  - No system SDL3: a system SDL3 3.4.16 is installed here (`/usr/lib/libSDL3.so.0.4.16`,
    `pkg-config --modversion sdl3`). It is not used: packages are required with
    `{system = false}`, the shim compiles with `-isystem ~/.xmake/packages/s/sdl3/3.4.16/<hash>/include`
    (verbose build), and the library has no SDL dependency (above).
  - SHA-256: with the last digit of the SDL3 hash changed, `xmake f` failed with
    `error: unmatched checksum, current hash(7322236c) != original hash(7322236c)` and the script
    exited 1. The recipe was restored.
  - Smoke check, `dotnet scripts/smoke-native.cs`, exit code 0:

    ```text
    Loaded artifacts/native/linux-x64/lib/libjade_native.so
    SDL_GetVersion: 3.4.16 (SDL-release-3.4.16-0-gfa2c02bb6) ok
    ma_version_string: 0.11.25 ok
    jade_native_abi_version: 1 ok
    SDL video drivers: wayland, x11, kmsdrm, offscreen, dummy, evdev
    ```

    It compares the versions with `metadata/versions.json` and the ABI with
    `include/jade/jade_native.h`. Exit code 2 for a non-host RID (checked with `--rid win-x64`).
  - Sizes: the staged release library is stripped (xmake's `build.release.strip` default): 3,466,784
    bytes. The same build with `--policies=build.release.strip:n` (symbol table, no `-g`): 4,018,960
    bytes; `strip` brings it to 3,466,784. `--config debug`: 12,035,704 bytes with DWARF sections,
    smoke check passes.
  - Highest symbol version: `GLIBC_2.43` (`objdump -T`), from this host's glibc.
  - Scripts: `dotnet build scripts/build-native.cs` and `dotnet build scripts/smoke-native.cs`:
    `0 Avertissement(s)` with the repository analyzers, AOT and trim analyzers included.
  - `dotnet build -c Release`: 0 warnings, 0 errors. `dotnet test -c Release`: total 1, failed 0.
- Decisions taken (and ADRs added):
  - Versions re-checked with `gh` on 2026-10-02, still the latest: SDL `release-3.4.16`
    (`fa2c02bb6e21974a89ea9824bc53c9932abe5f9c`), miniaudio `0.11.25`
    (`9634bedb5b5a2ca38c1ee7108a9358a4e233f14d`).
  - Sources: SDL's release asset `SDL3-3.4.16.tar.gz` (SHA-256 `7322236c…77c68`), not GitHub's
    generated tarball. Its `.sig` was not verified. miniaudio has no release assets, so its tag
    archive (`b900edcf…0274`, the same hash xmake-repo pins).
  - SDL3 CMake options: `CMAKE_BUILD_TYPE=Release|Debug`, `SDL_SHARED=OFF`, `SDL_STATIC=ON`,
    `SDL_TEST_LIBRARY=OFF`, `SDL_TESTS=OFF`, `SDL_EXAMPLES=OFF`, `SDL_AUDIO=OFF`, `SDL_GPU=OFF`,
    `SDL_RENDER=OFF`, `SDL_DEPS_SHARED=ON` (the default, stated because the `ldd` result depends on
    it). Everything else keeps SDL's default, so subsystems Video, Camera, Joystick, Haptic, HIDAPI,
    Power, Sensor, Dialog and Tray are on; nothing beyond the three allowed was disabled, so no
    question to the user was needed. xmake adds `CMAKE_POSITION_INDEPENDENT_CODE=ON` (package
    default `pic = true`).
  - Disabled subsystems still export their functions, because SDL keeps every dynamic API entry:
    `SDL_InitSubSystem(SDL_INIT_AUDIO)` returns false with `SDL not built with audio support`, and
    `SDL_GetNumRenderDrivers()` returns 0 (checked with a throwaway app).
  - miniaudio: compiled once, as a static library from `miniaudio.c`, with no define. That keeps
    every backend, decoder and the engine, and backends are loaded at runtime. The list lives in
    the package (`defines`), goes to consumers as package defines, and is staged in `versions.json`.
    Its inner build skips xmake's `mode.release` rule, which compiles static targets with
    `-fvisibility=hidden` and so hid every `MA_API` function (0 `ma_*` exports on the first build).
  - Export control: each package lists its public API in its stage fragment (`jade/exports.txt`,
    exact names or `*` patterns). SDL's list is parsed from `SDL_dynapi.sym`. A `SDL_*` pattern
    exported 3341 symbols, because SDL's static build leaves internal `SDL_*` functions and the
    `*_REAL` dynamic API targets visible. `JNI_OnLoad` is kept on Android only. miniaudio uses
    `ma_*`: all its global symbols are public, since internals are `static` (1178 functions + 1
    variable). Bundling a library is one line in `bundled` (`native/xmake.lua`).
  - Linux link flags from `jade.bundle`: `-Wl,--whole-archive,<lib.a>,--no-whole-archive` per
    archive, `-Wl,--version-script=<generated>` (content hash in the file name so a list change
    relinks), `-Wl,--no-undefined`, and `-Wl,--as-needed`. Without `--as-needed`, the library
    depended on `libstdc++.so.6` and `libgcc_s.so.1`: xmake links shared libraries with `clang++`,
    even for C-only targets.
  - macOS (`-force_load` + `-exported_symbols_list`, with the Mach-O underscore) is written but
    not verified. Windows raises an error; the plan, in `rules/bundle.lua`: `/WHOLEARCHIVE` plus a
    `.def` generated before linking from the archives' symbols matched against the lists, since
    `.def` files have no wildcards. A static `jade_native` raises an error too (104, 105).
  - The build hash of an xmake package ignores its scripts. Each recipe therefore has a read-only
    config `recipe` set to the SHA-256 of the recipe and of `modules/stage.lua`. xmake re-resolves
    packages only when project files change, so the script runs `xmake f --require=y`. Checked:
    editing `stage.lua` reinstalled both packages, and the bad-hash test failed as expected.
  - The script runs xmake with `native/` as working directory, not `-P native`. With `-P` from the
    repository root, xmake kept its config in `./.xmake` and linked into `./build/` while the
    manifest was read from `native/build/`. The script also deletes the manifest before building,
    so a stale one is never staged (`after_build` rewrites it on up-to-date builds too).
  - Official xmake-repo: never cloned (`set_policy("network.mode", "private")` only stops
    repository pulls, not source downloads).
  - Package cache: xmake's default `~/.xmake` (packages keyed by build hash, shared between
    worktrees). Build tree in `native/build/<rid>`, xmake config in `native/.xmake` (both ignored).
  - RID mapping (`scripts/build-native/Rids.cs`): win-x64/win-arm64 `windows` x64/arm64 `msvc`;
    linux-x64/linux-arm64 `linux` x86_64/arm64 `clang`; osx-x64/osx-arm64 `macosx` x86_64/arm64
    `xcode`; android-arm64/android-x64 `android` arm64-v8a/x86_64 `ndk`; ios-arm64 `iphoneos`
    arm64 `xcode`; iossimulator-arm64/x64 the same plus `--appledev=simulator` (seen in xmake's
    iphoneos platform source); browser-wasm `wasm` wasm32 `emcc`. Only linux-x64 is verified; the
    script warns for the others. Host RID: OS plus `RuntimeInformation.OSArchitecture`.
  - Staged layout: `lib/libjade_native.so`; `include/SDL3/` (66 headers: every installed header but
    `SDL_test*.h`, `SDL_opengl*.h` and `SDL_egl.h`), `include/miniaudio/miniaudio.h`,
    `include/jade/jade_native.h`; `metadata/versions.json` (`rid`, `config`, `upstreams[]` with
    `name`, `version`, `commit`, `url`, `sha256`, `license`, `defines`) and
    `metadata/licenses/<package>/`.
  - `jade_native_abi_version()` returns `JADE_NATIVE_ABI_VERSION` (1), to be bumped by any change
    that breaks bindings generated against the previous value.
  - JSON in scripts: source-generated `JsonContext` (manifest, `upstream.json`, `versions.json`)
    and `JsonDocument` in the smoke check. No NuGet package was needed, so `#:package` with CPM
    (ADR-0008) is still open for 201.
  - ADR-0010 (Proposed), at the user's request: a script's entry point is `scripts/<name>.cs`, its
    helpers sit in `scripts/<name>/`, one type per file, and every internal or public member has
    `///` docs. CLAUDE.md's Scripts section follows it.
- Deviations from the brief:
  - SDL3 exports come from upstream's exact list rather than a `SDL_*` pattern, which leaked
    internals (see Decisions). The result still lists only `SDL_*`.
  - Script layout: `scripts/build-native.cs` plus helpers in `scripts/build-native/`, and a second
    script, `scripts/smoke-native.cs` (with `scripts/smoke-native/`), for the smoke check (ADR-0010).
  - The staged library is stripped (xmake's release default), so 103 has no debug symbols to keep
    yet.
  - `THIRD-PARTY-NOTICES.md` also covers code compiled into SDL3 whose license wants its notice in
    binary distributions: HIDAPI (BSD option), yuv2rgb (BSD-3), fdlibm (Sun), and three
    X11-derived files (`edid-parse.c`, `imKStoUCS.c`, `xsettings-client.c`). The Wayland protocol
    glue is not covered (follow-up).
- Follow-ups:
  - 102: plug Dawn in through `modules/stage.lua` (`exports` with `wgpu*`) and one line in
    `bundled`. `Stage.cs` does not merge a `jade/metadata/` folder yet; add it for
    `metadata/dawn.json`. Static libstdc++ (ADR-0003) needs its own flags; `--as-needed` only drops
    unused runtimes.
  - 103: Windows export control (rule raises) and checks of the macOS path; the packages'
    `on_install` accepts `linux` only, so other platforms need their system libraries and
    frameworks. The cache key must hash `scripts/build-native.cs` and `scripts/build-native/**`.
    xmake caches under `~/.xmake` (or `XMAKE_PKG_INSTALLDIR`/`XMAKE_PKG_CACHEDIR`); old installs
    accumulate there, one per recipe hash. Debug symbols need a non-stripping release
    (`build.release.strip`). `--runtimes` is not passed yet (`/MT` per ADR-0003). This build needs
    `GLIBC_2.43` and pulls `__isoc23_*` symbols from the host headers.
  - 103: SDL silently drops a video backend whose development headers are missing. The container
    needs X11 (with Xext, Xcursor, Xi, Xfixes, Xrandr, Xss), Wayland, xkbcommon, libdecor, EGL,
    D-Bus and udev headers. ibus headers are missing here, so ibus IME support is not compiled in.
    The smoke check's video driver line can serve as the assertion.
  - 104: the NDK links with lld, which rejects a version-script name that is not defined (checked
    here with LLD 23.1.1: `version script assignment of 'global' to symbol 'missing_symbol' failed:
    symbol not defined`; GNU ld 2.47 accepts it). Every exported name must exist on that platform.
    `JNI_OnLoad` must be exported for `SDLActivity`.
  - 104, 105: a static `jade_native` needs the upstream archives merged into it; not investigated.
  - 201: inputs are staged as listed above; miniaudio's defines are in `versions.json`. Under
    ADR-0010, the generator's entry point becomes `scripts/generate-bindings.cs` (CLAUDE.md updated;
    the 201 brief still says `scripts/generate-bindings/generate-bindings.cs`).
  - 203: disabled subsystems (audio, GPU, render) export functions that fail at runtime; exclude
    them from bindings instead of expecting missing exports.
  - Orchestrator: accept or reject ADR-0010 (then ADR-0008's status), update `architecture.md`
    (`native/modules/`, `native/rules/`, `scripts/smoke-native.cs`) and the 103 and 201 briefs.
  - Before the first release (107): `THIRD-PARTY-NOTICES.md` does not reproduce the notices of the
    24 Wayland protocol glue files SDL generates from `wayland-protocols/*.xml` (MIT-style).
    `metadata/licenses/` only holds the upstream license files (SDL's `LICENSE.txt`, HIDAPI BSD,
    yuv2rgb, miniaudio's `LICENSE`).
