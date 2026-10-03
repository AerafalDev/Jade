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

- [x] Deterministic output; the solution builds with 0 warnings.
- [x] The export cross-check passes, with exceptions listed.
- [x] A test creates an instance, requests an adapter and a device, creates a buffer, writes it
      through the span overload, and releases everything. It is skipped with a clear reason when no
      adapter is available (headless CI).

## Verification

Generator run, build, export cross-check output, test run. Include a code excerpt showing a handle
method, a chained struct and a callback.

## Pitfalls

- ADR-0016: implement the `nint` rule for function pointers in import signatures in the generator,
  for every library, and regenerate SDL3 (its public API must not change). WebGPU is the first
  callback-heavy library, so the rule belongs here.
- From 203: `[LibraryImport]` needs `[MarshalAs(UnmanagedType.U1)]` on `bool` (ADR-0012 notes). That
  is irrelevant for `WGPUBool`, a 32-bit integer.
- Async operations (adapter and device requests, buffer mapping) complete through
  `wgpuInstanceProcessEvents` or `WaitAny`, depending on callback mode. The test must drive them.
- `WGPUBool` is 32-bit.

## Outcome

- Summary: the generator reads the staged `metadata/dawn.json` into the 201 `LibraryModel`
  (`DawnJsonReader`) and emits it through the shared `CSharpEmitter`, so there is no emitter fork.
  `DawnHeaderCheck` parses the staged `webgpu/webgpu.h` for the 12 ADR-0007 triples, compares it with
  the model and measures every struct's layout, which feeds `VarianceCheck` and the generated layout
  tests. `src/Jade.Interop/Generated/WebGpu/` holds 333 files:
  - 276 functions in `Wgpu`, 12 constants;
  - 65 enums and 6 bitmasks (`[Flags]`);
  - 195 structs (180 structures, 14 callback infos and `WGPUChainedStruct`);
  - 28 handles with instance methods, `AddRef` and `Release` included.

  195 layout tests are generated under `tests/Jade.Interop.Tests/Generated/WebGpu/`. The emitter
  gained ADR-0016 imports for every library, string-view and byte-span overloads, platform
  attributes on types and enum members, and constructors that preset fields. SDL3 was regenerated and
  its public API is unchanged. Hand-written: `src/Jade.Interop/WebGpu/StringView.cs` (`AsSpan()`,
  `Null`) and `tests/Jade.Interop.Tests/WebGpuTests.cs`.
