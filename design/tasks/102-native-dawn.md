# 102: Dawn from source, linked into jade_native

- Depends on: 101
- ADRs: 0001, 0003, 0004

## Goal

`libjade_native.so` for linux-x64 also contains Dawn, built from source at a pinned release tag
through an xmake package that drives Dawn's CMake. It exports the `wgpu*` C API. The matching
`webgpu.h` and `dawn.json` are staged for the generator.

## Context

- Dawn tags daily (`v20260930.214659` on 2026-10-02); pick the latest tag at task time.
  `src/dawn/dawn.json` in that revision is the generator input (ADR-0005).
- Official Linux prebuilts contain only static libraries (`libwebgpu_dawn.a` about 45 MB plus Tint
  archives) built on `ubuntu-latest`. They are not usable for our glibc baseline or our RID list,
  hence the source build.
- Linux backend: Vulkan. Later RIDs: D3D12 (and possibly D3D11) on Windows, Metal on Apple, Vulkan
  on Android.
- From 101: bundling goes through the package's `on_install` calling `native/modules/stage.lua`
  (exports such as `wgpu*`, headers, licenses), plus one line in the `bundled` list of
  `native/xmake.lua`. `scripts/build-native/Stage.cs` does not merge a `jade/metadata/` folder yet;
  add that so `metadata/dawn.json` reaches `artifacts/native/<rid>/metadata/`. Static libstdc++
  needs its own link flags: the current `--as-needed` only drops unused runtimes.
- From 101: SDL's exports come from upstream's exact list (`SDL_dynapi.sym`) because an `SDL_*`
  pattern leaked internals. Check whether Dawn has an equivalent list before relying on `wgpu*`.

## Scope

- `native/packages/d/dawn/xmake.lua`: pinned tag, commit and hash, driving Dawn's CMake. Pick the
  minimal option set from Dawn's `CMakeLists.txt` and its options files, with no option name taken
  from memory: monolithic `webgpu_dawn`, no samples, tests, GLFW, Node or emscripten bits, only the
  Tint parts the native backends need. Record the option list.
- Dependency fetching that is reproducible and pinned. Use Dawn's own mechanism, checked in its
  sources, and keep downloads cached across builds.
