# 0005. SDL3 as the platform layer, without its audio subsystem

- Status: Accepted
- Date: 2026-10-05

## Context

Each target has its own windowing, input, file access and application lifecycle model. SDL3
abstracts them for desktop, mobile and the browser. Audio is handled by a dedicated library
(see [0006](0006-miniaudio-for-audio.md)), so two audio stacks must never compete for the device.

## Decision

- SDL3 provides windowing, input, file access and lifecycle events.
- The SDL3 audio subsystem is never initialized.
- SDL3 bindings are generated from its C headers (see [0007](0007-in-house-binding-generator.md)).
- On Android, the application's `MainActivity` derives from `SDLActivity`.

## Consequences

- Android: moving to the background destroys the native surface. The WebGPU surface must be
  recreated when the application returns to the foreground, driven by SDL3 lifecycle events.
- The same lifecycle events stop and restart the audio device on mobile.
- The Java side of SDL3 must be shipped to Android applications; how it is packaged is settled
  with the Android sample.
- In the browser, SDL3 is a prebuilt Emscripten archive (see
  [0013](0013-browser-native-toolchain.md)).
