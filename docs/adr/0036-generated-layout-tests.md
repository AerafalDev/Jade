# 0036. Generated layout tests and layout libraries

- Status: Accepted
- Date: 2026-10-05

## Context

[0009](0009-interop-mapping-conventions.md) requires generated tests that compare the C
`sizeof`/`offsetof` of every structure with its C# layout, on every target, and foresees "a small
C program or library per target, built by the native toolchain". [0026](0026-binding-generator-pipeline.md)
never emits the sizes and offsets libclang computes: the C# layout follows from the member types,
and these tests check it against the real compilers. Until now the generator only checked that each
C structure has a sequential layout according to clang ([0033](0033-c-header-raw-layer-generation.md)),
and the WebGPU layouts were compared once by hand ([0032](0032-webgpu-raw-layer-generation.md)).
Roadmap task 9 adds the tests on the host; running them on the other targets and in CI is tasks 10
and 18.

The raw layers hold 525 structures: 195 in `Jade.Wgpu` (105 of them native only, one browser only), 125
in `Jade.Sdl` (5 anonymous records, 4 unions, 15 array members) and 205 in `Jade.MiniAudio` (61
anonymous records, 21 unions, 21 array members); the configuration of `Jade.MiniAudio` maps one
more, `ma_vec3f`, to `System.Numerics.Vector3`.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`, xmake 3.1.1, GCC 16.2.1 and clang 23.1.1:

- C17 defines `offsetof(type, member-designator)` for any designator such that `&(t.member-designator)`
  is an address constant (7.19), so `offsetof(struct S, in.u.d)` and
  `offsetof(struct S, arr[0].y)` are valid; GCC and clang accept them, and their differences, in a
  static initializer with `-std=c17 -Wpedantic -Werror`. `_Alignof` takes a type name only, and C17
  cannot name the type of an anonymous record (`typeof` arrives with C23).
- libclang names a record without tag after its typedef: `ma_vec3f` is
  `typedef struct { float x; float y; float z; } ma_vec3f;`, where `struct ma_vec3f` would name
  another, undeclared type. Most SDL3 and miniaudio records have a tag, which their typedef may
  not repeat.
- Dawn's build generates `dawn/webgpu.h` from `api.h` and installs it with `webgpu/webgpu.h`, which
  includes it (`dawn_headers` in `src/dawn/CMakeLists.txt`, `dawn_install_headers` in
  `src/cmake/DawnLibrary.cmake`); every structure is `typedef struct WGPUName { … } WGPUName`.
- xmake builds and installs the default targets when no target is named; `--group` selects the
  targets of a group whether they are default or not (`get_targets` in
  `modules/private/action/utils.lua`).
- `Marshal.OffsetOf` gives the offset in the unmanaged layout that marshalling produces, "which
  does not necessarily correspond to the offset of the managed structure layout" (its
  documentation). The raw layer is passed by pointer, so the managed layout is the one to check.
- In a C# local of a structure, the difference between the address of a member and the address of
  the local is its offset, and the difference between `&member + 1` and `&member` its size,
  whatever its type: pointers, function pointers, inline arrays, and the element of an inline array
  (`&value.Name[0]`). A sequential `struct { byte b; T value; }` places an unmanaged `T` at its
  alignment: 1 for `byte`, 8 for `long` and `nint`, 4 for `Vector3` (throwaway app, CoreCLR on
  `linux-x64`).
- A change to `build/xmake.lua` changes the build key of Dawn and SDL3
  ([0031](0031-native-build-definitions.md)), so including the layout definitions rebuilds both
  once; a change under `build/layout/` does not. In xmake, `add_packages(name, {links = {}})`
  replaces the links of a package (`_get_from_packages` in `core/project/target.lua`), while
  `-Wl,--as-needed` lands after the packages' `-l` flags and does not drop them.

## Decision

### What is compared

- Every generated structure, public or raw ([0034](0034-raw-layer-with-dotnet-names.md)), unions and
  anonymous records included, and every C structure that the configuration maps to a .NET type.
  The members of a mapped structure are expected under their .NET names (`ma_vec3f.x` and
  `Vector3.X`). Opaque types have no managed layout; enums, booleans and handles are checked
  through the members that hold them.
- For a record: its size and alignment. For each member: its size and its offset from the start of
  its record. An array member, the inline array of the C# side, also has its first element
  compared, `record.member[0]`. The alignment of an anonymous record is 0 on both sides: C cannot
  name its type, and the offsets and sizes of the records that hold it depend on it.
- A record is compared only on the platforms it is available on, with the same guard on both sides:
  a preprocessor condition on macros of `jade_layout.h` in C, `OperatingSystem` checks in C#.

### C side

- The binding generator writes `build/layout/<name>.g.c` for each interop library (`wgpu`, `sdl`,
  `miniaudio`: the project name without `Jade.`, in lower case). It defines the macros, includes the
  forced includes, the headers and the shims that the generator parsed, in the same order
  ([0026](0026-binding-generator-pipeline.md)), or `webgpu/webgpu.h` for `Jade.Wgpu`, then the
  hand-written `build/layout/jade_layout.h`. It declares a table of entries (name, size, alignment,
  offset) ended by an entry without name, and exports one function that returns it,
  `jade_<name>_layout`, named after the library so that several layout libraries can be linked
  into one module.
- An anonymous record is reached through the member designators that lead to it from the named
  record that holds it (`input.axis`). The intermediate representation records how C names each
  record (`struct SDL_Rect`, `ma_vec3f`) and where an anonymous record lies in its parent; a
  structure mapped to a .NET type keeps its members there, never emitted.
- `build/layout/xmake.lua` builds each source into a shared library `jade_<name>_layout`, in the
  xmake group `layout` and not a default target, in C17 with warnings as errors and hidden symbols.
  Its include directories are those of the pinned sources the generator parses (SDL3's `include/`,
  miniaudio's root) and, for WebGPU, Dawn's installed package, which holds the generated header; a
  layout library links nothing, Dawn's library included.
- `scripts/build-native.cs` builds and installs the group into `artifacts/native/test/<rid>/`,
  apart from the shipped libraries of `artifacts/native/bin/<rid>/`, and checks that each library
  exports its function. The layout libraries are never packaged.

### C# side

- The generator writes `tests/<project>.Tests/Generated/LayoutTests.g.cs` for each interop library:
  an internal test class with one test, `EveryStructureHasTheLayoutOfTheCompiler`. It loads the
  layout library from the test assembly's directory, where the test project copies
  `artifacts/native/test/<rid>/`, reads the table through the exported function, measures every
  record with a generated method per record (`sizeof`, the alignment probe, address differences in
  a local), and fails with the list of every difference, entries present on one side only
  included. Without the layout library the test is inconclusive, like the other native tests.
- The C# names come from the projection ([0034](0034-raw-layer-with-dotnet-names.md)); the test
  assembly reaches the raw types through `InternalsVisibleTo`.
- The CI `build` jobs check that a regeneration leaves `interop/`, `build/layout/` and `tests/`
  unchanged.

## Consequences

- Every generated layout is checked against the C compiler that builds the natives, on the host:
  a wrong type mapping, member order, union or inline array length fails the test, and so does a
  .NET type mapped to a C structure of another layout.
- Compiling the layout library also checks that every structure and member of the model exists in
  the C headers under its C name, which matters for `Jade.Wgpu`, whose model reproduces Dawn's
  `api.h` from `dawn.json` instead of parsing it.
- A layout library older than the generated tests reports entries on one side only; the message
  says to rebuild the natives. Regenerating does not rebuild Dawn or SDL3.
- Tasks 10 and 18 build the layout libraries for every RID: static archives for iOS and the
  browser, where the tests must reach the function without `NativeLibrary.TryLoad` from a file;
  MSVC needs C11 or later for `_Alignof`. Not verified: the alignment probe and the measurements
  under NativeAOT, Mono on iOS and Android, and Mono WebAssembly.
- Each test project gains a generated file of 2,400 to 3,500 lines, and `build/layout/` three
  generated sources of about 1,000 to 1,300 lines.

## Alternatives considered

- **A generated C program that prints the layouts**: simpler to debug, but no test can start a
  process on iOS, Android or in the browser, so task 18 would need another mechanism.
- **Expected values computed by libclang and written into the C# tests**: no C build, but libclang
  is not the compiler of the natives, the check would no longer be independent of the generator,
  and [0026](0026-binding-generator-pipeline.md) keeps parse-time layouts out of the output.
- **`_Static_assert` in C on values the generator computes for C#**: the generator would have to
  reproduce the runtime's layout algorithm, which is what the tests check.
- **`Marshal.OffsetOf` and `Marshal.SizeOf`**: they give the marshalled layout, not the managed one.
- **One test per structure** (`DynamicData`): 525 results instead of one failure that lists every
  difference.
- **The layout libraries in `artifacts/native/bin/<rid>/`**: that directory is what the packages
  ship and what the native build checks file by file.
- **The C sources in the test projects**: the native build would compile files from `tests/`.