- C++ runtime: static libstdc++ on Linux (ADR-0003), with the outcome confirmed by `ldd`.
- Export `wgpu*` (and Dawn's C-API extension symbols, if any) through the 101 mechanism.
- Stage `include/webgpu/webgpu.h` (and Dawn headers if bound) and `metadata/dawn.json` from the
  same revision.
- Add Dawn, and the licenses of what it statically pulls in (Abseil, SPIRV-Tools, ...), to
  `THIRD-PARTY-NOTICES.md`.

## Out of scope

- Other RIDs (103+), bindings (202), surface creation from SDL windows (done in C# later).

## Acceptance criteria

- [ ] The build command produces `libjade_native.so` with `wgpu*`, `SDL_*`, `ma_*` and `jade_*`
      exports and nothing else (list exceptions).
- [ ] `ldd` shows no libstdc++/libc++ dependency. Vulkan is loaded at runtime, not linked, unless
      Dawn requires otherwise; document which.
- [ ] Smoke check: `wgpuCreateInstance` returns non-null and an adapter request completes on this
      machine. The adapter part needs a GPU and cannot be verified on headless CI; say so.
- [ ] Clean build time and disk usage (sources + build tree) are reported. Both matter for the
      14 GB arm64 runners in 103.
- [ ] A rebuild with no changes does not rebuild Dawn.

## Verification

Build, `nm -D --defined-only | grep -c '^.* T wgpu'`, `ldd`, smoke check, `du -sh` of the build and
cache directories, and stripped and unstripped size of the library.

## Pitfalls

- Dawn's dependency fetch may need Python. Running the system `python3` on a script is fine.
  `pip install` is not; use `uv run --with ...` if packages are needed.
- Dawn exports with `WGPU_SHARED_LIBRARY` / `WGPU_IMPLEMENTATION`. Check how its CMake sets them
  for a static monolithic build linked into our shared library.
- Whole-archive linking of Dawn can drag in unused code. Measure the size difference between
  whole-archive and an explicit export list before choosing.

## Outcome

- Summary: `native/packages/d/dawn/xmake.lua` builds Dawn `v20260930.214659` from source through
  Dawn's CMake (static monolithic `webgpu_dawn`, Vulkan and Null backends) and stages it through
  `modules/stage.lua`; `dawn` is the third entry of `bundled`. `libjade_native.so` for linux-x64
  now also exports the 276 `wgpu*` functions, contains libstdc++ and libgcc, and is linked with
  `--gc-sections`. `include/webgpu/webgpu.h`, `include/dawn/webgpu.h` and `metadata/dawn.json` are
  staged: a stage fragment can now carry `jade/metadata/` (`metadata` option of `stage.lua`, merged
  by `Stage.cs`). The smoke check creates a WebGPU instance and requests a Null and a default
  adapter. `THIRD-PARTY-NOTICES.md` covers Dawn, the code it compiles in and the GCC runtime.
- Verification (commands and results), on linux-x64 (CachyOS, 16 cores, xmake 3.1.1, clang 23.1.1,
  CMake 4.4.3, Python 3.14.7, git 2.56.0):
  - Cold build: with `XMAKE_PKG_INSTALLDIR` and `XMAKE_PKG_CACHEDIR` pointed at empty directories
    (so `~/.xmake` was left alone) and `native/build`, `native/.xmake` and `artifacts/native`
    removed, `dotnet scripts/build-native.cs --rid linux-x64` downloaded and built SDL3, miniaudio
    and Dawn, and staged in 100.3 s (the script's own stopwatch). Dawn's dependency fetch alone took
    6.6 s when run by hand.
  - Disk, same run, sampled every 5 s: peak 746 MB for the package cache and installs together.
    The parts: Dawn's archive 27 MB, its extracted source tree 491 MB, the cached dependencies
    123 MB (git checkouts), and the build tree (91 MB for the `webgpu_dawn` target in an equivalent
    manual CMake build). xmake deletes the source and build trees after installing. After the
    build: package cache 169 MB (Dawn dependencies 123 MB, the three archives 46 MB), installed
    packages 46 MB (Dawn 26 MB), `native/build` 12 MB.
  - `--config debug`: 92 s, peak 3.4 GB, `libwebgpu_dawn.a` 728 MB, `libjade_native.so`
    247,247,096 bytes; the smoke check passes. It reused the cached dependencies (the clones kept
    their timestamps).
  - Unchanged rebuild: three runs at 534, 529 and 499 ms wall, xmake `build ok, spent 0,028s`, no
    download or install line, so Dawn is not rebuilt.
  - `nm -D --defined-only artifacts/native/linux-x64/lib/libjade_native.so`: 2726 symbols, 1270
    `SDL_*`, 1179 `ma_*`, 276 `wgpu*`, 1 `jade_*`, nothing else (no exceptions to list);
    `grep -c '^.* T wgpu'` gives 276. The `wgpu*` exports are exactly the functions declared in
    the staged `dawn/webgpu.h` (`comm -3` prints nothing). SDL and miniaudio counts are unchanged by
    `--gc-sections`.
  - `ldd`: `linux-vdso.so.1`, `libm.so.6`, `libc.so.6`, `ld-linux-x86-64.so.2`. `readelf -d`:
    NEEDED `libm.so.6`, `libc.so.6` and now `ld-linux-x86-64.so.2`, for `_dl_find_object@GLIBC_2.35`
    (the static libgcc unwinder) and `__tls_get_addr@GLIBC_2.3`. The 363 undefined symbols are all
    glibc's: none from libstdc++, Vulkan, X11 or Wayland. Vulkan is loaded at runtime: Dawn opens
    `libvulkan.so.1` (`src/dawn/native/vulkan/BackendVk.cpp`, the string is in the library) and
    X11 through `libX11.so.6` and `libX11-xcb.so.1`.
  - Smoke check, `dotnet scripts/smoke-native.cs`, exit code 0:

    ```text
    Loaded artifacts/native/linux-x64/lib/libjade_native.so
    SDL_GetVersion: 3.4.16 (SDL-release-3.4.16-0-gfa2c02bb6) ok
    ma_version_string: 0.11.25 ok
    jade_native_abi_version: 1 ok
    SDL video drivers: wayland, x11, kmsdrm, offscreen, dummy, evdev
    wgpuCreateInstance: non-null ok
    wgpuInstanceRequestAdapter (Null backend): Success (Null backend (Null, Unknown)) ok
    wgpuInstanceRequestAdapter (default backend): completed (Success: AMD Radeon RX 7900 XTX (RADV NAVI31) (Vulkan, DiscreteGPU)) ok
    ```

    The Null request needs no GPU. The default request needs a GPU and a Vulkan driver, so on
    headless CI it can only be expected to complete (with `Unavailable`); that part is verified on
    this machine only.
  - Sizes: staged (stripped) release library 12,162,112 bytes with the package cache under the
    scratch directory, 12,145,728 bytes with the default `~/.xmake` (the difference is the length
    of the build paths embedded in it, see Follow-ups), against 3,466,784 before Dawn. With
    `--policies=build.release.strip:n` (xmake run by hand for this measurement only, scratch cache):
    14,980,824 bytes, no DWARF; `strip` brings it back to 12,162,112.
  - Canonical run, `dotnet scripts/build-native.cs` with the default `~/.xmake` (after the runs
    above, with `native/.xmake` deleted, see Follow-ups): installs SDL3, miniaudio and Dawn and
    stages in 101.1 s; the smoke check, `nm` and `readelf` give the results above.
  - Link mode, measured on Dawn's archive alone, linked into a test library with the same version
    script and runtime flags, then stripped: whole archive 10,005,152 bytes; without whole-archive
    but `--undefined` for each export 9,419,096; either one with `--gc-sections` 8,735,096.
  - Highest symbol version: `GLIBC_2.44` (`cosh` and `sinh` from this host's libm), up from
    `GLIBC_2.43`.
  - Fetch failure: with `markupsafe` deleted from the cache and `GIT_ALLOW_PROTOCOL=file` (git
    refuses https), the build exited 1 with `dawn: fetching third_party/markupsafe@4256084a… from
    https://chromium.googlesource.com/chromium/src/third_party/markupsafe failed (HEAD is 'HEAD')`
    and left no checkout behind. The next normal build cloned `markupsafe` only (the other clones
    kept their timestamps), rebuilt Dawn in 88.2 s and passed the smoke check.
  - Scripts: `dotnet build scripts/build-native.cs` and `dotnet build scripts/smoke-native.cs`:
    0 warnings, 0 errors. `dotnet build -c Release`: 0 warnings, 0 errors. `dotnet test -c Release`:
    total 1, failed 0.
- Decisions taken (and ADRs added):
  - Version: `v20260930.214659`, still the latest release on 2026-10-02 (`gh release list -R
    google/dawn`). The tag is commit `6fa6adb71bdcbf7bb17fe21fb1702bf43e89078f`; the release notes
    cite its parent `9af2744f`, and the tagged commit only changes the V8 pin in `DEPS`. Source:
    GitHub's archive of the tag (SHA-256 `4a561928…cef03`). dawn.googlesource.com, the official
    source, has no release archives, and the release assets are binaries and headers only.
  - Dependencies, pinned by `DEPS` at that tag, fetched by Dawn's own
    `tools/fetch_dawn_dependencies.py`: `abseil-cpp` (`af5aede1`), `jinja2` (`c3027d88`),
    `markupsafe` (`4256084a`), `spirv-headers` (`cb42dec3`), `spirv-tools` (`19e35eae`),
    `vulkan-headers` (`3c65a017`), `vulkan-utility-libraries` (`666fdaa0`). The recipe imports the
    script and calls its `process_dir` with this list instead of setting `DAWN_FETCH_DEPENDENCIES`,
    which clones a hard-coded list of 19 entries (DXC, glslang, googletest, protobuf, GLFW, ...)
    into the source tree that xmake deletes after each install. The script also ignores git errors
    and assumes each target directory exists (in a temporary root it cloned nothing in 0.06 s and
    reported no error): the recipe creates the directories, compares each checkout's HEAD with
    `DEPS` and fails, removing the entry, on a mismatch. Shallow clones at a pinned commit are
    verified by git's SHA-1 object IDs, not by a SHA-256.
  - Dependency cache: `<XMAKE_PKG_CACHEDIR or ~/.xmake/cache/packages>/dawn-deps/<Dawn commit>/`.
    A reinstall (recipe edit, other configuration) clones nothing; a Dawn bump starts a new
    directory.
  - CMake options, all spelled out, defaults included: `DAWN_BUILD_MONOLITHIC_LIBRARY=STATIC`,
    `DAWN_FETCH_DEPENDENCIES=OFF`, `DAWN_ENABLE_INSTALL=OFF`, `DAWN_BUILD_SAMPLES=OFF`,
    `DAWN_BUILD_TESTS=OFF`, `DAWN_BUILD_BENCHMARKS=OFF`, `DAWN_BUILD_FUZZERS=OFF`,
    `DAWN_BUILD_NODE_BINDINGS=OFF`, `DAWN_BUILD_PROTOBUF=OFF`, `DAWN_USE_GLFW=OFF`,
    `DAWN_ENABLE_SWIFTSHADER=OFF`, `DAWN_ENABLE_VULKAN=ON`, `DAWN_ENABLE_NULL=ON`,
    `DAWN_ENABLE_DESKTOP_GL=OFF`, `DAWN_ENABLE_OPENGLES=OFF`, `DAWN_USE_X11=ON`,
    `DAWN_USE_WAYLAND=ON`, `DAWN_ENABLE_SPIRV_VALIDATION=OFF`, `TINT_BUILD_SPV_READER=OFF`,
    `TINT_BUILD_WGSL_READER=ON`, `TINT_BUILD_SPV_WRITER=ON`, `TINT_BUILD_NULL_WRITER=ON`,
    `TINT_BUILD_WGSL_WRITER=OFF`, `TINT_BUILD_GLSL_WRITER=OFF`, `TINT_BUILD_GLSL_VALIDATOR=OFF`,
    `TINT_BUILD_HLSL_WRITER=OFF`, `TINT_BUILD_MSL_WRITER=OFF`, `TINT_BUILD_IR_BINARY=OFF`,
    `TINT_BUILD_CMD_TOOLS=OFF`, `TINT_BUILD_TESTS=OFF`, `TINT_BUILD_BENCHMARKS=OFF`,
    `TINT_BUILD_FUZZERS=OFF`, plus `CMAKE_BUILD_TYPE=Release|Debug` and one `DAWN_*_DIR` per
    dependency. xmake adds `BUILD_SHARED_LIBS=OFF` and `CMAKE_POSITION_INDEPENDENT_CODE=ON`.
  - Backends: Vulkan, with surfaces from X11 and Wayland windows (what SDL provides), plus Null,
    Dawn's default, kept because it answers adapter requests without a GPU (above), which makes
    headless tests possible. OpenGL and OpenGL ES are off.
  - Shaders: WGSL only. Without SPIR-V shader modules there is no SPIR-V reader and no validation
    of SPIR-V input, the only users of SPIRV-Tools, which is therefore configured (its sources must
    exist) but never built. The Vulkan backend needs only Tint's WGSL reader and SPIR-V writer,
    which uses SPIRV-Headers alone.
  - Only the `webgpu_dawn` target is built, then the archive and the two C headers are copied.
    Dawn's install step needs the default target, which adds 203 build steps (Abseil flags, log
    and random libraries, `dawn_wire`, test utilities): 19 s and 30 MB more in the manual build.
  - Exports: Dawn has no export list. Its shared build exports what `dawn/webgpu.h` declares with
    `WGPU_EXPORT`, so the recipe parses those declarations: 276 names.
    `emscripten_webgpu_get_device` is declared there too, unconditionally, but defined only in
    Emscripten builds, so the list keeps names starting with `wgpu`. A `wgpu*` pattern would give
    the same set today (the archive defines exactly these 276 `wgpu*` globals); the exact list
    guards against future internal `wgpu*` symbols and suits lld (104). Dawn has no other C API:
    its other global non-C++ symbols are Abseil internals (`AbslInternal*`) and Tint markers
    (`tint_*_symbol`).
  - Export macros: in a static monolithic build `WGPU_SHARED_LIBRARY` is not defined, so
    `WGPU_EXPORT` is empty. Dawn's CMake does not use `-fvisibility=hidden`, so every symbol in the
    archive keeps default visibility and the version script alone decides what is exported.
  - Link mode: whole-archive stays, and `jade.bundle` adds `-Wl,--gc-sections` on Linux and
    Android (Android not verified; measurements above: it removes more than an explicit export
    list does, and patterns such as `ma_*` keep working).
  - C++ runtime (ADR-0003): `jade.bundle` adds `-static-libstdc++ -static-libgcc` on Linux.
    Without `-static-libgcc`, `libgcc_s.so.1` became NEEDED (checked on the test link). xmake's
    `stdc++_static` runtime does not fit: it adds `-static-libstdc++` only to targets with C++
    sources (`modules/core/tools/clang.lua`), and jade_native has none, and never `-static-libgcc`.
  - Licenses staged in `metadata/licenses/dawn/`: Dawn's `LICENSE`, Chromium's license (from
    `tools/nocompile/LICENSE`, for `src/dawn/common/LinkedList.h`), RenderDoc's, and the license
    files of Abseil, SPIRV-Headers, Vulkan-Headers and Vulkan-Utility-Libraries. That set comes from
    Ninja's header dependencies of an equivalent manual build, plus the copyright lines of the Dawn
    files it lists: no SPIRV-Tools, jinja2 or markupsafe code, and none of Dawn's other in-tree
    third-party code is used.
  - CMake generator: xmake's default, which on Linux without `CMAKE_GENERATOR` is Unix Makefiles
    run with `-j` (`modules/package/tools/cmake.lua`). Ninja is not a prerequisite.
  - `JADE_NATIVE_ABI_VERSION` stays 1: adding exports breaks nothing generated against it.
  - No ADR added.
- Deviations from the brief:
  - Dependencies come from Dawn's script through its `process_dir` function with a reduced list,
    not from `DAWN_FETCH_DEPENDENCIES` (see Decisions).
  - `--gc-sections` was not in the brief; the brief asked to compare whole-archive with an export
    list, and `--gc-sections` beats both.
  - `THIRD-PARTY-NOTICES.md` also lists the statically linked GCC runtime, and the closing fence of
    the miniaudio block, which sat on the last text line, is now on its own line.
  - `Stage.cs` now refuses a fragment that would stage `metadata/versions.json`.
- Follow-ups:
  - 103: CI needs Python 3 and git for Dawn (its code generator and the dependency fetch), and the
    X11 headers. The package cache (`XMAKE_PKG_CACHEDIR`) holds `dawn-deps/` (123 MB); old Dawn
    commits accumulate there, as old installs do. The peak (746 MB release, 3.4 GB debug) is mostly
    Dawn's 491 MB source tree; xmake's `download.lua` passes `url_excludes` to the extractor, not
    investigated as a way to skip Dawn's tests and CTS. `GLIBC_2.44` here comes from this host's
    libm, and `_dl_find_object@GLIBC_2.35` from this host's static libgcc; a baseline container
    links its own, older ones. macOS has no equivalent of `--gc-sections` yet (`-dead_strip`, not
    verified). Windows needs its own Dawn options (D3D12, maybe D3D11, DXC) and dependency list.
  - 103: the library embeds 250 absolute build paths (`__FILE__` in Dawn and Abseil, under the
    package cache, so the builder's home directory), which also makes its size depend on where the
    cache is. `-fmacro-prefix-map` (or `-ffile-prefix-map`) for the Dawn package would fix it; not
    done here.
  - 103: switching `XMAKE_PKG_INSTALLDIR` between two runs breaks `xmake f --require=y` until
    `native/.xmake` is deleted: its package cache keeps the old paths, packages are not
    reinstalled, and `jade.bundle` fails on a missing `jade/exports.txt`. CI sets the variable for
    every run, so only local switching is affected.
  - 104: the C++ runtime flags are added on Linux only; Android's static libc++ is left to the NDK
    toolchain. Android needs Vulkan (and maybe OpenGL ES) options and its own dependency list.
  - 202: `include/webgpu/webgpu.h` only includes `dawn/webgpu.h`; `metadata/dawn.json` is the file
    of the tag (`cmp` with `src/dawn/dawn.json`: identical). Skip `emscripten_webgpu_get_device`,
    declared but not exported. `WGPUShaderSourceSPIRV` exists in the header, and
    `wgpuHasInstanceFeature(ShaderSourceSPIRV)` reports it (a static list in `Instance.cpp`), but
    this build rejects such a module with the validation error `SPIR-V is disallowed.`
    (`Device.cpp`, `!TINT_BUILD_SPV_READER`). Read in the source, not run.
  - 205: the Null backend answers adapter requests on headless CI (its adapter reports type
    `Unknown`); device creation on it was not tried.
  - Before the first release (107): the notices file says every license is reproduced in full,
    which the GCC runtime entry (GPLv3 with the GCC Runtime Library Exception 3.1) does not do;
    check that wording.
