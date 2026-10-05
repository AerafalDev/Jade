# Architecture Decision Records

Important or hard-to-reverse decisions are recorded here, one per file, as described in
[0001](0001-record-architecture-decisions.md). New records start from [`template.md`](template.md).

| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-license-and-public-identity.md) | MIT license and a technology-neutral public identity | Accepted |
| [0003](0003-csharp-15-and-dotnet-11.md) | C# 15 and .NET 11, without preview features | Accepted |
| [0004](0004-webgpu-via-dawn.md) | WebGPU through Dawn, from a single pinned commit | Accepted |
| [0005](0005-sdl3-platform-layer.md) | SDL3 as the platform layer, without its audio subsystem | Accepted |
| [0006](0006-miniaudio-for-audio.md) | miniaudio for audio, with opaque platform-dependent structs | Accepted |
| [0007](0007-in-house-binding-generator.md) | In-house binding generator, with committed output | Accepted |
| [0008](0008-two-layer-interop.md) | Two interop layers: raw blittable and idiomatic | Accepted |
| [0009](0009-interop-mapping-conventions.md) | Interop mapping conventions | Accepted |
| [0010](0010-native-builds-with-xmake.md) | Native libraries built by us with xmake | Accepted |
| [0011](0011-native-package-layout.md) | Layout of the Jade.Native packages | Accepted |
| [0012](0012-supported-targets.md) | Supported targets | Accepted |
| [0013](0013-browser-native-toolchain.md) | Browser natives prebuilt with the workload's Emscripten version | Superseded by [0025](0025-browser-natives-with-workload-emscripten.md) |
| [0014](0014-repository-layout-and-conventions.md) | Repository layout and content conventions | Superseded by [0020](0020-repository-layout-and-conventions.md) |
| [0015](0015-build-and-packaging-conventions.md) | Build and packaging conventions | Superseded by [0021](0021-build-and-packaging-conventions.md) |
| [0016](0016-public-api-conventions.md) | Public API conventions | Accepted |
| [0017](0017-github-repository-baseline.md) | GitHub repository settings and supply-chain baseline | Accepted |
| [0018](0018-test-framework.md) | MSTest as the test framework | Accepted |
| [0019](0019-browser-and-roslyn-component-targeting.md) | Targeting of the browser interop project and the Roslyn components | Accepted |
| [0020](0020-repository-layout-and-conventions.md) | Repository layout and content conventions | Superseded by [0023](0023-repository-layout-and-conventions.md) |
| [0021](0021-build-and-packaging-conventions.md) | Build and packaging conventions | Accepted |
| [0022](0022-ci-runners-and-caching.md) | CI runners and caching | Accepted |
| [0023](0023-repository-layout-and-conventions.md) | Repository layout and content conventions | Accepted |
| [0024](0024-minimum-os-versions.md) | Minimum operating system versions | Accepted |
| [0025](0025-browser-natives-with-workload-emscripten.md) | Browser natives built with the .NET workload's Emscripten toolchain | Accepted |
| [0026](0026-binding-generator-pipeline.md) | Binding generator pipeline and intermediate representation | Accepted |
| [0027](0027-interop-mapping-rules.md) | Remaining interop mapping rules | Accepted |
| [0028](0028-internal-raw-interop-layer.md) | Internal raw interop layer | Accepted |
| [0029](0029-descriptors-and-chained-structs.md) | Descriptors and chained structures in the idiomatic layer | Accepted |
| [0030](0030-d3d12-shader-compilers.md) | D3D12 shader compilers: a built DXC is shipped, FXC comes from the system | Accepted |
| [0031](0031-native-build-definitions.md) | Native build definitions and the host build | Accepted |
