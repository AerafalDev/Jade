# 203: SDL3 bindings

- Depends on: 201, 003
- ADRs: 0005, 0006

## Goal

The complete public SDL3 API that jade_native exports, generated into
`src/Jade.Interop/Generated/Sdl3/`. Overloads and handles follow ADR-0006, and the config documents
every exclusion.

## Context

- 201 delivered the generator core and a vertical slice of SDL3. This task extends the config to
  every public header staged by 101.
- 101 disabled SDL's audio, GPU and render subsystems, but their functions are still exported (the
  export list is upstream's `SDL_dynapi.sym`) and fail at runtime with "not built with ... support".
  Exclude those headers (`SDL_audio.h`, `SDL_gpu.h`, `SDL_render.h` and whatever depends only on
  them) from the bindings. The export cross-check lists them as exported but intentionally unbound.
- ADR-0012: C `bool` maps to C# `bool`. 201 emitted `byte`. Change the mapping in the generator and
  regenerate. Then update CLAUDE.md's interop bullet if it still says otherwise.
- From 201, gaps to close in the generator or `Sdl3Config.cs`:
  - constants (`SDL_PROP_*` strings, `SDL_WINDOWPOS_*`, version macros);
  - `[InlineArray]` for arrays of structs, anonymous members and bit-fields;
  - callback typedef docs, which are dropped today;
  - `const char*` returns, still `byte*` (a span or UTF-8 helper?);
  - ID typedefs (`SDL_WindowID`, ...), which are plain `uint` (typed IDs?);
  - enum member casing that needs word splitting for `SDL_PixelFormat` (`Index1lsb`,
    `Rgba8888`, ...);
  - `SDL_Event`, bound so far with only `type`, `common`, `display`, `window`, `quit` and padding,
    which needs every member;
  - Emscripten-only functions such as `SDL_SetWindowFillDocument`, which are not marked.
- From 201: on macOS, SDL requires window calls on the main thread, and xunit runs tests on other
  threads, so window tests are skipped there. Keep that explicit.
- From 201: headers that include more system headers may need more stubs in
  `scripts/generate-bindings/sysroot/`. The parse fails with the missing header's name.

## Scope

- Config for all public SDL3 headers: handles (`SDL_Window*`, `SDL_Gamepad*`, ...), properties API,
  events union, callbacks, `SDL_IOStream`, error handling, and out parameters across the API.
- The events union: an exact-layout C# representation, checked by the layout-variance report on
  every triple.
- From 003: enum members whose C name, once the prefix is stripped, starts with a digit
  (`SDL_SCANCODE_1` → `_1`) fail CA1707. Give them a config-driven name (for example `Digit1`),
  applied the same way to every such enum, and list the mapping in the Outcome.
- Inline functions and macros that users need (for example `SDL_BUTTON_MASK`): hand-written helpers
  in `src/Jade.Interop/Sdl3/`, kept to a minimum and listed in the Outcome.
- Export cross-check like 202: every exported `SDL_*` is bound or excluded with a reason.

## Out of scope

- Platform main and app lifecycle glue (104 ADRs), engine input abstractions.

## Acceptance criteria

- [ ] Deterministic output; the build is clean.
- [ ] The export cross-check passes.
- [ ] The layout-variance report is clean on every triple 201 parses.
- [ ] Tests: version, init and quit without video, the properties round-trip, and `SDL_IOStream`
      from memory. Anything needing a display is skipped headless, with a reason.

## Verification

Generator run, build, cross-check output, test run.

## Pitfalls

- SDL3 uses C `bool` (1 byte). Keep it distinct from WebGPU's 32-bit `WGPUBool`.
- Variadic functions (`SDL_Log`, `SDL_SetError`, ...) cannot be called through P/Invoke portably.
  Exclude them or bind a non-variadic sibling, and document which.

## Outcome

- Summary: `Sdl3Config.cs` classifies every staged SDL3 header: 50 are bound, 16 are excluded with a reason, and a
  new or unlisted header fails generation. `src/Jade.Interop/Generated/Sdl3/` now holds 265 files: 982 functions in
  `Sdl`, 484 constants, 101 enums (16 ID enums, 18 macro enums), 89 structs (4 opaque) and 27 handles, plus 85
  generated layout tests. The generator gained:
  - a two-pass macro probe for constants;
  - macro enums, ID typedefs and anonymous field types;
  - `[InlineArray]` fields and callback typedef docs;
  - a platform merge for declarations only some targets have;
  - parameter rules inferred from SDL's `\param` text;
  - an ELF export cross-check, and a stale-config check across all targets.

  C `bool` is now C# `bool`. Hand-written helpers live in `src/Jade.Interop/Sdl3/Sdl.Macros.cs`. Tests cover version,
  init and quit, events, hints, properties and `SDL_IOStream`.
