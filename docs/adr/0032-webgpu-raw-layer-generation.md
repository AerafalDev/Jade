# 0032. WebGPU raw layer generation

- Status: Accepted
- Date: 2026-10-05

## Context

Roadmap task 7 builds the intermediate representation, the projection and the raw emitter of
[0026](0026-binding-generator-pipeline.md) for `Jade.Wgpu`, following the mapping rules of
[0009](0009-interop-mapping-conventions.md) and [0027](0027-interop-mapping-rules.md), the internal
raw layer of [0028](0028-internal-raw-interop-layer.md) and the structure classification of
[0029](0029-descriptors-and-chained-structs.md). Several points those records leave open have to be
settled to emit code: what the model holds beyond `dawn.json`, the defaults of structures, how
native libraries are found, how the public API files follow the generated code, and how the output
is verified.

Verified on 2026-10-05 at Dawn `b1236a9` (the commit of `build/versions.json`) with SDK
`11.0.100-rc.1.26425.128`:

- `generator/dawn_json_generator.py` renders Dawn's `include/dawn/webgpu.h` with the tags `dawn`,
  `emscripten`, `native` and `deprecated`, and the wire client header, the proc table and the
  native implementation with `dawn`, `native` and `deprecated` only. The two sets differ by
  `WGPUEmscriptenSurfaceSourceCanvasHTMLSelector`, its `WGPUSType` value and
  `WGPUINTERNAL_HAVE_EMDAWNWEBGPU_HEADER`, and by no function: the native availability of
  [0026](0026-binding-generator-pipeline.md) is what the library implements. `libwebgpu_dawn.so`
  built by `scripts/build-native.cs` exports exactly the 276 functions of that header (`nm -D`).
- `generator/templates/api.h` adds what `dawn.json` does not list: `WGPUBool` (`uint32_t`),
  `WGPUFlags` (`uint64_t`), `WGPUChainedStruct { next; sType; }`, a `nextInChain` pointer first in
  every extensible structure and callback info, a `chain` header first in every chained structure,
  `userdata1` and `userdata2` last in every callback info and callback function, `AddRef` and
  `Release` for every object, and `FreeMembers`, which takes the structure by value, for every
  output structure with a pointer or string member. Enum values get `0x0005_0000` for `dawn`,
  `0x0004_0000` for `emscripten` alone and `0x0001_0000` for `native`, and each enum ends with a
  `Force32` value. Each structure has a `WGPU_*_INIT` macro, whose defaults differ from the
  `default` of `dawn.json` in places: an enum with an `undefined` value defaults to it.
- `WGPUINTERNAL_HAVE_EMDAWNWEBGPU_HEADER` is an Emdawnwebgpu marker with one placeholder member,
  not an API. The 19 `_comment` fields of `dawn.json` are maintainer notes, mostly TODOs and bug
  links, not API documentation.
- Roslyn does not report never-assigned or unread fields (CS0649, CS0169) in a type that has
  `[StructLayout]` (`SourceAssemblySymbol.GetUnusedFieldWarnings`, `dotnet/roslyn` main).
- CA5392 asks for `[DefaultDllImportSearchPaths]` on every P/Invoke, and CA5393 counts
  `AssemblyDirectory` as unsafe. The host's `NATIVE_DLL_SEARCH_DIRECTORIES` only lists the
  directories of the native assets of `deps.json`; on Linux, a NativeAOT executable with
  `libwebgpu_dawn.so` beside it fails to load it with `SafeDirectories` (`DllNotFoundException`)
  and loads it with `AssemblyDirectory` (throwaway app).
- `dotnet format analyzers --diagnostics RS0016 --include-generated` applies the
  PublicApiAnalyzers fix to declarations in generated files, which `dotnet format` skips otherwise.
  RS0017 (a declared API that no longer exists) has no fix it applies.
- Microsoft.Testing.Platform exits with code 8 when every test is skipped, and with 0 when some
  pass and the others are skipped.
