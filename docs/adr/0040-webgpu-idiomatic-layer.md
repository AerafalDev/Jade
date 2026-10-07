# 0040. WebGPU idiomatic layer

- Status: Accepted
- Date: 2026-10-07

## Context

[0008](0008-two-layer-interop.md) puts an idiomatic layer above the raw layer, and
[0029](0029-descriptors-and-chained-structs.md) decided how it takes descriptors and chained
structures. Roadmap task 12 generates that layer for `Jade.Wgpu`. 0029 left two questions open:
how output structures freed by `FreeMembers` are exposed, and how extensions of nested chain
roots are passed. [0032](0032-webgpu-raw-layer-generation.md) left three more: `required`
members, the constants made public, and the name of the `Buffer` handle, ambiguous with
`System.Buffer` under `using System;`. Generating every function also needs rules that no record
gives yet: where each function goes, how asynchronous operations and statuses are exposed, and
which constructs are written by hand.

Verified on 2026-10-07 in Dawn `b1236a9` (the commit of `build/versions.json`) and with SDK
`11.0.100-rc.1.26425.128`:

- `dawn.json` has 28 objects and the 276 functions of [0032](0032-webgpu-raw-layer-generation.md).
  Nine functions take a callback info: eight return a future (request adapter, request device,
  map async, create compute and render pipeline async, pop error scope, on submitted work done,
  get compilation info), and `wgpuDeviceSetLoggingCallback` keeps its callback. Every callback
  starts with a status enum that has a `success` value. The device descriptor holds the
  device-lost and uncaptured error callback infos; the Dawn-only cache device descriptor and
  shared buffer memory host pointer descriptor hold others. 41 method arguments have a `default`
  (`size` is `whole size`, `instance count` is `1`), which Dawn's C++ wrapper turns into default
  arguments (`render_cpp_default_value` in `generator/templates/api_cpp.h`).
- Ten output structures have a `FreeMembers` function (adapter info, surface capabilities, the
  three lists of supported features, the two end access states, the memory heaps and subgroup
  matrix configs of an adapter, the DRM format capabilities). Each frees its own members only:
  `APIAdapterInfoFreeMembers` deletes the four strings, and each extension has its own function
  (`src/dawn/native/Adapter.cpp`). `APISharedTextureMemoryEndAccessStateFreeMembers` also
  releases the fences it returned (`src/dawn/native/SharedTextureMemory.cpp`). The compilation
  info is only passed to a callback, valid during the call.
- Four roots are extended through a structure member rather than a parameter: bind group
  entries (two extensions), bind group layout entries (three), color target states (one) and the
  limits a device descriptor requires (three). All these extensions are value structures. Dawn
  reads the compatibility mode limits chained to the required limits (`src/dawn/native/Limits.cpp`).
- Dawn calls a device's lost callback exactly once: when the device is lost or destroyed, when
  its creation fails (`FailedCreation`, `src/dawn/native/Adapter.cpp`) and when the instance
  shuts down (`CallbackCancelled`). `DeviceLostEvent::Complete` clears the uncaptured error and
  logging callbacks, and waits until nothing uses them, before it calls the lost callback
  (`src/dawn/native/Device.cpp`).
- `dawn.json` does not record which members WebGPU requires. The sampler descriptor's `compare`
  has no default and is not optional there, while `WGPU_SAMPLER_DESCRIPTOR_INIT` sets it to
  `WGPUCompareFunction_Undefined`, which means no comparison.
- `dawn.json` has 12 constants: `whole size`, `whole map size`, `strlen`, `invalid binding`
  (Dawn only) and eight `*_UNDEFINED` sentinels, which the `*_INIT` defaults use.
- Of the 295 public and raw type names of `Jade.Wgpu`, only `Buffer` matches a non-generic type
  of the namespaces `ImplicitUsings` imports (`System.Buffer`), checked by reflection over the
  shared framework.
- C#: a `ref struct` can implement an interface whose `internal static abstract` members take an
  internal `ref struct`, and a generic method constrained by it with `allows ref struct` calls
  them without boxing. `Nullable<T>` cannot hold a `ref struct`. `GCHandle<T>` exists in .NET 11.
  The fields of a struct spread over several partial declarations raise CS0282. A method that
  takes a `ref struct` by `ref`, called with a span of a narrower scope, fails with CS8350 or
  CS9080 unless the parameters it never captures are `scoped` (throwaway apps, builds of
  `Jade.Wgpu`).
