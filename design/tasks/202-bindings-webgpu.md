# 202: WebGPU bindings from dawn.json

- Depends on: 201, 102, 003
- ADRs: 0001, 0005, 0006

## Goal

The complete WebGPU API, including Dawn extensions, generated from the staged `dawn.json` into
`src/Jade.Interop/Generated/WebGpu/`. Handles have instance methods, and the friendly overloads come
from the metadata.

## Context

- `metadata/dawn.json` is staged by 102 from the pinned Dawn revision. The same revision produces
  the staged `webgpu.h`; use the header only to cross-check the result.
- `dawn.json` describes objects (handles and their methods), structures with chained `nextInChain`
  extensions, callbacks (callback info structs with modes), string views, and features or tags that
  distinguish Dawn-only (native) from emdawnwebgpu (web) availability. Read its schema in the Dawn
  sources rather than assuming.
- From 201: build the `dawn.json` reader into the existing `LibraryModel`. `VarianceCheck` and
  `LayoutTestEmitter` already accept any list of `TargetModel`. Per-library config follows
  `Sdl3Config.cs`. `WGPUBool` is a `uint32_t` typedef: decide between `uint` and a 4-byte wrapper
  (ADR-0012), and justify the choice.
- From 102: the staged `include/webgpu/webgpu.h` only includes `dawn/webgpu.h`, and
  `metadata/dawn.json` is byte-identical to the tag's `src/dawn/dawn.json`. Exclude
  `emscripten_webgpu_get_device`, which is declared but defined only in Emscripten builds. This
  build is WGSL-only: `WGPUShaderSourceSPIRV` exists in the header and is reported by
  `wgpuHasInstanceFeature`, but a SPIR-V module fails validation ("SPIR-V is disallowed."). Bind
  it, and document that it is rejected at runtime.
- From 102: the Null backend answers adapter requests without a GPU (adapter type `Unknown`), so
  the device test can run on headless CI with it. Device creation on Null has not been tried.

## Scope

- `dawn.json` reader into the 201 model; no emitter fork.
- Handles: instance methods; `AddRef`/`Release` exposed as-is, with no ownership semantics
  (ADR-0006).
- Chained structs: a zero-cost way to build `nextInChain` chains from C#. Design it, show it in the
  Outcome, and keep it allocation-free.
- Callbacks: callback-info structs with `delegate* unmanaged[Cdecl]` and `userdata` pointers. No
  delegates.
- `WGPUStringView` in and out: `ReadOnlySpan<byte>` overloads, no NUL required.
- Platform availability: APIs absent from emdawnwebgpu are marked (attribute or separate partial
  class, your choice) so engine code can tell. Document how the information was derived.
- Cross-check: every exported `wgpu*` function in `libjade_native.so` has a binding, and every
  binding has an export. Bindings with no export are allowed only for documented web-only APIs.

## Out of scope

- Safe wrappers, resource ownership, surface creation helpers (engine layer).

## Acceptance criteria

- [ ] Deterministic output; the solution builds with 0 warnings.
- [ ] The export cross-check passes, with exceptions listed.
- [ ] A test creates an instance, requests an adapter and a device, creates a buffer, writes it
      through the span overload, and releases everything. It is skipped with a clear reason when no
      adapter is available (headless CI).

## Verification

Generator run, build, export cross-check output, test run. Include a code excerpt showing a handle
method, a chained struct and a callback.

## Pitfalls

- Async operations (adapter and device requests, buffer mapping) complete through
  `wgpuInstanceProcessEvents` or `WaitAny`, depending on callback mode. The test must drive them.
- `WGPUBool` is 32-bit.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
