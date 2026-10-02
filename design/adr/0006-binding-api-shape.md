# ADR-0006: Shape of the public bindings

- Status: Accepted
- Date: 2026-10-02

## Context

The bindings are a public API of Jade. Users can drop down to raw WebGPU, SDL3 and the others for
anything the engine does not expose. They must cost nothing over a raw P/Invoke, work under
NativeAOT and trimming, and still read like .NET.

## Decision

Rules the generator enforces:

- **Namespaces**: `Jade.Interop.<Lib>` (`WebGpu`, `Sdl3`, `Miniaudio`, ...), all public.
- **Names**: library prefixes are stripped (`wgpu`/`WGPU`, `SDL_`, `ma_`, ...) and names are
  PascalCase. Enums become C# enums with prefix-free members; bitmasks get `[Flags]`. Per-library
  config fixes the remaining names, such as the static class that holds free functions.
- **Raw layer, always present**: every C function has a 1:1 static method with pointer parameters.
  It is the reference the friendly overloads forward to.
- **Handles**: a `readonly struct` wrapping the native pointer, of the same size and blittable. A
  function whose first parameter is the handle also becomes an instance method:
  `device.CreateBuffer(in descriptor)`. Handles carry no ownership semantics: no `IDisposable`, no
  finalizer. Struct copies would make release-on-dispose unsafe, and ownership belongs to the
  engine.
- **Friendly overloads**, generated only when metadata or config proves them correct:
  `ReadOnlySpan<T>`/`Span<T>` for pointer+count pairs, `in` for const struct pointers, `ref`/`out`
  for in/out parameters, `ReadOnlySpan<byte>` for UTF-8 strings. NUL-terminated C strings need a
  terminated copy (stack for small, pooled for large), never a GC allocation. WebGPU string views
  carry their own length.
- **No GC allocation, no exceptions** in generated code. Pinning uses `fixed`.
- **Callbacks** are `delegate* unmanaged[Cdecl]<...>`. No managed delegates.
- **Booleans and widths**: C `bool`/`_Bool`, `WGPUBool` and `long` map to explicitly sized types
  chosen per library (for example `CLong` and `CULong` for `long`).
- **Cross-target layout**: a type whose size or layout differs between target triples is not
  exposed by value under one definition. It becomes opaque behind a handle, gets an accessor shim,
  or is split per platform. Per-library config records which, never silently.
- **Docs**: XML doc comments come from header comments or `dawn.json` docs when available.

```csharp
using Jade.Interop.Sdl3;
using Jade.Interop.WebGpu;

Window* window = Sdl.CreateWindow("Jade"u8, 1280, 720, WindowFlags.Resizable);
GpuBuffer buffer = device.CreateBuffer(in bufferDescriptor);
queue.WriteBuffer(buffer, 0, vertices);   // ReadOnlySpan<Vertex> overload
```

The exact type and class names are illustrative. Tasks 201 to 204 fix them per library in the
generator config.

## Consequences

- Every rule is a generator feature. That is a large surface, built incrementally: core in 201,
  then one library per task.
- Changes to generated public names are breaking changes for users once a version is released.
