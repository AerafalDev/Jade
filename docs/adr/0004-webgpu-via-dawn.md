# 0004. WebGPU through Dawn, from a single pinned commit

- Status: Accepted
- Date: 2026-10-05

## Context

Jade needs one GPU API that works on every target, including the browser. WebGPU's C API
(`webgpu.h`) is available natively through Dawn and in the browser through Emdawnwebgpu, which
is built from the Dawn source tree. Dawn also publishes `dawn.json`, a machine-readable
description of the API that a binding generator can consume directly.

## Decision

- GPU access goes through WebGPU, implemented by Dawn, pinned to a single commit recorded in
  `native/versions.json`.
- The browser uses Emdawnwebgpu built from the same commit, so `webgpu.h` is identical on every
  target and a single set of bindings serves them all.
- The binding generator reads `dawn.json` from the pinned commit.
- The projects keep the "Wgpu" name (`Jade.Wgpu`, `Jade.Native.Wgpu`) even though the
  implementation is Dawn.

## Consequences

- Updating Dawn is a deliberate change: bump the commit, rebuild every native, regenerate the
  bindings and review the API diff in the same pull request.
- Dawn is the heaviest native dependency to build (see [0010](0010-native-builds-with-xmake.md));
  native CI must avoid rebuilding it when its pinned version has not changed.
- The "Wgpu" name can be read as a reference to other WebGPU implementations. NuGet package IDs
  cannot be renamed once published, so the name gets a final check before the first
  publication.