- Verification (commands and results), on linux-x64 (CachyOS, .NET SDK 10.0.401, SDL 3.4.16):
  - `dotnet scripts/build-native.cs` in this worktree: staged in 94 s (packages from the shared xmake cache).
  - `dotnet scripts/generate-bindings.cs`: 2.6 to 2.7 s for the 12 targets. After deleting both `Generated/Sdl3`
    folders, the first run writes 266 files and the second changes 0; the SHA-256 over all `*.g.cs` is the same after
    both (`97a26416b3f4d165...`). No CR and no BOM in the output.
  - Platform merge, as printed: 21 functions (Windows 3, iOS 3, Android 13, Linux 2) and 2 constants
    (`SDL_ANDROID_EXTERNAL_STORAGE_READ`/`WRITE`) are declared on some targets only and get `[SupportedOSPlatform]`.
  - Layout report: 21 structs differ between targets (`SDL_Surface`, `SDL_UserEvent`, `SDL_hid_device_info`,
    `SDL_VirtualJoystickDesc`, ...), all on browser-wasm only and only through pointer fields, which the generated
    definitions follow. No unhandled variance.
  - Export cross-check against `artifacts/native/linux-x64/lib/libjade_native.so`:

    ```text
    Sdl3: export cross-check: 1270 exported SDL_*, 982 bound, 288 excluded:
      58 declared in SDL3/SDL_audio.h: audio subsystem built out of jade_native (101: SDL_AUDIO=OFF, ...)
      95 declared in SDL3/SDL_gpu.h: GPU subsystem built out of jade_native (101: SDL_GPU=OFF, ...)
      6 declared in SDL3/SDL_main.h: platform main and app lifecycle glue, out of scope until the 104 ADRs
      102 declared in SDL3/SDL_render.h: 2D renderer built out of jade_native (101: SDL_RENDER=OFF, ...)
      4 only declared for Microsoft GDK builds (SDL_PLATFORM_GDK), which no ADR-0007 target is: SDL_GDKResumeGPU, ...
      7 takes a va_list, whose type differs on every ABI: SDL_IOvprintf, SDL_LogMessageV, SDL_SetErrorV, ...
      1 the entry point of SDL's dynamic API loader, declared in no public header: SDL_DYNAPI_entry
      15 variadic (printf-style); P/Invoke cannot call variadic functions portably: SDL_IOprintf, SDL_Log, ...
    ```

  - `dotnet build -c Release --no-incremental`: `0 Avertissement(s)`, `0 Erreur(s)`.
  - `dotnet test -c Release`: total 101, succeeded 101 (85 layout tests, 15 SDL tests, 1 assembly test). With
    `DISPLAY`, `WAYLAND_DISPLAY` and `XDG_RUNTIME_DIR` unset, 100 pass and the window test skips with
    `SDL video is unavailable here (headless?): No available video device`. With `SDL_VIDEO_DRIVER=dummy` as well,
    101 pass.
  - `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'`: exit 0. The converted-project
    check of each script: exit 0 for all three. `MSBuildTreatWarningsAsErrors=true dotnet build scripts/<name>.cs
    --no-incremental`: exit 0 for all three.
  - Negative checks, each reverted (the config was restored byte for byte and regenerated with 0 changes):
    - dropping the `SDL_DYNAPI_entry` exclusion fails with `SDL_DYNAPI_entry is exported but neither bound nor
      excluded`;
    - dropping `SDL_copying.h` from `ExcludedHeaders` fails with `header SDL3/SDL_copying.h is neither bound nor
      excluded`;
    - dropping the `SDL_PRIs64` exclusion fails with `declarations differ between win-x64 and linux-x64:
      ... I64d ... vs ... ld`.

    While writing the config, rules keyed `SDL_GetRectAndLineIntersection.x1` failed as stale entries: the C
    parameter is `X1`.
  - Not verified: generator runs on Windows and macOS hosts, and the export cross-check of PE and Mach-O libraries
    (not implemented, see Follow-ups). No binding was called on another OS. CI has not run: nothing is pushed.
