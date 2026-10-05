# 0030. D3D12 shader compilers: a built DXC is shipped, FXC comes from the system

- Status: Accepted
- Date: 2026-10-05

## Context

On Windows, Dawn translates WGSL to HLSL and compiles it with either FXC (`d3dcompiler_47.dll`) or
DXC (`dxcompiler.dll`). Which one it can use is a build option, and each library it loads is either
shipped by Jade or taken from the system. Roadmap task 6 left both questions open.

Verified on 2026-10-05 at the commits pinned in `build/versions.json`:

- Dawn `b1236a9`:
  - `DAWN_USE_BUILT_DXC` (default `OFF`) builds DirectXShaderCompiler from the
    `third_party/directx-shader-compiler/src` entry of `DEPS` (Microsoft's repository at
    `9757d44`) as the shared library `dxcompiler.dll`, and defines `DAWN_USE_BUILT_DXC`. Only then
    does the default platform report `kWebGPUUseDXC` (`src/dawn/platform/DawnPlatform.cpp`), which
    turns the `use_dxc` toggle on for adapters with shader model 6.0 or higher
    (`src/dawn/native/d3d12/PhysicalDeviceD3D12.cpp`). Without it, `use_dxc` is forced off.
  - Without DXC, the D3D12 backend rejects `shader-f16`, `subgroups`, `subgroup-size-control`,
    `chromium-experimental-sampling-resource-table` and `atomic-vec2u-min-max`, and
    `primitive-index` on Intel GPUs ("requires DXC for D3D12"), and polyfills packed 4x8 integer
    dot products and pack/unpack intrinsics. FXC is limited to shader model 5.1.
  - The D3D11 backend always uses FXC, and so does D3D12 on adapters below shader model 6.0.
  - `dxcompiler.dll` and `d3dcompiler_47.dll` are loaded at runtime by name, from Dawn's runtime
    search paths: the directory of `webgpu_dawn.dll`, then the executable's, then the default
    search (`LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS`), which includes
    `System32` (`src/dawn/native/Instance.cpp`, `src/dawn/common/DynamicLib.cpp`).
  - Dawn's CMake build copies `d3dcompiler_47.dll` from the Windows SDK into its build directory,
    unless `DAWN_FORCE_SYSTEM_COMPONENT_LOAD` is set; a comment in
    `src/dawn/native/d3d/PlatformFunctions.cpp` warns that the system copy "contains functionality
    and heap corruption bugs on older Windows versions".
  - Dawn's GitHub release builds set `DAWN_USE_BUILT_DXC` on Windows
    (`.github/workflows/dawn-ci.cmake`).
- DirectXShaderCompiler `9757d44`: `dxcompiler` includes the open-source validator
  (`tools/clang/tools/dxcvalidator`), which computes the retail hash that D3D12 checks. The separate
  `dxil.dll` of Microsoft's releases is not needed, and Dawn never loads it. The repository is
  under the University of Illinois/NCSA license, with the third-party parts listed in its
  `LICENSE.TXT` and `ThirdPartyNotices.txt`.
- Microsoft's DXC release `v1.9.2609` ships a `dxcompiler.dll` of 22 MB for x64 and 24 MB for
  arm64, as an order of magnitude; Dawn's build leaves out the SPIR-V code generator.
- `d3dcompiler_47.dll` is part of Windows 10 in `System32`. The Windows SDK copy is a proprietary
  Microsoft redistributable.

## Decision

- Windows builds of Dawn set `DAWN_USE_BUILT_DXC=ON`, and `Jade.Native.Wgpu` ships the resulting
  `dxcompiler.dll` next to `webgpu_dawn.dll` for `win-x64` and `win-arm64`.
- `d3dcompiler_47.dll` is not redistributed: FXC is the system's copy, used by the D3D11 backend and
  by adapters below shader model 6.0.
- DirectXShaderCompiler joins `THIRD-PARTY-NOTICES.md`, with the parts of its `LICENSE.TXT` and
  `ThirdPartyNotices.txt` that are not build or test dependencies.
- The Windows build (roadmap task 10) fetches the `third_party/directx-shader-compiler/src` entry of
  Dawn's `DEPS`, checks that `dxcompiler.dll` is produced for both architectures, and checks the
  notices against the files compiled into it, as for `linux-x64`
  ([0031](0031-native-build-definitions.md)).

## Consequences

- WebGPU applications on D3D12 get the same optional features as on Vulkan and Metal, with the
  compiler Dawn's own releases use.
- Every package stays built from source: no proprietary binary is shipped.
- Windows packages grow by one DLL of tens of megabytes per architecture, and the Windows native
  build compiles an LLVM fork, which lengthens it considerably.
- The D3D11 backend and old adapters depend on the system FXC, with the bugs Dawn mentions on old
  Windows 10 builds; Jade's minimum is Windows 10 1607 ([0024](0024-minimum-os-versions.md)).
- Nothing of this is verified yet: no Windows build exists until roadmap task 10, including the
  cross-architecture build of DXC for `win-arm64`.

## Alternatives considered

- **No DXC, FXC from the system**: nothing more to build or ship, but D3D12 loses `shader-f16` and
  subgroups and keeps the older, slower compiler.
- **DXC built, plus the Windows SDK's `d3dcompiler_47.dll`**: avoids the old system FXC, at the cost
  of shipping a proprietary binary in an MIT package.
- **Microsoft's prebuilt `dxcompiler.dll` and `dxil.dll`**: breaks the rule that every native is
  built by Jade ([0010](0010-native-builds-with-xmake.md)), and `dxil.dll` is proprietary.
