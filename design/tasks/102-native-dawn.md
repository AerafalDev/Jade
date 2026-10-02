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

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
