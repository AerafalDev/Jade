# 0008. Two interop layers: raw blittable and idiomatic

- Status: Accepted
- Date: 2026-10-05

## Context

The interop must run without runtime code generation (NativeAOT, iOS AOT, Mono WebAssembly) and
with predictable costs, while the engine and its users need a safe, idiomatic C# API. One layer
cannot serve both goals without compromise.

## Decision

- **Raw layer**: a direct image of the C API. Signatures are strictly blittable, the assembly is
  marked `[assembly: DisableRuntimeMarshalling]`, and nothing relies on runtime code generation.
- **Idiomatic layer**, built on the raw layer: `Span<T>` and `ReadOnlySpan<T>`, `in`/`ref`/`out`
  parameters, unmanaged function pointers, methods placed on the type they operate on, and
  extension members where relevant.
- Both layers are produced by the generator (see [0007](0007-in-house-binding-generator.md)),
  completed by hand-written partials where generation is not worth it.

## Consequences

- No marshalling stubs are generated at runtime; the same code works under JIT, NativeAOT, iOS AOT
  and Mono WebAssembly.
- The idiomatic layer must stay a thin, allocation-free wrapper on hot paths.
- Whether the raw layer is public (in a sub-namespace) or internal is still open (see the
  roadmap).
