# 0029. Descriptors and chained structures in the idiomatic layer

- Status: Superseded by [0040](0040-webgpu-idiomatic-layer.md)
- Date: 2026-10-05

## Context

The idiomatic layer ([0008](0008-two-layer-interop.md)) takes spans, strings and nested structures
where the C API takes pointers, and must stay allocation-free on hot paths. WebGPU descriptors nest
arrays of structures that hold strings and pointers, and many structures can be extended through a
`nextInChain` list of structures identified by an `sType`. The roadmap proposed `ref struct` mirrors
copied into an arena on cold paths, and `fixed` without copy on hot paths.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128` and the `dawn.json` pinned in
`build/versions.json`:

- `ReadOnlySpan<T>` rejects a `ref struct` element type (CS9244, and CS9358 for a collection
  expression). An array of descriptors therefore cannot be a span of `ref struct` mirrors.
- Of the 181 structures of `dawn.json`, 19 hold no pointer, 78 hold no pointer except
  `nextInChain`, and 84 hold strings, arrays or other pointers. Three arrays have elements that need
  a conversion: `vertex state.buffers` (`vertex buffer layout` holds an array),
  `fragment state.targets` (`color target state` holds an optional pointer) and the `constants` of
  the shader stages (`constant entry` holds a string). `WGPUStringView` carries its length, so a
  UTF-8 span maps to it without a terminator.
- 88 structures are chained; `dawn.json` lists the roots each one may extend, and some extend
  several (`dawn toggles descriptor` extends the instance descriptor, the adapter request options
  and the device descriptor).
- A prototype with a `ref struct` descriptor, an extension passed as a generic parameter that
  `allows ref struct`, a static abstract lowering method and an arena over a `stackalloc` buffer
  allocates 0 bytes over 1,000 calls.
- A public interface can declare an `internal static abstract` member whose signature uses internal
  types, and a public generic method constrained by that interface compiles and runs. Other
  assemblies cannot implement such an interface.

## Decision

### Classification

The generator classifies every structure from the intermediate representation
([0026](0026-binding-generator-pipeline.md)):

- **Value structures** hold no pointer, or only `nextInChain`. One public blittable struct serves
  both layers ([0028](0028-internal-raw-interop-layer.md)); `nextInChain` is an internal field, null
  by default. Value structures are passed by reference or in spans and pinned as they are: never
  copied.
- **Descriptors** are input structures with strings, arrays, pointers to structures or callbacks.
  The raw form is internal; the public form is a `ref struct` mirror. Strings become a text type
  that accepts both UTF-8 spans and `string` ([0016](0016-public-api-conventions.md)), arrays
  become `ReadOnlySpan<T>`, optional pointers to value structures become nullable values, and nested
  descriptors become nested mirrors. Mirrors keep the parameterless constructor with schema defaults
  and the `required` members of [0009](0009-interop-mapping-conventions.md).
- **Element mirrors** are descriptors used as array elements. They cannot be `ref struct`s, so they
  are regular structs whose arrays are `ReadOnlyMemory<T>` and whose strings are `string`.

### Lowering

- The generated wrapper pins every top-level span of a mirror with `fixed` and passes value
  structures by address: nothing is copied.
- What lies below a nested mirror, element mirrors, `string` values and chained extensions that hold
  pointers are copied into an arena: a `ref struct` over a buffer that the wrapper allocates with
  `stackalloc`, which grows into native memory freed before the wrapper returns. No managed memory
  is allocated and no state is shared between threads.
- This relies on the library not keeping pointers into a descriptor after the call returns. The
  configuration marks any function that does; such a function is never lowered through the arena.
- Hot paths need no separate API: a function whose arguments lower without the arena is zero-copy by
  construction. The configuration marks hot functions, and the generator fails if a hot function
  would need the arena.

### Chained structures

- The raw chain header (`WGPUChainedStruct`) is internal. Roots hold its pointer in `nextInChain`;
  extensions start with it.
- Each extension implements the public `IChainedExtension<TSelf, TRoot>` once for every root that
  `dawn.json` lists for it. The interface's members are `internal static abstract`: they give the
  `sType` and lower the extension, into the arena when it holds pointers. Only the generated
  extensions can implement it.
- A function that takes a root descriptor gets generic overloads with one and with two extensions,
  constrained by `IChainedExtension<TExtension, TRoot>` and `allows ref struct`: the compiler
  rejects an extension that does not belong to the root, and nothing is boxed or allocated. A root
  that needs more extensions at once gets a hand-written overload.
- An output chain is passed by `ref` ([0009](0009-interop-mapping-conventions.md)): the wrapper sets
  each extension's `sType`, links the chain for the call and unlinks it afterwards.
- Extensions of nested roots, such as array elements or the limits pointed to by a device
  descriptor, are exposed by roadmap task 12 through the same interface.

## Consequences

- Common descriptors cost no copy and no allocation; nested ones cost a copy into stack memory
  during the call.
- Element mirrors allocate when the caller builds their arrays for each call; arrays built once,
  such as fixed vertex layouts, avoid it. These descriptors (pipeline creation) are on cold paths.
- Value structures expose `nextInChain` to nobody: extending an element such as a bind group entry
  waits for task 12.
- The text type keeps null and empty strings distinct, as `WGPUStringView` requires
  ([0009](0009-interop-mapping-conventions.md)).
- Output structures that the library fills in, and frees with its `FreeMembers` functions, are not
  covered here; task 12 decides how they are exposed.

## Alternatives considered

- **`ref struct` mirrors everywhere with a thread-static arena**, as the roadmap first proposed:
  arrays of descriptors cannot hold `ref struct`s (CS9244), and a thread-static arena is shared
  state that a reentrant call or a callback could reset under a running call.
- **Raw structures with helper methods, no mirrors**: the least code, but users would handle
  pointers, against [0008](0008-two-layer-interop.md).
- **One named property per known extension on each root mirror**: discoverable, but roots such as
  the surface descriptor have more than ten extensions, and an optional `ref struct` has no natural
  representation.
- **An explicit chain builder**: covers nested roots too, but is more verbose and checks less at
  compile time.
