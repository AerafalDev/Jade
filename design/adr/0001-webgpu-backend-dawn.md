# ADR-0001: Dawn as the WebGPU implementation

- Status: Accepted
- Date: 2026-10-02

## Context

Jade needs WebGPU on desktop, mobile and the browser. There are two native implementations of
`webgpu.h`: Dawn (Google, C++) and wgpu-native (Rust).

In the browser, WebGPU comes from the browser itself. emdawnwebgpu, shipped in Dawn releases as
`emdawnwebgpu_pkg`, implements `webgpu.h` on top of it for Emscripten. Dawn and emdawnwebgpu both
come from the same `src/dawn/dawn.json`, so they expose the same header.

Facts checked on 2026-10-02:

- Dawn publishes a tag every day (`vYYYYMMDD.HHMMSS`; the latest was `v20260930.214659`).
- Dawn's official prebuilts cover win-x64, linux-x64, osx-x64, osx-arm64, Android and an Apple
  xcframework, plus headers and `emdawnwebgpu_pkg`. There are none for linux-arm64 or win-arm64.
- Those prebuilts contain only static libraries. On Linux, `libwebgpu_dawn.a` is about 45 MB, next
  to the Tint `.a` files.
- wgpu-native `v29.0.1.1` ships prebuilts for every desktop and mobile RID. Using it would still
  mean emdawnwebgpu on the web, so we would maintain two headers with different extensions:
  wgpu-native's `wgpu.h` on native and Dawn's on the web.

## Decision

- Dawn on every native RID, built from source by us ([ADR-0004](0004-native-build-with-xmake.md)).
- emdawnwebgpu on browser-wasm.
- WebGPU bindings are generated from the `dawn.json` of the pinned Dawn revision
  ([ADR-0005](0005-in-house-binding-generator.md)).
- Dawn is pinned to one release tag and bumped deliberately, never tracked automatically.

## Consequences

- We own a heavy C++ build (Abseil, Tint, D3D12/Vulkan/Metal backends) for about twelve RIDs. CI
  time, disk usage (14 GB on arm64 runners) and caching are first-class concerns.
- Dawn extensions are available on every native RID. On the web, only what emdawnwebgpu supports is
  available. The bindings must make that difference visible (task 202).
- There is no runtime switch between WebGPU implementations.
