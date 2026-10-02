# ADR-0005: In-house binding generator

- Status: Accepted
- Date: 2026-10-02

## Context

The bindings are public ([ADR-0006](0006-binding-api-shape.md)) and should feel like .NET: methods on
handles, span, `in`, `ref` and `out` overloads. ClangSharpPInvokeGenerator produces mature but raw
1:1 output, with a style fixed by the tool. Dawn's `src/dawn/dawn.json` (about 200 KB, in the Dawn
repository) describes WebGPU better than its header does: lengths, optional values, handle
ownership, Dawn extensions. Its siblings `webgpu.yml` and `webgpu.json` exist in webgpu-headers.

## Decision

- The generator is our own C# file-based app in `scripts/generate-bindings/`
  ([ADR-0008](0008-scripts-as-file-based-apps.md)).
- Two readers produce one intermediate model:
  - a `dawn.json` reader for WebGPU;
  - a libclang reader (ClangSharp package, `21.1.8.4` on 2026-10-02) for C headers: SDL3,
    miniaudio and later libraries.
- One emitter turns the model into C#. Per-library configuration covers naming, annotations
  (pointer+count pairs, out parameters, nullability), exclusions and platform availability.
- C headers are parsed once per target triple. The generator reports any type whose size or layout
  differs between triples ([ADR-0006](0006-binding-api-shape.md)).
- Inputs come from what the native build staged (`artifacts/native/<host-rid>/include` and
  `metadata/dawn.json`), so bindings and binaries always come from the same revision.
- Output is committed under `src/Jade.Interop/Generated/<Lib>/*.g.cs`. Generation is
  deterministic: same inputs, byte-identical output. CI regenerates and fails on any diff.

## Consequences

- Higher upfront cost than an off-the-shelf tool, but full control over the public API.
- The libclang native runtime must be available wherever the generator runs, presumably through
  NuGet runtime packages. Task 201 verifies this.
- The generator gets no separate test project. Its tests are the committed output (reviewed diffs)
  plus the interop smoke tests (task 205).