- On Linux with Vulkan and Wayland, releasing a device destroys the swap chain of the surface it
  presented to, and the driver then uses the window's display: releasing the window first
  crashes in `wl_proxy_marshal_flags` under `NativeDeviceRelease` (backtrace of the surface
  smoke test).

## Decision

This record supersedes [0029](0029-descriptors-and-chained-structs.md), whose other rules it
keeps, and replaces the statements of [0009](0009-interop-mapping-conventions.md) that
descriptors have `required` members and that chained output structures are passed by `ref`, and
that of [0032](0032-webgpu-raw-layer-generation.md) that the constants stay internal.

### Generation and configuration

- The generator projects the idiomatic layer from the model and the raw layer
  (`Projection/IdiomaticProjection.cs`) and writes it (`Emission/IdiomaticLayerEmitter.cs`,
  `MirrorEmitter.cs`, `MethodShape.cs`) into `Generated/`: new public types in `{Type}.g.cs`,
  and what it adds to a handle or a value structure of the raw layer in a second partial file,
  `{Type}.Idiomatic.g.cs`. Only a library whose `bindings.json` has an `idiomatic` object gets
  one, and it needs the `dawn.json` front-end, whose functions have an owner and a kind.
- `idiomatic` has three members, strict like the rest of the file: `constants` (a constant's C
  name and the C name of the handle or structure that declares it), `handWritten` (functions,
  and members as `Structure.member`, with the reason) and `skip` (functions and structures left
  out, with the reason). An entry that matches nothing fails the generator, and so does any
  construct the rules below do not cover, until the configuration names it. The report lists
  the skipped and hand-written entries and the internal constants.
- Hand-written code lives beside the generated files: `Arena`, `Utf8Text`, the extension
  interfaces, the exceptions, `GpuError`, the device callback delegates and the partials of
  `DeviceDescriptor`, `Device` and `GpuBuffer`.

### Classification

Structures are classified from how the functions use them. A structure is an input when a
function takes a constant pointer to it or takes it by value, or when an input holds it; an
output when a function takes a non-constant pointer to it or a callback receives a pointer to it,
or when an output holds it. An extension follows its roots. Then:

- value structures, as in [0029](0029-descriptors-and-chained-structs.md), serve both layers;
- an input with pointers is a **mirror**, a public `ref struct`, unless it is used as an array
  element, which makes it an **element mirror**, a public struct;
- an output with pointers is a **snapshot**, a managed copy (below);
- a structure with pointers that is both an input and an output fails the generator.

### Mirrors and lowering

Kept from [0029](0029-descriptors-and-chained-structs.md), with these rules:

- Members are public fields. Text is `Utf8Text` in a mirror (a UTF-8 span or a string, the null
  string distinct from the empty one) and `string?` in an element mirror. A pointer and its count
  become one span (`ReadOnlySpan<T>`, or `ReadOnlyMemory<T>` in an element mirror), the count set
  from it; a fixed length is checked. An array of C strings is a span of strings. An optional
  pointer to a value structure is a nullable value, `void*` is `nint`, and a function pointer
  keeps its unmanaged type.
- The parameterless constructor applies the `*_INIT` defaults of the raw constructor, except
  for text, whose default is already the null string. A member that points to an optional nested
  mirror passes a null pointer when the mirror is `default` and was never constructed, since
  `Nullable<T>` cannot hold a `ref struct`.
- A call pins with `fixed` the text and blittable spans of a descriptor parameter, its own span
  parameters and its `in` value parameters, and copies everything else into its arena: a
  `stackalloc` buffer of 1 KiB that grows into native blocks freed before the call returns. The
  arena never keeps a reference to what it is given, and its parameters are `scoped`. The module
  has `SkipLocalsInit`, so an unused arena costs one stack adjustment.
- The configuration of hot functions of 0029 is dropped: every method whose arguments are values,
  handles or spans is zero-copy by construction, and a descriptor costs a copy into stack memory.

### Chained structures

- Input extensions implement `IChainedExtension<TSelf, TRoot>` once per root, as in 0029, and a
  method that takes a root gets overloads with one and two extensions. Every extension is copied
  into the arena, a value structure too: an `in` argument may live on the heap, and a pin cannot
  outlive the lowering that takes its address.
