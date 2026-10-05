# 0006. miniaudio for audio, with opaque platform-dependent structs

- Status: Accepted
- Date: 2026-10-05

## Context

miniaudio provides audio on every Jade target from a single C library. Several of its structs
(`ma_device`, `ma_engine`, `ma_sound`, among others) have a size and layout that depend on the
platform and on the `MA_*` configuration defines, so a single C# definition of them cannot be
correct everywhere.

## Decision

- Audio goes through miniaudio.
- Structs whose size depends on the platform or on `MA_*` defines are opaque on the C# side. They
  are allocated and freed by a small C shim that lives in `native/` and is built together with
  miniaudio.
- The `MA_*` defines are defined once and used identically for the native build and for header
  parsing by the binding generator.
- On mobile, the audio device is stopped and restarted on SDL3 lifecycle events.

## Consequences

- The C# side only holds pointers to these structs; their fields are reached through miniaudio
  functions or shim accessors.
- The shim is C code in the repository and is covered by CodeQL C/C++ analysis (see
  [0017](0017-github-repository-baseline.md)).
- Changing a `MA_*` define means rebuilding the natives and regenerating the bindings together.
