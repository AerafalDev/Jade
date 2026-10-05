# 0034. Raw interop layer with .NET names in a `Raw` namespace

- Status: Accepted
- Date: 2026-10-05

## Context

[0028](0028-internal-raw-interop-layer.md) made the raw layer internal and kept the C names of its
declarations: `WGPUBufferDescriptor`, `ma_engine_config`, `wgpuDeviceCreateBuffer`,
`SDL_CreateWindow`, with C member and parameter names. Generating the SDL3 and miniaudio raw layers
(roadmap task 8, [0033](0033-c-header-raw-layer-generation.md)) put about 180 such types next to
the public ones, and the maintainer found the mix of `ma_..._...` names with .NET names unfit for
the code that the idiomatic layer and its hand-written partials will be written in.

The C names also served a purpose: the descriptor mirrors of
[0029](0029-descriptors-and-chained-structs.md) are public `ref struct`s that take the .NET name of
the descriptor (`BufferDescriptor`), so the raw form needs another name.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`, by building the three regenerated
interop assemblies:

- A type of the namespace `Jade.Wgpu.Raw` finds the types of `Jade.Wgpu` without a using
  directive, since C# looks up the enclosing namespaces before the using directives; `Buffer`
  there still names `Jade.Wgpu.Buffer`, not `System.Buffer`.
- `LibraryImport` imports a function under another name through `EntryPoint`.
- The public API of `Jade.Wgpu` is unchanged by the renaming: `PublicAPI.Unshipped.txt` needs no
  update.

## Decision

This record supersedes [0028](0028-internal-raw-interop-layer.md), whose other rules it keeps, and
replaces the statements of [0027](0027-interop-mapping-rules.md) and
[0032](0032-webgpu-raw-layer-generation.md) that give raw declarations their C names.

- Every generated declaration has a .NET name, built by the rules of
  [0027](0027-interop-mapping-rules.md) and [0033](0033-c-header-raw-layer-generation.md), with the
  exceptions of the configuration's `names` and `words`.
- Types identical in both layers (enums, flags, handles, booleans, value structures and their inline
  arrays) are public, in the library's namespace (`Jade.Wgpu`), as before.
- The raw forms of the other structures, their inline arrays and `NativeMethods` are internal, in
  the namespace `Jade.<Library>.Raw`, and their files in `Generated/Raw/`: `Raw.BufferDescriptor`,
  `Raw.EngineConfig`, `Raw.NativeMethods`. The library's namespace keeps those names free for the
  idiomatic types. No public type and raw type share a name, in either namespace, so that one name
  never means two things; a public type refers to a raw one as `Raw.ChainedStruct`.
- Structure members are PascalCase; miniaudio's Hungarian prefixes are dropped through
  `memberPrefixes` (`pUserData` gives `UserData`) ([0033](0033-c-header-raw-layer-generation.md)).
- Functions are PascalCase and imported through `EntryPoint`: a free function from its own words
  (`SDL_CreateWindow` gives `CreateWindow`, `ma_engine_init` gives `EngineInit`), a method of the
  WebGPU model from its owner's words and its own (`wgpuDeviceCreateBuffer` gives
  `DeviceCreateBuffer`). Parameters are camelCase, the first word lowered whole when it is an
  acronym. Constants are PascalCase (`WGPU_WHOLE_SIZE` gives `WholeSize`,
  `SDL_HINT_VIDEO_DRIVER` gives `HintVideoDriver`). A collision inside `NativeMethods` fails the
  generator until `names` resolves it (`ma_version_string` is `GetVersionString`, next to the
  `VersionString` constant).
- Every raw declaration and member keeps a summary that names its C declaration
  (`Maps <c>ma_engine_config.pResourceManager</c>.`), so the C API can still be searched from the
  generated code.
- Kept from [0028](0028-internal-raw-interop-layer.md): raw functions and the raw forms of
  structures with pointers are internal; handles expose their native pointer as a public `nint`;
  `InternalsVisibleTo` goes to the interop assembly's test project only.

## Consequences

- The idiomatic layer, its hand-written partials and the tests read as .NET code: `using
  Jade.Wgpu.Raw;` and `NativeMethods.DeviceCreateBuffer(device, &descriptor)`.
- Searching for a C name now goes through the summaries or `EntryPoint`, not the identifiers.
- The configuration gains name exceptions where the rules collide, which is where a reviewer sees
  them.
- The layout tests of roadmap task 9 map C names to the C# names through the projection, as
  [0032](0032-webgpu-raw-layer-generation.md) planned.
- `Jade.Wgpu`'s generated code and tests change; its public API does not.

## Alternatives considered

- **Keep the C names** ([0028](0028-internal-raw-interop-layer.md)): no naming exception and a 1:1
  match with the C documentation, but C-style identifiers in all the internal code.
- **.NET names with a suffix, in the library's namespace** (`BufferDescriptorNative`): one
  namespace, but a suffix on hundreds of types.
- **C names for the functions only**: exact exported symbols as identifiers, but inconsistent with
  the types; `EntryPoint` keeps the symbol next to the .NET name.
