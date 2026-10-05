# 0011. Layout of the Jade.Native packages

- Status: Accepted
- Date: 2026-10-05

## Context

Native binaries reach applications through NuGet. Desktop and Android consume shared libraries
from the standard `runtimes/{rid}/native` folders, while iOS and the browser link native code
statically and need MSBuild items instead.

## Decision

- One package per native dependency: `Jade.Native.Wgpu`, `Jade.Native.Sdl`,
  `Jade.Native.MiniAudio`.
- Desktop and Android binaries go under `runtimes/{rid}/native/`. The universal macOS binaries go
  under `runtimes/osx/native/`.
- iOS uses `buildTransitive/` targets that add a `NativeReference` to an xcframework containing the
  device slice and the universal simulator slice.
- The browser uses `buildTransitive/` targets that add `NativeFileReference` items for the static
  archives, plus the Emdawnwebgpu JavaScript library for the `emcc` link (see
  [0013](0013-browser-native-toolchain.md)).

## Consequences

- Applications reference the interop packages and get the natives transitively, with no manual
  copy step.
- The `buildTransitive/` targets are part of the public contract of the packages and need tests
  through the iOS and browser samples.
