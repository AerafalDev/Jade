# ADR-0016: Function pointers are `nint` in import signatures

- Status: Accepted
- Date: 2026-10-03
- Amends: the callback rule of ADR-0006, for import declarations only

## Context

ADR-0006 types callbacks as `delegate* unmanaged[Cdecl]<...>` everywhere. Survey 301 found that, on
browser-wasm, function pointer types are not supported in P/Invoke signatures. Microsoft Learn's
"ASP.NET Core Blazor WebAssembly native dependencies" (.NET 10, read on 2026-10-03) says: "For C#
function pointer types in `[DllImport]` methods, use `IntPtr` in the method signature on the managed
side instead of `delegate *unmanaged<int, void>`", citing dotnet/runtime#56145.

`[LibraryImport]` forwards to an inner `[DllImport]` with the same blittable types, so it carries the
same limitation. Jade targets browser-wasm (ADR-0007) from a single `Jade.Interop` assembly, so the
signatures cannot differ per platform.

## Decision

- In every generated import declaration (the `[LibraryImport]`/`[DllImport]` method), a parameter or
  return value of function pointer type is declared as `nint`.
- The public API keeps the typed form. The raw 1:1 method and the friendly overloads take and return
  `delegate* unmanaged[Cdecl]<...>`, and cast to and from `nint` around the private import. The cast
  is free.
- Struct fields of function pointer type stay typed: the limitation is about P/Invoke signatures, not
  layouts. Task 105 confirms this on browser-wasm and reports otherwise.

## Consequences

- Task 202, which brings WebGPU's callbacks, implements the rule in the generator for every library
  and regenerates SDL3. The public SDL3 API does not change.
- Task 105 verifies a callback round-trip on browser-wasm (native code calling an
  `[UnmanagedCallersOnly]` method passed through such a parameter).