- Decisions taken (and ADRs added): no ADR added.
  - C `bool` is C# `bool` (`PrimitiveType.Bool`, the `LibraryConfig` default). The `[LibraryImport]` generator
    rejects a bare `bool` even with runtime marshalling disabled (SYSLIB1051, 796 errors on the first build), so raw
    imports mark `bool` parameters and returns `[MarshalAs(UnmanagedType.U1)]`. The generated stub passes one byte
    and reads a returned one as `!= 0`. Struct fields and function pointer signatures stay plain `bool`; the layout
    tests confirm the runtime layout of the structs that hold one (`SDL_KeyboardEvent`, ...). `[LibraryImport]`
    stays the import style, as the user chose in 201. CLAUDE.md's interop bullet now says so.
  - Headers: `Headers` lists the bound ones; `ExcludedHeaders` gives each excluded one a reason: audio, GPU and
    render (built out by 101), `SDL_main.h` and `SDL_main_impl.h` (out of scope, 104), and headers that only hold C
    compiler macros, inline functions, old names, the license or the revision. Excluded headers are still parsed,
    so the export cross-check knows which exported functions they declare. `SDL_intrin.h` needed 9 empty stubs in
    `sysroot/` (`mmintrin.h`, `immintrin.h`, `arm_neon.h`, ...).
  - Constants: every object-like macro of a bound header is probed as `static const __typeof__(M) p = M;`. A first
    parse keeps those that evaluate to an integer, float or string literal; a second parse only probes those and
    must have no error. 484 constants: 266 hints, 148 property names, version, window position, haptic, hat, time
    and limit values. Strings are `static ReadOnlySpan<byte> X => "..."u8` without an explicit NUL. Spans then
    compare equal to strings that SDL returns, and passing one costs a stack copy. Constants of type `long` take the
    width of the target's `long`, because `SDL_SINT64_C` expands to `L` on LP64 and `LL` on LLP64. Docs come from
    the comment block or trailing comment of the macro, or else from the "`SDL_PROP_X`: text" item of the function
    that takes it. `SDL_LINE`, `SDL_FILE`, `SDL_FUNCTION`, `SDL_ASSERT_FILE` (values of the probe file),
    `SDL_ASSERT_LEVEL`, `SDL_PRI*`, `SDL_SIZE_MAX` and `SDL_ICONV_E*` (target-dependent) are excluded with reasons.
  - Macro enums replace 201's flag macros: `MacroEnum.Flags` or `MacroEnum.Values`, a prefix and an optional
    pattern. There are 18, among them `Keycode`, `Keymod`, `BlendMode`, `MouseButtonFlags`, `WindowFlags` and the GL
    and haptic types. Configured macro enums and the C enums of bound headers are bound even when no function uses
    them (`MessageBoxColorType` indexes `MessageBoxColorScheme.Colors`).
  - Typed IDs: the 16 `SDL_*ID` typedefs are C# enums without members, for example `public enum WindowID : uint {}`.
    A same-size wrapper struct would read better but rely on how every ABI passes one-field structs (Apple arm64,
    wasm32), which ADR-0012 already avoided for `bool`; an enum is passed exactly like its integer. Typed constants
    use them: `Sdl.TouchMouseID = (MouseID)0xFFFFFFFF`.
  - `const char*` returns stay `byte*`: a span cannot tell NULL from empty (`SDL_GetHint` returns NULL for an unset
    hint), and `MemoryMarshal.CreateReadOnlySpanFromNullTerminated` already converts without allocation.
  - `SDL_Event` binds all 40 members. Anonymous field types are named after their owner and field
    (`GamepadBindingInput`, `GamepadBindingInputAxis`, ...). An array of non-primitives becomes a nested
    `[InlineArray]` named `<Field>Array`: only `MessageBoxColorScheme.ColorsArray`. Arrays of primitives stay `fixed`.
    No bound header has a bit-field or a C11 anonymous member, so the generator still rejects those.
  - Callback typedefs keep their name in the model (`TypeRef.Alias`). Each parameter or field that uses one gets
    the typedef's summary, parameters and return value in its remarks; the C# type stays `delegate* unmanaged[Cdecl]`.
  - Platform availability: a function or constant that only some targets declare is bound on all of them, with
    `[SupportedOSPlatform]` for exactly the platforms that declare it (`PlatformMerge`). Its signature must match
    wherever it is declared, and only enums, handles and opaque structs may come along with it. `SupportedPlatforms`
    in the config marks functions declared everywhere but documented for one platform:
    - `SDL_SetWindowFillDocument`: browser;
    - `SDL_Metal_*`: ios and macos;
    - `SDL_SetX11EventHook`: linux.
  - Friendly overloads: `ParameterInference` reads SDL's `\param` text. It infers:
    - `Span` when an integer parameter with a count-like name counts the pointer;
    - `out` for "filled", "store", "receive", "written here", ...;
    - `ref` for "to be modified" and "read and adjusted";
    - `in` for a const pointer to a struct.

    Arrays and buffers whose length is not tied to a parameter stay raw. I went through the 487 friendly parameters
    this produces (UTF-8 strings and scalar `out` values only skimmed); 28 config rules add or correct directions.
    A friendly overload only pins memory for the call, so 10 `ParameterRule.Raw` entries keep raw the pointers that
    SDL returns or keeps (`SDL_GetStringProperty`'s default, `SDL_strchr`/`strstr`/..., `SDL_SetScancodeName`, the
    dialog filters). I checked those in the pinned SDL sources (tarball SHA-256 equal to the recipe's).
  - Naming:
    - all-caps words start a new word after digits (`INDEX1LSB` to `Index1Lsb`);
    - `Words` adds `IO`, `IOStream`, `VSync`, `MouseID`, `TouchID` and `WindowPos`;
    - eight types named like a type of the SDK's implicit usings keep an `Sdl` prefix: `SdlDateTime`,
      `SdlEnvironment`, `SdlGuid`, `SdlMutex`, `SdlSemaphore`, `SdlThread`, `SdlThreadPriority`, `SdlThreadState`;
    - instance methods drop the native stem (`MethodStems`: `IO` for `SDL_IOStream` gives `stream.Read`, `Hid`,
      `Object`). A shortened name that would hide an `object` member keeps the full one (`gamepad.GetGamepadType`).
  - Leading digits (from 003): `LeadingDigitPrefix = "Digit"` replaces the `_` of 201 everywhere:
    - `Scancode` and `Keycode`: `Digit0` to `Digit9`;
    - `PackedLayout`: `Digit332`, `Digit4444`, `Digit1555`, `Digit5551`, `Digit565`, `Digit8888`, `Digit2101010`,
      `Digit1010102`;
    - `BitmapOrder`: `Digit4321`, `Digit1234`;
    - `TimeFormat`: `Digit24Hr`, `Digit12Hr`.
  - Other mappings:
    - `wchar_t*` becomes `void*` (the pointee is 2 bytes on Windows and 4 elsewhere);
    - `VkSurfaceKHR` is `ulong` on every target (`TypedefMappings`);
    - `VkInstance` and `VkPhysicalDevice` are `ForeignHandles`, without instance methods.
  - Exclusions: 15 variadic and 7 `va_list` functions. SDL 3.4.16 has no non-variadic sibling for any of them.
  - The export cross-check reads the staged library's ELF `.dynsym` (`ElfExports`). Every exported `SDL_*` must be
    bound, excluded by name, or declared in an excluded header, and every binding must be exported. A library in
    another format skips the check with a message.
  - Config entries are now checked against all targets together, so an entry only one platform uses is not stale.
  - With the user's approval, `.editorconfig` turns CA1720 off for `src/Jade.Interop/Generated/`, like the four CA
    rules of 003. It rejected C names such as `ptr`, `guid`, `PropertyType.String` and `SystemCursor.Pointer`.
  - Hand-written helpers (`Sdl.Macros.cs`): `ButtonMask`, `VersionNum`, `VersionNumMajor`/`Minor`/`Micro`,
    `WindowPosUndefinedDisplay`, `WindowPosCenteredDisplay`, `WindowPosIsUndefined`, `WindowPosIsCentered`.
    Inline functions (`SDL_PointInRect`, `SDL_RectEmpty`, swaps, bit helpers) are not bound: they are trivial in C#
    or covered by the BCL.
- Deviations from the brief:
  - CA1720 is turned off for generated bindings (approved by the user; it was not foreseen).
  - "whatever depends only on" audio, GPU and render: no other header does. `SDL_AudioDeviceEvent` and
    `SDL_RenderEvent` stay `SDL_Event` members for its layout, so `AudioDeviceID` is bound as a type.
  - `SDL_main.h` is excluded too (out of scope per the brief).
  - Platform-only functions are bound with `[SupportedOSPlatform]` rather than excluded; Android and iOS glue is
    callable for 104.
- Follow-ups:
  - 103: read PE and Mach-O export tables in the cross-check (today Windows and macOS hosts skip it), and run the
    generator on those hosts.
  - 104: decide on `SDL_main.h` (`SDL_SetMainReady`, `SDL_RunApp`, `SDL_EnterAppMainCallbacks`). The Android and iOS
    functions are already bound for that platform glue.
  - 104, 105: check on Apple arm64 and wasm32 that handle structs (`Window`, ...) pass like pointers, as ADR-0006
    assumes. The ID enums avoid the question.
  - 202, 204:
    - `ParameterInference` follows SDL's doc phrasing; dawn.json carries its own metadata;
    - `ExportPrefixes`, `MacroEnums`, `IdTypedefs`, `MethodStems` and `PlatformMerge` apply to any library;
    - `[MarshalAs(UnmanagedType.U1)]` covers C `bool`, while `ma_bool32` and `WGPUBool` keep their width.
  - 205: NativeAOT publish of the bindings, including the bool-marshalling stubs.
  - Orchestrator:
    - ADR-0006 may want the naming rules recorded here: the `Sdl` prefix for BCL clashes, `Digit`, method stems;
    - `Digit332` and `Digit24Hr` read poorly; `Renames` can override per member;
    - `design/architecture.md` does not describe the platform merge, the export cross-check or the layout tests;
    - SDL upgrades: review the diff of inferred friendly overloads, since stale-config checks cannot see a changed
      `\param` text.