- Verification (commands and results), on linux-x64 (CachyOS, .NET SDK 10.0.401), with the
  `jade_native` of `native.yml` run 37092056428 (`dotnet scripts/fetch-native.cs`; no change under
  `native/` since its commit) and Dawn `v20260930.214659`:
  - `dotnet scripts/generate-bindings.cs`: 3.0 to 3.1 s for both libraries (2.6 to 2.7 s for SDL3
    alone in 203). After deleting the four `Generated/{Sdl3,WebGpu}` folders, the first run writes
    600 files and the second changes none; the SHA-256 over all `*.g.cs` is the same after both
    (`549e6af96fe2a725...`). No CR and no BOM in the output.
  - Header cross-check, as printed:

    ```text
    WebGpu: header cross-check: webgpu/webgpu.h declares the same 276 functions, 195 structs, 65 enums, 6 bitmasks, 28 handles and 12 constants as dawn.json, with the same types and values, except:
      function emscripten_webgpu_get_device: declared by Dawn's api.h template outside dawn.json, and only defined in Emscripten builds
      struct WGPUINTERNAL_HAVE_EMDAWNWEBGPU_HEADER: a marker that C code tests with #ifdef to tell emdawnwebgpu's header apart; it holds no data
    ```

    The comparison covers return, parameter and field types and names, enum and bitmask values,
    enum sizes, handle typedefs, callback typedefs and the constant macros evaluated per target.
  - Layout report: 175 structs differ between targets, all on browser-wasm only and only through
    pointer and `size_t` fields, which the generated definitions follow. No unhandled variance.
  - Export cross-check against `artifacts/native/linux-x64/lib/libjade_native.so`:

    ```text
    WebGpu: export cross-check: 276 exported wgpu*, 276 bound, 0 excluded:
    WebGpu: every binding is exported.
    ```

    Exceptions: none on either side. `emscripten_webgpu_get_device` is not exported and has no
    `wgpu` prefix; the header check lists it. A binding without an export would only pass if its
    `SupportedPlatforms` left out the checked platform (web-only APIs). There are none: dawn.json has
    no Emscripten-only function.
  - ADR-0016 on SDL3: 43 functions now forward to a private `nint` import, for example
    `LoadFunction` to `LoadFunctionImport`. A reflection dump of the public `Jade.Interop.Sdl3` API
    before and after has the same 5,122 lines of types and member signatures; only the import
    attributes moved to the private methods. WebGPU has one such function, `GetProcAddress`.
  - `MSBuildTreatWarningsAsErrors=true dotnet build -c Release --no-incremental`: `0 Avertissement(s)`,
    `0 Erreur(s)`.
  - `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'`: exit 0. Converted
    `generate-bindings` project, `dotnet format --verify-no-changes`: exit 0.
    `MSBuildTreatWarningsAsErrors=true dotnet build scripts/generate-bindings.cs --no-incremental`:
    exit 0.
  - `dotnet test -c Release`: total 300, succeeded 299, skipped 1. The new tests:
    - 195 WebGPU layout tests; the skipped one is
      `EmscriptenSurfaceSourceCanvasHTMLSelector`, see Decisions;
    - `A_device_writes_a_buffer_through_the_span_overload` for the default and the Null backend. It
      creates an instance with `TimedWaitAny`, requests an adapter and a device through
      `WaitAnyOnly` callbacks and `WaitAny`, and reads `AdapterInfo` (then `FreeMembers`). It
      writes `[0xDEADBEEF, 1, 2, 3]` with `queue.WriteBuffer(buffer, 0, values)` inside a
      validation error scope, maps the buffer, reads the same values back, and releases everything;
    - `A_chained_WGSL_module_is_valid_and_SPIR_V_ones_are_rejected`: a WGSL module chained through
      `&source.Chain` validates. `ShaderSourceSPIRV` and `DawnShaderSourceSPIRV` modules fail
      with `SPIR-V is disallowed`, while `HasInstanceFeature(ShaderSourceSPIRV)` is non-zero;
    - `StringView_AsSpan_reads_sized_terminated_null_and_empty_views`.
  - Adapters, checked by temporarily turning the adapter info into a skip message (reverted):
    - with the GPU, the default request gets `Vulkan, DiscreteGPU: radv: Mesa 26.2.4-arch3.1`;
    - with `VK_DRIVER_FILES` and `VK_ICD_FILENAMES` pointing at nothing, it gets `Null, Unknown`;
    - a `D3D12` request on Linux skips with `No D3D12 adapter is available here (no GPU, driver or
      backend for it): Unavailable, No supported adapters`.

    With no Vulkan driver, all 299 still pass.
  - Negative checks, each reverted (the config restored, then 0 files changed):
    - dropping the `emscripten_webgpu_get_device` exclusion fails with `emscripten_webgpu_get_device
      is a function of the header but not of the model`;
    - mapping `WGPUBool` to `Byte` fails with 51 mismatches such as `wgpuAdapterHasFeature: return
      type is UInt32 in the header, Byte in the model`;
    - a misspelled `Notes` key fails as a stale config entry.
  - Not verified: generator runs on Windows and macOS hosts. Nothing was called on another OS, and
    nothing on browser-wasm (the cross-check parses Dawn's header for wasm32, see Decisions). CI has
    not run: nothing is pushed.
  - Excerpts:

    ```csharp
    // Handle method (Device.g.cs) and its raw form, plus a byte-span overload (Queue.g.cs).
    public GpuBuffer CreateBuffer(BufferDescriptor* descriptor) => Wgpu.DeviceCreateBuffer(this, descriptor);
    public GpuBuffer CreateBuffer(in BufferDescriptor descriptor) => Wgpu.DeviceCreateBuffer(this, in descriptor);
    public void WriteBuffer<T>(GpuBuffer buffer, ulong bufferOffset, ReadOnlySpan<T> data) where T : unmanaged => Wgpu.QueueWriteBuffer(this, buffer, bufferOffset, data);

    // Wgpu.Queue.g.cs: the static friendly form pins the span and passes its size in bytes.
    public static void QueueWriteBuffer<T>(Queue queue, GpuBuffer buffer, ulong bufferOffset, ReadOnlySpan<T> data) where T : unmanaged
    {
        fixed (T* dataPtr = data)
        {
            QueueWriteBuffer(queue, buffer, bufferOffset, dataPtr, (nuint)data.Length * (nuint)sizeof(T));
        }
    }

    // Chained struct (ShaderSourceWGSL.g.cs).
    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct ShaderSourceWGSL
    {
        public ChainedStruct Chain;
        public StringView Code;

        public ShaderSourceWGSL()
        {
            Chain.SType = SType.ShaderSourceWGSL;
        }
    }

    // Building a chain: no allocation, stack values only.
    var wgsl = new ShaderSourceWGSL { Code = new StringView { Data = code, Length = (nuint)length } };
    var descriptor = new ShaderModuleDescriptor { NextInChain = &wgsl.Chain };
    var module = device.CreateShaderModule(in descriptor);

    // Callback info (RequestAdapterCallbackInfo.g.cs) and a callback (WebGpuTests.cs).
    public delegate* unmanaged[Cdecl]<RequestAdapterStatus, Adapter, StringView, void*, void*, void> Callback;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnAdapter(RequestAdapterStatus status, Adapter adapter, StringView message, void* userdata1, void* userdata2) =>
        Completion.From(userdata1).Complete((int)status, adapter.Handle, message);

    var callback = new RequestAdapterCallbackInfo { Mode = CallbackMode.WaitAnyOnly, Callback = &OnAdapter, Userdata1 = state };
    var wait = new FutureWaitInfo { Future = instance.RequestAdapter(in options, callback) };
    instance.WaitAny(new Span<FutureWaitInfo>(ref wait), timeoutNS);

    // ADR-0016 (Wgpu.Global.g.cs).
    public static delegate* unmanaged[Cdecl]<void> GetProcAddress(StringView procName) => (delegate* unmanaged[Cdecl]<void>)GetProcAddressImport(procName);

    [LibraryImport("jade_native", EntryPoint = "wgpuGetProcAddress")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial nint GetProcAddressImport(StringView procName);
    ```

- Decisions taken (and ADRs added): no ADR added.
  - Schema: read from the pinned Dawn sources, not assumed: `generator/dawn_json_generator.py`
    (`parse_json`, `item_is_enabled`, `Name`, `c_methods`, `has_free_members_function`, enum value
    ranges) and `generator/templates/api.h` (C names, `nextInChain`, `chain`, the two `userdata`
    pointers, `FreeMembers`, `WGPUBool`). `docs/dawn/codegen.md` misses callback infos, callback
    functions and string views. The tarball is the xmake cache's, SHA-256 equal to the recipe. The
    reader binds what Dawn's header declares (tags `dawn`, `emscripten`, `native`, `deprecated`),
    with the same C names and order. Unknown tags or categories fail generation. One quirk is kept:
    `item_is_enabled` tests `tag not in ('art_experimental')`, a substring test that also drops
    `art`; neither is ever enabled here, so results are unchanged.
  - Header cross-check: `DawnHeaderCheck` maps the header's clang types to `TypeRef`s and compares
    them with the model. Dawn's header has `#error` under `__EMSCRIPTEN__`, so the wasm32 parse
    undefines it. Layouts only depend on the ABI, and the reader rejects any struct member tagged
    for one implementation only (none today), so emdawnwebgpu's structs have the same layout. A
    `math.h` stub (`NAN`, `INFINITY`) joins `sysroot/`.
  - Platform availability comes from tags: available in emdawnwebgpu when enabled for
    `["emscripten"]` (how `MultiGeneratorFromDawnJSON` renders emdawnwebgpu's `webgpu.h`), in Dawn
    when enabled for `["dawn", "native", "deprecated"]`. A method needs both its object and itself
    enabled, an enum member both its enum and itself. Dawn-only items get
    `[UnsupportedOSPlatform("browser")]`:
    - 69 functions, with their instance and friendly forms;
    - 5 handles, 105 structs, 11 enums, 216 enum members and 1 constant.

    Emscripten-only items get `[SupportedOSPlatform("browser")]`: the struct
    `EmscriptenSurfaceSourceCanvasHTMLSelector` and its `SType` member. Two functions that
    emdawnwebgpu declares but aborts in (`library_webgpu.js` at the pinned tag) are also marked
    through `UnsupportedPlatforms`, with a doc note: `wgpuGetProcAddress` and `wgpuSurfacePresent`.
    A check fails generation if a declaration uses a type that does not exist everywhere the
    declaration does.
  - `WGPUBool` is `uint` (`TypedefMappings`): it is a `uint32_t` that C compares with 0, `uint`
    passes like it on every ABI, and ADR-0012 forbids normalizing it to `bool`. A 4-byte wrapper
    struct would read better, but WGPUBool is passed by value in signatures (`HasFeature`, ...), so
    it would depend on how each ABI passes a one-field struct, which 104 and 105 have yet to check for
    handles. An enum adds a type and still needs `!= 0`.
  - Names come from Dawn's casing of each canonical name, so C# names equal the C names without
    `WGPU`/`wgpu` (`TextureFormat.RGBA8Unorm`, `ShaderSourceWGSL`, `SType`). Static class `Wgpu`;
    raw functions keep the C name (`Wgpu.DeviceCreateBuffer`); instance methods drop the object
    (`device.CreateBuffer`). `QuerySet.QuerySetGetType` keeps its full name: `GetType` would hide
    `object.GetType` (203's rule). Renames:
    - `WGPUBuffer` is `GpuBuffer`, as in ADR-0006's example: `Buffer` is ambiguous with
      `System.Buffer` under the SDK's implicit usings;
    - `1D`, `2D`, `2DArray` and `3D` texture (view) dimensions are `Dimension1D`, ...;
    - `BT_1886`, `SMPTE_170M` and `Unorm10_10_10_2`/`Snorm10_10_10_2` lose their underscores
      (CA1707);
    - the field `TextureBindingViewDimension.textureBindingViewDimension` is `ViewDimension`
      (CS0542).
  - Friendly overloads come from dawn.json's metadata only (no inference, and `Parameters` only
    accepts `Raw`):
    - a `length` naming another argument makes a span: `ReadOnlySpan<CommandBuffer>`,
      `ReadOnlySpan<uint>`, `Span<FutureWaitInfo>`. A `void` pointer with a byte count becomes a
      generic `ReadOnlySpan<T>`/`Span<T> where T : unmanaged`, whose length times `sizeof(T)` fills
      the count. ADR-0006's `queue.WriteBuffer(buffer, 0, vertices)` works as written;
    - `const*` to one struct is `in`;
    - `*` to one struct is `out`, or `ref` when the struct is extensible, so a caller's
      `NextInChain` (for example `AdapterPropertiesVk` under `AdapterInfo`) is not cleared;
    - a `WGPUStringView` argument gets `ReadOnlySpan<byte>`, wrapped as `{ptr, length}` with no
      terminator. An empty span is the empty string, `{NULL, 0}`.
  - Chained structs: `new` on a chained struct presets `Chain.SType` through a parameterless
    constructor (`StructModel.Initializers`), and `default` still zeroes everything. A chain is
    built from stack values with `&value.Chain`, since `Chain` is at offset 0. No allocation, no
    helper type. Docs list where each struct chains and what each `NextInChain` accepts, and which
    `FreeMembers` releases an output struct. The `WGPU_*_INIT` defaults are not generated (see
    Follow-ups).
  - Callbacks: callback-info structs keep typed `delegate* unmanaged[Cdecl]` fields and both
    `userdata` pointers. Each callback typedef's parameter names and C types are listed in the
    field's remarks, since C# function pointer types carry no names.
  - String views out: `StringView.AsSpan()` (hand-written) reads sized, NUL-terminated
    (`WGPU_STRLEN`) and null views without copying. `StringView.Null` is
    `{NULL, WGPU_STRLEN}`, because `default` is the empty string, not null. `WGPU_STRLEN` and
    `WGPU_WHOLE_MAP_SIZE` are `SIZE_MAX`, emitted as `static nuint ... => nuint.MaxValue`.
  - ADR-0016 for every library: a function with a function-pointer parameter or return gets a
    private `<Name>Import` with `nint`, and the public 1:1 method casts. Struct fields stay typed.
  - Export check: a binding without an export passes only when its `SupportedPlatforms` leave out
    the checked library's platform, and the report lists it.
  - Layout test of `EmscriptenSurfaceSourceCanvasHTMLSelector`: CA1416 rejects touching a
    `[SupportedOSPlatform("browser")]` type without a guard, so the generated test only runs behind
    `OperatingSystem.IsOSPlatform("browser")` and skips elsewhere. The generator still compares its
    layout with clang's on all 12 targets.
  - SPIR-V: bound, with a doc note on `ShaderSourceSPIRV`, `DawnShaderSourceSPIRV` and
    `InstanceFeatureName.ShaderSourceSPIRV`, all checked by the test above.
  - FXC on Windows: CI's first run failed the device test on win-x64 with `DynamicLib.Open:
    d3dcompiler_47.dll Windows Error: 87` from `EnsureFXC`. Dawn tries the library's and the
    executable's directories, then the bare name with `LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR`, which
    requires a full path (`ERROR_INVALID_PARAMETER`), so the system copy that 103 and 106 relied on
    is never searched. The Dawn recipe now sets `DAWN_FORCE_SYSTEM_COMPONENT_LOAD=ON` on Windows,
    which loads `d3dcompiler_47.dll` from System32 only. Dawn's comment on that option warns that
    older Windows versions ship a compiler with bugs.
  - Tests use `CallbackMode.WaitAnyOnly` and `WaitAny` with a 30 s timeout. That needs the instance
    feature `TimedWaitAny`, and keeps callbacks on the test thread. Without a GPU, Dawn answers a
    default request with its Null adapter (`InstanceBase::EnumeratePhysicalDevices` tries every
    backend when `backendType` is `Undefined`). The buffer is written by the queue and mapped back
    without a copy: the Null backend's `Queue::SubmitImpl` runs no command buffer, while
    `WriteBufferImpl` and mapping use the buffer's memory (`src/dawn/native/null/DeviceNull.cpp`).
- Deviations from the brief:
  - On headless machines the device test runs on the Null backend instead of skipping. The skip
    only happens when jade_native has no adapter at all for the request (checked with D3D12 on
    Linux).
  - Beyond "absent from emdawnwebgpu": Emscripten-only items are marked
    `[SupportedOSPlatform("browser")]`. Two functions emdawnwebgpu declares but aborts in are marked
    unsupported. The layout test of the one web-only struct skips off-browser (above).
  - Extra tests: the chained WGSL and SPIR-V modules, and `StringView.AsSpan`.
  - The SDL3 regeneration also adds a line to its export report (`every binding is exported`).
- Follow-ups:
  - 105:
    - stage emdawnwebgpu's header, and parse it for browser-wasm instead of Dawn's with
      `__EMSCRIPTEN__` undefined;
    - emdawnwebgpu's blocking `WaitAny` aborts unless built with ASYNCIFY (`emwgpuWaitAny` under
      `#if ASYNCIFY` in `library_webgpu.js`), so on the web the engine needs `AllowSpontaneous` or
      `AllowProcessEvents` callbacks, or an ASYNCIFY build;
    - check a callback round-trip through a callback-info struct and its typed field;
    - check whether a pointer to a function pointer in a signature (SDL's `SDL_GetMemoryFunctions`)
      also fails; ADR-0016 only covers function pointer types themselves.
  - 104, 105: check on Apple arm64 and wasm32 that structs passed by value (`StringView`, callback
    infos, handles) pass as C expects.
  - 205: NativeAOT publish of the WebGPU bindings, generic span overloads included.
  - Engine or a later generator task: dawn.json's `default` values (the `WGPU_*_INIT` macros: null
    string views, `undefined` limits and enums) are not generated. `new T()` zeroes everything but
    `SType`, which differs from C's `_INIT` for string views and limits.
  - Docs: dawn.json carries no documentation. webgpu-headers' `webgpu.yml` documents the standard
    part and could feed XML docs; today every member says `Binds ...`.
  - Enum values are emitted in decimal; Dawn's ranges (`0x0005_0000` and up) would read better in
    hexadecimal.
  - Orchestrator:
    - `design/architecture.md` does not describe the dawn.json reader, the header cross-check or
      the platform marking;
    - ADR-0006 could record the WebGPU names (`Wgpu`, `GpuBuffer`, `Dimension2D`);
    - ADR-0012's notes could record `WGPUBool` as `uint`;
    - CLAUDE.md's interop section could mention ADR-0016's `nint` imports.