- Output extensions implement `IChainedOutputExtension<TSelf, TRoot>`, whose internal members
  allocate an initialized extension in the arena and read it back, freeing what the library
  allocated for it. A method returns its output root and takes one or two of its extensions as
  generic `out` parameters; it links them for the call and unlinks the value it returns.
- **Nested roots get typed slots** (maintainer's decision). For each root a descriptor reaches
  through a member, a public struct `{Root}Extensions` has one nullable field per extension;
  the descriptor gains a `ReadOnlySpan<{Root}Extensions>` member, empty or one per element, or a
  `{Root}Extensions` member for a pointer (`BindGroupEntryExtensions`,
  `BindGroupLayoutEntryExtensions`, `ColorTargetStateExtensions`, `LimitsExtensions`). The
  elements are copied and linked only when a slot has a value. A slot holds value structures
  only; another extension of a nested root fails the generator.

### Output structures

- **Snapshots are immutable managed copies** (maintainer's decision): sealed classes with
  get-only properties, `string?` for text, `ImmutableArray<T>` for arrays and nested copies for
  nested structures, built from the raw structure by an internal constructor.
- The method copies the output and calls `FreeMembers` in a `finally` right after, so nothing the
  library allocated outlives the call. A handle in a copy gets its own reference, which its
  owner releases, since `FreeMembers` releases the library's. Output extensions with pointers are
  copies too, freed by their own `FreeMembers`.
- A value output is returned as is. The compilation info is copied in its callback.

### Methods

- Each function becomes a member of the handle it belongs to. A free function becomes a static
  member of the handle whose words its name contains, without them (`wgpuCreateInstance` gives
  `Instance.Create`); `wgpuGetProcAddress` is skipped.
- A getter without argument that returns a value the caller does not own is a property
  (`texture.Width`, `buffer.MapState`, `device.LostFuture`); one that returns a handle stays a
  method, since the caller owns the reference.
- Handles implement `IDisposable`: `Dispose` calls `Release`. `AddRef` and `Release` do nothing
  for a handle without object.
- Parameters: text gets two overloads, a UTF-8 span preferred by `[OverloadResolutionPriority(1)]`
  and a string ([0016](0016-public-api-conventions.md)); a constant pointer to a value structure
  is a `scoped in` parameter and to an input with pointers its mirror; an optional pointer gets an
  overload without it; a pointer and a count are a span, `params` when last; untyped data and its
  size are a span of a type parameter constrained to `unmanaged`, the size in bytes; a pointer
  the function writes with a count is a `Span<T>`; `void*` is `nint`. The documented argument
  defaults become optional parameters where every following parameter has one
  (`Draw(uint vertexCount, uint instanceCount = 1, …)`).
- Results: a `WGPUStatus` other than success throws `WgpuException<Status>`; an output pointer
  becomes the return value; a function that returns a future and takes a callback info becomes
  an `…Async` method that returns a `Task`.

### Asynchronous operations

- The method registers its callback with `WGPUCallbackMode_AllowProcessEvents`, a
  `TaskCompletionSource` with `RunContinuationsAsynchronously`, so that no application code runs
  inside the native callback, and a `GCHandle<T>` to it as userdata, which the generated
  `[UnmanagedCallersOnly]` trampoline frees. A success status completes the task with the
  callback's result (a handle, or a copy); any other status faults it with
  `WgpuException<TStatus>`, which carries the status and the library's message.
- The task completes when the application calls `Instance.ProcessEvents`; the future the
  function returns is not exposed. `Device.PopErrorScopeAsync`, whose callback reports an error
  and a message on success, is written by hand and returns a `GpuError`.

### Device callbacks

- `DeviceDescriptor.DeviceLost` and `DeviceDescriptor.UncapturedError` are fields the generator
  declares for the callback infos marked hand-written, typed with the delegate named after the
  callback (`DeviceLostCallback`, `UncapturedErrorCallback`); a hand-written partial method
  lowers them. Both use one `GCHandle`, freed by the lost callback, registered with
  `AllowSpontaneous`: Dawn calls it exactly once per device and calls no uncaptured error
  callback after it.
- The callbacks run on any thread; an exception they throw ends the process. The logging callback
  and the Dawn-only descriptors with callbacks are skipped: nothing tells when their callbacks
  stop.

### `required` members

**None** (maintainer's decision), on mirrors and on value structures: `new T()` applies the C
initializer and the implementation validates, through error scopes and the uncaptured error
callback. `dawn.json` cannot tell which members WebGPU requires.

### Public constants

**Each public constant is declared on the type it concerns** (maintainer's decision), from
`idiomatic.constants`: `GpuBuffer.WholeSize` and `GpuBuffer.WholeMapSize`,
`Limits.LimitU32Undefined` and `LimitU64Undefined`, `TextureViewDescriptor.MipLevelCountUndefined`
and `ArrayLayerCountUndefined`, `TexelCopyBufferLayout.CopyStrideUndefined`,
`RenderPassColorAttachment.DepthSliceUndefined`,
`RenderPassDepthStencilAttachment.DepthClearValueUndefined` and
`PassTimestampWrites.QuerySetIndexUndefined`. A constant whose value is not a C# constant
(`nuint.MaxValue`) is a static property. `WGPU_STRLEN`, which `Utf8Text` hides, and
`WGPU_INVALID_BINDING`, which nothing uses, stay internal.

### Name of the buffer handle

The handle is **`GpuBuffer`** (maintainer's decision), through `names` (`WGPUBuffer`), as
`GPUBuffer` in JavaScript. The types that only contain the word (`BufferDescriptor`,
`BufferUsage`) keep it, since they collide with nothing.

### Verification

`tests/Jade.Wgpu.Tests` checks the managed side of the layer (defaults, constants, text, arena,
chains, slots, copies, statuses) and, on the host, drives the GPU through the idiomatic layer
only: adapter and device requests with output extensions, a buffer round trip, a triangle
rendered into a texture and read back, error scopes, uncaptured errors, device loss and a failed
task. It clears the surface of a window over three frames. SDL3 creates the window, loaded with
`NativeLibrary` as the layout tests load their libraries, since the raw layer of `Jade.Sdl` is
internal to its own tests. The tests release a surface and its device before the window.

## Consequences

- The idiomatic layer adds 168 generated files and 870 public declarations (261 members on 28
  handles, 62 mirrors, 3 element mirrors, 15 snapshots, 55 extended value structures, 4 slot
  types). A Dawn update that adds a construct the rules do not cover fails the generator until
  the rules or the configuration cover it.
- Application code reads as .NET (`device.CreateBuffer(new BufferDescriptor { Label = "vertices",
  Usage = BufferUsage.Vertex, Size = 64 })`), while the raw layer stays available internally.
- The application must pump `Instance.ProcessEvents` for tasks to complete; the engine's loop
  will do it (roadmap task 14). Whether Emdawnwebgpu delivers these callbacks in the browser is
  checked by task 15.
- Code in a namespace under `Jade.Wgpu` that uses raw structures with an idiomatic counterpart
  must qualify them with `Raw.`, since the enclosing namespace is searched first.
- A surface, and the device that presented to it, must be released before the window; the
  desktop sample follows that order.
- Snapshots allocate, on cold paths; strings passed as `string` are encoded for each call; element
  mirrors allocate their arrays, as 0029 expected.
- Not verified: the surfaces of Windows, macOS, Android and iOS, and the browser; NativeAOT
  publication (the trim and AOT analyzers report nothing). The callback mode and the 46
  C++-mangled functions of Emdawnwebgpu remain for task 15.

## Alternatives considered

- **`ref struct` views of output structures**, spans over the library's memory freed by
  `Dispose`: no allocation, but copying the view by assignment then disposing both frees twice,
  and a span kept after `Dispose` reads freed memory; these structures are read on cold paths.
- **Nested-root extensions left out**, or passed through per-call tables of element indexes: the
  first leaves external textures and compatibility limits unreachable, the second reads poorly
  and holds one extension type per call.
- **`required` members from a list taken from the WebGPU IDL**, or from `dawn.json` members
  without default: the first is a list to maintain with every Dawn update, the second is wrong
  (`compare`), and neither fits value structures, which also serve as outputs.
- **All constants in one static class**: a name like `Wgpu` would hide the namespace `Jade.Wgpu`
  inside the engine's own namespaces.
- **`DeviceBuffer`, or keeping `Buffer`**: the first leaves the WebGPU vocabulary, the second
  makes every user write an alias under `using System;`.
- **`AllowSpontaneous` callbacks for tasks**: tasks would complete without `ProcessEvents`, but
  on the library's threads, at any point of a call into it; `AllowProcessEvents` completes them
  where the application pumps events, which the engine's loop does anyway.
- **`Try…` methods returning `bool` for statuses**: an error status means a misuse, such as
  presenting an unconfigured surface, which an exception reports better.
