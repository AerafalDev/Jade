# 0028. Internal raw interop layer

- Status: Superseded by [0034](0034-raw-layer-with-dotnet-names.md)
- Date: 2026-10-05

## Context

[0008](0008-two-layer-interop.md) splits each interop assembly into a raw blittable layer and an
idiomatic layer on top, and leaves open whether the raw layer is public, in a sub-namespace, or
internal. Every interop assembly is a package that tracks its public API and passes package
validation ([0021](0021-build-and-packaging-conventions.md)).

Verified on 2026-10-05:

- PublicApiAnalyzers analyzes generated code and reports on it
  (`GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics` in
  `DeclarePublicApiAnalyzer.cs`, `dotnet/roslyn` main), and also tracks APIs marked
  `[Experimental]`. A public raw layer adds every C function, structure and member to
  `PublicAPI.*.txt`.
- With `GenerateDocumentationFile` and warnings as errors, CS1591 requires an XML comment on every
  public member.
- The raw layer would mirror more than the upstream WebGPU API: 127 of the 347 entries of
  `dawn.json` at the pinned commit carry the `dawn` tag of Dawn's own extensions; 212 are untagged,
  shared by every header variant.
- Every function is wrapped by the generated idiomatic layer ([0008](0008-two-layer-interop.md)),
  and a prototype of that layer (a `ref struct` descriptor, a generic chained extension and an arena
  on the stack) allocates nothing over 1,000 calls with SDK `11.0.100-rc.1.26425.128`.

## Decision

- Raw functions and the raw forms of structures that hold pointers are `internal`. They keep their
  C names (`wgpuDeviceCreateBuffer`, `WGPUBufferDescriptor`, `SDL_CreateWindow`), so the C API can
  be searched directly in the generated code.
- Types that are identical in both layers are public, with .NET names: enums, flags, handles, and
  structures whose only pointer is `nextInChain` ([0029](0029-descriptors-and-chained-structs.md)).
  They are part of the idiomatic API.
- Handles expose their native pointer as a public `nint`, so that an application can call a native
  function the idiomatic layer does not cover through its own `LibraryImport`.
- `InternalsVisibleTo` is granted only to the interop assembly's test project, for the layout tests
  ([0009](0009-interop-mapping-conventions.md)). The engine uses the idiomatic layer.

## Consequences

- The public API of each interop package is its idiomatic layer: a Dawn update changes it only where
  the idiomatic surface changes, and `PublicAPI.*.txt` tracks that surface alone.
- Internal raw declarations need no XML documentation for CS1591; they still carry a summary that
  names their C declaration ([0027](0027-interop-mapping-rules.md)).
- The idiomatic layer must cover every function an application may need, and stay allocation-free
  on hot paths; the generator produces it for every function, completed by hand-written partials.
- Making the raw layer public later is an additive change; making a public raw layer internal would
  have broken applications.
- Interop with another WebGPU, SDL3 or miniaudio binding goes through handles and native pointers
  only.

## Alternatives considered

- **Public raw layer in a sub-namespace**: a complete escape hatch, but every Dawn update breaks two
  public layers, and every raw member must be tracked in `PublicAPI.*.txt` and documented.
- **Public raw layer marked `[Experimental]`**: users opt in and get no compatibility promise, but
  the analyzers still track and require documentation for every member.