- The generated layer was compared once with Dawn's `include/dawn/webgpu.h`, rendered by Dawn's
  generator at the pinned commit and compiled by GCC on `linux-x64`: the sizes and offsets of the
  195 structures and 771 members, the 675 enum and flag values, the parameter counts of the 276
  functions, and the bytes of 194 of the 195 `*_INIT` defaults are identical. The last one is
  `depthClearValue`: GCC's `NAN` is `0x7FC00000` and `float.NaN` is `0xFFC00000`; Dawn only
  tests it with `std::isnan`.

## Decision

### Intermediate representation and Dawn front-end

- The model of [0026](0026-binding-generator-pipeline.md) lives in
  `scripts/binding-generator/Model/`. Declarations carry their C name, the words of their name and
  their availability; structure members their role (value, `nextInChain`, chain header, userdata),
  default, array length and optionality; functions their owner and kind (free, method, `AddRef`,
  `Release`, `FreeMembers`), which the idiomatic layer needs to place them.
- The Dawn front-end (`Dawn/DawnModelBuilder.cs`) reproduces `api.h` at the pinned commit: the
  members and functions it adds, the tag offsets of enum values, and the defaults of the
  `WGPU_*_INIT` macros. `Force32` values are not part of the model. A type used where it is not
  available, a reference to an excluded entry, a typedef entry or a wire-only native fails the
  generator.

### Configuration

`interop/Jade.Wgpu/bindings.json` gains, strict like the rest of the file:

- `library`: the name imported from, `webgpu_dawn`;
- `exclude`: C names with the reason, reported by every run (`WGPUINTERNAL_HAVE_EMDAWNWEBGPU_HEADER`);
- `names`: .NET names by C name, or by `Structure.member`, that replace the rule's (`WGPUBool` is
  `Bool32`);
- `words`: the casing of words the rule gets wrong (`sRGB` is `Srgb`).

An entry that matches nothing fails the generator.

### Projection

- Enums (`uint`), flags (`[Flags]` over `ulong`), handles, booleans and value structures are
  public with .NET names; the other structures are internal with their C names and C member names;
  constants and functions are internal, with their C names, in the class `NativeMethods`
  ([0028](0028-internal-raw-interop-layer.md)).
- A value structure holds no pointer, string, callback or userdata, except its chain members,
  which are internal fields ([0029](0029-descriptors-and-chained-structs.md)). Handles count as
  values: they are public types of their own.
- Every generated structure whose `*_INIT` macro sets a non-zero value has a parameterless
  constructor that applies it, so `new T()` is the C initializer and `default(T)` the zeroed
  structure. Chained structures set their `sType` there. No member is `required` yet.
- Constants stay internal until roadmap task 12 decides the public surface; the value structures'
  constructors use them.
- A function pointer type is spelled inline as `delegate* unmanaged[Cdecl]<…>`
  ([0027](0027-interop-mapping-rules.md)).
- .NET names follow [0027](0027-interop-mapping-rules.md): inside a word, upper-case runs of more
  than two letters are Pascal-cased (`RGBA8` gives `Rgba8`, `WebGPU` gives `WebGpu`), two-letter
  ones are kept (`IO`), digits and other letters keep their casing (`float32x2` gives
  `Float32x2`). A name that collides with another, or with its enclosing type, fails the generator
  until `names` gives one.

### Emission

- One file per type, `Generated/<C# name>.g.cs`, and `Generated/NativeMethods.g.cs` for the
  constants and functions. Each file starts with `// <auto-generated/>` and a line naming the
  generator, enables nullable annotations, writes framework types with `global::` and carries
  `[GeneratedCode("Jade.BindingGenerator", "1.0.0")]` on its type.
- Every structure carries `[StructLayout(LayoutKind.Sequential)]`: it states the C layout, and the
  compiler then treats it as an interop type whose fields native code writes.
- Functions use `[LibraryImport]` and `[UnmanagedCallConv]` with `CallConvCdecl`, and platform
  attributes from their availability; an enum value has its own only when it differs from its
  enum's.
