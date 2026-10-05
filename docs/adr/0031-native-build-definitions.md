# 0031. Native build definitions and the host build

- Status: Accepted
- Date: 2026-10-05

## Context

[0010](0010-native-builds-with-xmake.md) chose to build every native library with xmake, delegating
to CMake for Dawn and SDL3, from the versions of `build/versions.json`. Roadmap task 6 writes those
definitions and `scripts/build-native.cs`, starting with `linux-x64`. The build must give the same
libraries on every machine that has the documented prerequisites, so every input has to be pinned
or checked.

Verified on 2026-10-05, at the commits pinned in `build/versions.json`, with xmake 3.1.1 and CMake
4.4.4, on CachyOS and in fresh `ubuntu:24.04` (GCC 13.3) and `archlinux` (GCC 16.2) containers:

- Dawn `b1236a9`:
  - `DAWN_FETCH_DEPENDENCIES` runs `tools/fetch_dawn_dependencies.py`, which executes the `DEPS`
    file and clones a fixed list of 19 entries whatever the options, test and tool dependencies
    included (DirectXShaderCompiler on Linux, googletest, Google Benchmark, protobuf, GLFW, glslang,
    the Vulkan loader), and ignores git failures.
  - `generator/dawn_version_generator.py` reads Dawn's commit with `git rev-parse HEAD`; without a
    git checkout the version is empty, and `src/dawn/native/Device.cpp` puts it in the cache key of
    every device (`kDawnVersion`).
  - `DAWN_BUILD_MONOLITHIC_LIBRARY=SHARED` requires `BUILD_SHARED_LIBS=OFF` and produces
    `libwebgpu_dawn.so`, which exports the `wgpu*` functions.
  - `src/cmake/DawnCompilerChecks.cmake` enables the C++20 module interface when a module compiles
    with `-fmodules-ts`; GCC 13 passes the check, but CMake cannot scan its modules and the
    generation step fails.
  - With the options below, the Linux build needs these `DEPS` entries and no others:
    `third_party/abseil-cpp`, `jinja2`, `markupsafe`, `spirv-headers/src`, `spirv-tools/src`,
    `vulkan-headers/src`, `vulkan-utility-libraries/src`, `EGL-Registry/src`,
    `OpenGL-Registry/src`.
- SDL `release-3.4.18`: its CMake build silently drops a feature whose development files are
  missing (IBus support disappeared on a machine without the `ibus` headers), and only fails when
  neither X11 nor Wayland is found. It records what it enabled in the generated
  `SDL_build_config.h`. With `SDL_AUDIO=OFF`, `CheckPipewire` is never called, so the PipeWire
  camera driver goes too; V4L2 remains.
- miniaudio `0.11.25`: `MA_DLL` "is not officially supported"; `MA_API` is a documented build option
  (section 2.7 of `miniaudio.h`). Loading the Apple frameworks at runtime can fail notarization,
  which `MA_NO_RUNTIME_LINKING` avoids (section 2.2). On Android, AAudio is only used from API 27
  (`MA_AAUDIO_MIN_ANDROID_SDK_VERSION`), so OpenSL|ES is still needed at API 26.
- xmake 3.1.1 (`/usr/share/xmake`):
  - `add_requires` looks for a system package first: `sdl` resolved to the system's SDL 1.2
    through pkg-config.
  - An installed package is reused while its platform, architecture, configs and toolchain are
    unchanged (`_get_buildhash` in `core/package/package.lua`), even if its source directory or
    definition changed.
  - `package.tools.cmake` picks the CMake generator from the `package.cmake_generator.ninja` policy,
    whose default follows the `compatibility.version` policy, and otherwise from
    `CMAKE_GENERATOR`; `set_sourcedir`, the `package.install_locally` and `package.precompiled`
    policies, `XMAKE_GLOBALDIR` and `XMAKE_CONFIGDIR` behave as used below.
  - Shared libraries are linked with the C++ driver (`g++`), which made the C-only miniaudio library
    depend on `libstdc++.so.6`; targets get no optimization flag without a mode rule or
    `set_optimize`.
  - xmake refuses to run as root without `--root` or `XMAKE_ROOT=y`, and the release binary
    `xmake-bundle-v3.1.1.linux.x86_64` needs `libncurses.so.6`.

## Decision

### Layout

- `build/xmake.lua` is the project; `build/dawn/`, `build/sdl/` and `build/miniaudio/` hold one
  definition each. Dawn and SDL3 are xmake packages built through `package.tools.cmake`; miniaudio
  and its shim are an xmake target.
- `scripts/build-native.cs`, with its code in `scripts/build-native/` like the binding generator,
  is the entry point. It takes no arguments and builds the host's runtime identifier; only
  `linux-x64` is defined until roadmap task 10.

### Inputs

- `build-native.cs` checks that `xmake --version` and `cmake --version` report the versions of
  `build/versions.json`, and that git, Ninja and Python 3 are installed.
- It fetches each dependency with git at its pinned commit (`git fetch --depth 1`, then checks
  `HEAD`) into `artifacts/native/sources/<dependency>/<commit>/`, written aside and moved in place
  when complete. Checkouts rather than archives give Dawn its version.
- Dawn's third-party sources are the `DEPS` entries listed in `build/dawn/deps.json`, fetched the
  same way at the commits Dawn's `DEPS` pins. `DAWN_FETCH_DEPENDENCIES` stays off.
- xmake runs with its global directory and project configuration under `artifacts/native/`, with
  `--network=private`, packages built from the local definitions only (`system = false`,
  `package.precompiled` off) and installed inside the build directory. `CFLAGS`, `CXXFLAGS`,
  `CPPFLAGS`, `LDFLAGS`, `CMAKE_GENERATOR` and `CMAKE_TOOLCHAIN_FILE` are removed from its
  environment, and CMake always uses Ninja.
- Each package gets a build key, a hash of its pinned commit, of `build/xmake.lua` and of its
  definition directory, passed as a package config so that xmake rebuilds it when one changes.

### Configuration

- Dawn: the monolithic shared library `webgpu_dawn`, release build, with Dawn's default backends
  for the platform (on Linux: Vulkan, OpenGL, OpenGL ES and Null, X11 and Wayland surfaces); no
  samples, tests, benchmarks, Tint tools, protobuf, GLFW or C++20 module interface. Windows builds
  add a built DXC ([0030](0030-d3d12-shader-compilers.md)).
- SDL3: the shared library only, release build, without tests, examples or the test library, and
  with `SDL_AUDIO=OFF`, since its audio subsystem is never initialized
  ([0005](0005-sdl3-platform-layer.md)). Its optional dependencies are loaded at runtime (SDL's
  default). After configuration, the build fails unless `SDL_build_config.h` defines every feature
  of the platform's list in `build/sdl/xmake.lua`; on Linux: X11 with its extensions, Wayland with
  libdecor, KMS/DRM, EGL, GLX, Vulkan, D-Bus, IBus, Fcitx, udev, libusb, liburing, FriBidi and
  libthai, each loaded at runtime where SDL can.
- miniaudio: one shared library with the shim, built with `-O3` and `NDEBUG` like the CMake release
  builds, hidden symbols, warnings as errors, and `-Wl,--as-needed` on Linux so that it depends on
  `libc` and `libm` only.
- The `MA_*` defines of `build/miniaudio/config.h`:
  - `MA_API` exports the API: `__declspec(dllexport)` on Windows, default visibility elsewhere.
  - `MA_NO_RUNTIME_LINKING` on Apple platforms.
  - Nothing else: every backend, decoder and the engine keep miniaudio's defaults.
- The shim (`build/miniaudio/jade_miniaudio.h`) allocates and frees the structures that the managed
  side keeps opaque ([0006](0006-miniaudio-for-audio.md)): `ma_context`, `ma_device`, `ma_engine`
  and `ma_sound`, zeroed and aligned to `MA_SIMD_ALIGNMENT`. Roadmap task 8 completes the list with
  every structure the generator makes opaque.

### Outputs

- `artifacts/native/bin/<rid>/` holds exactly the shipped files, laid out as
  `runtimes/<rid>/native/` ([0011](0011-native-package-layout.md)). The libraries keep their
  upstream names, which the interop projects will import: `libwebgpu_dawn.so`, `libSDL3.so` (the
  library file itself, whose `SONAME` stays `libSDL3.so.0`) and `libminiaudio.so`.
- When the target is the host, `build-native.cs` loads each library, resolves a few of its exports
  and prints its size and SHA-256.
- The host build links against the host's glibc and C++ runtime and is meant for local work; the
  glibc 2.28 baseline of [0024](0024-minimum-os-versions.md) applies to the builds of roadmap task
  10.
- `THIRD-PARTY-NOTICES.md` is checked against every source file and header that Ninja records for
  the library targets (`ninja -t inputs` and `ninja -t deps`), for each target when its build is
  added.

## Consequences

- A machine with the prerequisites listed in `CONTRIBUTING.md` builds the same libraries, or fails
  with the missing tool, version or development package named.
- A first build fetches about 1 GB of sources (OpenGL-Registry alone takes 277 MB on disk) and
  takes a few minutes on 16 cores; later builds reuse the sources and the packages until a commit
  or a definition changes.
- `build/dawn/deps.json` and the SDL3 feature list must follow Dawn's and SDL3's updates: a missing
  `DEPS` entry fails Dawn's configuration, and a new optional SDL3 feature is not built until it is
  added to the list.
- Disabling SDL3's audio also drops its PipeWire camera driver on Linux.
- On a machine without the IBus development files, the SDL3 build fails rather than producing a
  library without IBus support.
- The `MA_NO_RUNTIME_LINKING` choice for Apple is not verified until the macOS and iOS builds exist
  (roadmap task 10).

## Alternatives considered

- **`DAWN_FETCH_DEPENDENCIES`**: Dawn's supported way, but it clones the test and tool dependencies
  too and continues after a failed clone.
- **Source archives instead of git checkouts**: no git needed, but Dawn's version, and with it the
  key of its pipeline cache, would be empty.
- **Letting xmake download the sources**: a git URL with a commit hash makes xmake clone the whole
  history (`git clone --filter=tree:0`), and archive URLs need a SHA-256 that `build/versions.json`
  does not pin.
- **Keeping whatever SDL3 detects**: the libraries would differ between machines without any
  input saying so.
- **`MA_DLL`**: documented as unsupported by miniaudio.
- **Library names with a Jade prefix**: would avoid clashes with other packages that ship SDL3 or
  miniaudio, but would differ from SDL3's module name, which the browser archives already follow
  ([0025](0025-browser-natives-with-workload-emscripten.md)); the packaging (roadmap task 11) can
  revisit it.