- Summaries name the C declaration (`Maps <c>WGPUExtent3D.width</c>.`); members of internal
  structures have none, since their names are the C names. The `_comment` fields of `dawn.json`
  are not used.
- Output is UTF-8 without a byte order mark and with LF line endings. A run writes only the files
  that change and deletes the `*.g.cs` files it no longer produces; two names that differ only in
  case fail it.

### Native library loading

- `Jade.Wgpu` declares `[assembly: DisableRuntimeMarshalling]` and
  `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]`
  in `Properties/AssemblyInfo.cs`: the directory of the assembly, where the natives are deployed,
  and the safe Windows directories, without the current directory and `PATH`. CA5393 is suppressed
  for `NativeMethods` with that justification (the maintainer's choice among the options of the
  context).
- `InternalsVisibleTo` is granted to `Jade.Wgpu.Tests` only ([0028](0028-internal-raw-interop-layer.md)).

### Public API files

The generated public declarations go into `PublicAPI.Unshipped.txt` through the analyzer's own fix:
`dotnet format analyzers interop/Jade.Wgpu/Jade.Wgpu.csproj --diagnostics RS0016 --severity info --include-generated`.
Declarations that disappear are removed by hand from the lines RS0017 names.

### Verification

- The CI `build` jobs build `scripts/binding-generator.cs` with warnings as errors, run it, and fail
  when `git diff` on `interop/` is not empty (new files included); running it on Linux, Windows
  and macOS checks that the output does not depend on the host.
- `tests/Jade.Wgpu.Tests` is the interop assembly's test project. It checks the managed side of
  the raw layer (defaults, booleans, handles), and smoke tests call the library: they create an
  instance, list the instance features and request an adapter through a callback. Its project
  copies `artifacts/native/bin/<host RID>/` next to the tests; without the library the smoke tests
  are reported as skipped (`Assert.Inconclusive`), as in CI until the native builds of roadmap
  tasks 10 and 11 reach it.

## Consequences

- The raw layer is a checked image of `webgpu.h`: the layouts, values, signatures and defaults
  that were compared match the C header on `linux-x64`. Generated layout tests on every target
  remain roadmap tasks 9 and 18.
- `PublicAPI.Unshipped.txt` lists 1,474 generated declarations; a Dawn update that changes the
  public types changes that file and fails the build until the fix is applied.
- The idiomatic layer (roadmap task 12) builds on the owners, kinds, lengths, optionality and
  chain data of the model, and has to decide: `required` members of value structures, public
  constants, and the name `Buffer`, which is ambiguous with `System.Buffer` under
  `using System;`.
- A NaN default is `float.NaN`, whose sign bit differs from GCC's `NAN`; WebGPU only tests NaN.
- Not verified: structures passed and returned by value through P/Invoke on WebAssembly
  (roadmap task 15), the import name on iOS (task 17), and the generator's output on Windows and
  macOS until the CI runs on a pull request.
- Each CI build job also restores the generator's packages and fetches its pinned inputs; the
  added time is not measured yet.

## Alternatives considered

- **Defaults from the `default` of `dawn.json`**: closer to the JavaScript API, but different
  from the C initializers the library is written against (an enum with `undefined` defaults to
  it in C).
- **`required` members on value structures now**: the C API has none, output structures could
  not be created without them, and the choice belongs with the descriptor mirrors of task 12.
- **`#pragma warning disable CS0649` in generated files**: hides the warning without stating
  anything; `[StructLayout]` states the layout contract the warning exception was made for.
- **`SafeDirectories` alone, or the runtime default**: the first breaks NativeAOT and natives
  copied next to the application on Linux and macOS; the second keeps the current directory and
  `PATH` in the Windows search.
- **The generator writing `PublicAPI.Unshipped.txt` itself**: duplicates the analyzer's line
  format, which the analyzer's own fix already produces.
- **A separate regeneration job**: it would need a ruleset change to be required, while the
  required `build` checks already run on every host.
