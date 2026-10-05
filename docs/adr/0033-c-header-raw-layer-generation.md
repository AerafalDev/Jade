# 0033. SDL3 and miniaudio raw layer generation from their C headers

- Status: Accepted
- Date: 2026-10-05

## Context

Roadmap task 8 builds the C header front-end of [0026](0026-binding-generator-pipeline.md) and the
raw layers of `Jade.Sdl` and `Jade.MiniAudio`, with the mapping rules of
[0009](0009-interop-mapping-conventions.md) and [0027](0027-interop-mapping-rules.md), the
projection and emitter of [0032](0032-webgpu-raw-layer-generation.md) and the names of
[0034](0034-raw-layer-with-dotnet-names.md). Several points those records leave open have to be
settled to emit code: how twelve parses become one model, what the configuration annotates, which
SDL3 headers are bound, and what the miniaudio shim allocates.

Verified on 2026-10-05 at the commits of `build/versions.json` (SDL `release-3.4.18`, miniaudio
0.11.25), with ClangSharp 21.1.8.4 and SDK `11.0.100-rc.1.26425.128`:

- Parsed for the 12 triples of [0012](0012-supported-targets.md), the declarations that differ
  between targets are all in miniaudio: `ma_event`, `ma_mutex`, `ma_semaphore` and `ma_thread` are
  Win32 handles on Windows and pthread types elsewhere; `ma_proc` is a function pointer where
  `__GNUC__` is defined and `void*` for the MSVC triples, which do not define it; `ma_uintptr` is
  `ma_uint32` on `browser-wasm` and `ma_uint64` elsewhere; `ma_wchar_win32` is `wchar_t` on
  Windows and `ma_uint16` elsewhere; miniaudio declares `wchar_t` itself for MSVC. `ma_context` and
  `ma_device` have backend members that depend on the platform, inside anonymous unions.
- Holding one of those types by value makes `ma_async_notification_event`,
  `ma_device_job_thread`, `ma_engine`, `ma_fence`, `ma_job_queue`, `ma_log`,
  `ma_resource_manager`, `ma_sound` and `ma_sound_inlined` platform-dependent too. No function
  of miniaudio's API takes an `ma_thread`.
- With `SDL_AUDIO=OFF` ([0031](0031-native-build-definitions.md)), SDL3 still compiles the core of
  `src/audio/` (its CMake build globs `src/audio/*.c` unconditionally), but no audio driver, and
  `SDL_Init(SDL_INIT_AUDIO)` fails with "SDL not built with audio support" (`src/SDL.c`). The
  device functions of `SDL_audio.h` can only fail; WAV loading and audio streams work but duplicate
  miniaudio.
- `SDL3/SDL.h` does not include `SDL_main.h`, which declares `SDL_RunApp`, `SDL_SetMainReady`,
  `SDL_EnterAppMainCallbacks`, `SDL_GDKSuspendComplete`, and `SDL_RegisterApp` and
  `SDL_UnregisterApp` on Windows. Without `SDL_MAIN_HANDLED` it renames `main` and includes
  `SDL_main_impl.h`, which defines entry points; with it, it still declares `SDL_main`, which the
  application provides. `SDL_assert.h` declares the MSVC intrinsic `__debugbreak` on Windows.
- Unless `SDL_DISABLE_OLD_NAMES` is defined, `SDL_oldnames.h` defines the SDL2 names as macros that
  expand to undeclared identifiers (`SDL_WINDOW_SHOWN` to
  `SDL_WINDOW_SHOWN_deprecated_windows_are_shown_by_default`), so that old code fails to compile.
- SDL3's flags are integer typedefs whose values are macros, and neither their position nor their
  prefix delimits them: `SDL_WINDOWPOS_*` follows `SDL_WindowFlags`, `SDL_BUTTON_LEFT` (a button
  index) shares `SDL_BUTTON_` with the `SDL_BUTTON_LMASK` mask, `SDL_MESSAGEBOX_BUTTON_*` starts
  with the prefix of `SDL_MessageBoxFlags`, and `SDL_WINDOW_SURFACE_VSYNC_*` with that of
  `SDL_WindowFlags`.
- libclang evaluates a macro through a variable of its own type,
  `const __typeof__(M) v = M;`, with `clang_Cursor_Evaluate`: 992 SDL3 and 15 miniaudio macros
  give integers, floats and string literals (`CXEval_StrLiteral` for a `char` array).
- libclang reports a parameter declared as an array (`const ma_backend backends[]`) with its array
  type, not the pointer C adjusts it to.
- `clang_Cursor_getOffsetOfField` and the sizes and alignments of the canonical member types give,
  for every non-opaque structure of both libraries on the 12 targets, the offsets of a sequential
  layout: miniaudio's `MA_ATOMIC` alignments never exceed the natural alignment of their types.
- The `LibraryImport` generator rejects a `bool` parameter or result without marshalling
  information (SYSLIB1051), even under `[assembly: DisableRuntimeMarshalling]`, although the
  runtime then passes `bool` as one byte ("Disabled runtime marshalling", .NET documentation).
- `libSDL3.so` and `libminiaudio.so` built by `scripts/build-native.cs` export every function that
  the generated raw layers import for Linux (`ExportTests` of the test projects, through
  `NativeLibrary.TryGetExport`).
- `ma_vec3f` (three `float`) returned by value from `ma_spatializer_listener_get_position` arrives
  intact as `System.Numerics.Vector3` on `linux-x64`.

## Decision

### Front-end

- Each target's translation unit is collected on its own (`Clang/TargetModelBuilder.cs`): the
  functions, structures, unions, enums and typedefs of the library's files, the object-like macros,
  and the values of the selected macros, from a second parse that appends their evaluation
  variables. A declaration is the library's when its file is under the pinned sources or is a shim
  header; the generator's runtime headers and clang's builtins never are.
- The merge (`Clang/ClangModelBuilder.cs`) makes each declaration available on the platform
  families whose targets declare it. A declaration on some targets of a family only, or that
  differs between targets, fails the generator unless the configuration makes it opaque or
  excludes it; one run lists every such declaration. Enum values and macro values merge one by
  one, so a value can have its own availability.
- Typedefs are kept by name ([0026](0026-binding-generator-pipeline.md)) and resolved at
  projection; a typedef that only names a structure or an enum adds nothing. The C runtime names
  (`size_t`, `int8_t` to `uint64_t`, `intptr_t`, `uintptr_t`, `ptrdiff_t`, `va_list`, `wchar_t`)
  are builtin types whoever declares them.
- A structure never defined, or made opaque, is a handle; a pointer to a handle, through typedefs,
  is the handle; a declaration that holds a handle by value fails the generator, which lists every
  structure that holds an opaque type by value, transitively. A typedef that only renames a handle
  (`MSG` for `tagMSG`) is dropped.
- An anonymous structure or union is named after its parent and its member, `{Parent}{Member}`
  ([0027](0027-interop-mapping-rules.md)), with an `Element` suffix when the member is an array of
  it, since the array takes `{Parent}{Member}`.
- A record whose members do not lie where a sequential layout of their types puts them fails the
  generator unless it is opaque; the sizes come from clang and are only used for this check. Bit-fields,
  anonymous members, flexible arrays and arrays of arrays fail the same way.
- Static and inline functions, variadic functions, functions that use `va_list`, `wchar_t` (unless
  mapped) or `long double`, and global variables are skipped and listed in the output. Function-like
  macros are never bound and are counted, as are the object-like macros the configuration does not
  select.

### Configuration

`interop/<project>/bindings.json` keeps `library`, `exclude`, `names` and `words`
([0032](0032-webgpu-raw-layer-generation.md)); its `clang` object gains, strict like the rest:

- `defines`, macros defined for the parse only; `shims`, repository headers whose declarations
  belong to the library; `prefixes`, the prefixes .NET names drop, longest first; `memberPrefixes`,
  the words member and parameter names drop when they come first (miniaudio's Hungarian `p` and
  `pp`);
- `excludeHeaders`, headers whose declarations are left out, except the types the other headers
  use, and whose macros are never bound;
- `types`, C types mapped by name whatever they alias on each target: to a builtin type
  (`ma_uintptr` to `uintptr_t`), to `void*` (`ma_proc`), to `void` for a type only pointed to whose
  size differs between targets (`wchar_t`, so that `wchar_t*` is `void*`), or to a .NET type of the
  same layout (`ma_vec3f` to `System.Numerics.Vector3`);
- `opaque`, structures and typedefs made handles, with the reason, and `opaqueAllocators`, the
  patterns of the shim functions that allocate and free each of them;
- `booleans`, the integer typedefs that hold booleans (`ma_bool8`, `ma_bool32`);
- `enums`: a C enum marked as flags, or an integer typedef whose values are the macros a regular
  expression selects, as flags or not (`SDL_WindowFlags` and `SDL_WINDOW_(?!SURFACE_VSYNC_).+`);
- `constants`, regular expressions that select the object-like macros bound as constants.

Every entry must match something; a macro matched by two patterns fails the generator.

### Macros

- Selected macros are evaluated by clang as typed variables, never read from their tokens. An
  enum made of macros takes the integer type of its typedef. A constant takes a fixed-width integer
  of the size and signedness of its C type, `float` or `double`, or, for a string, a
  `ReadOnlySpan<byte>` property over a UTF-8 literal, which is NUL-terminated in memory so that a
  `fixed` pointer to it is a C string.
- Enum values drop the longest underscore-separated prefix that all the values of their enum share,
  keeping at least one word (`SDL_EVENT_KEY_UP` gives `KeyUp` in `EventType`, `ma_format_f32`
  gives `F32`); this replaces the "prefix they share with their enum" of
  [0027](0027-interop-mapping-rules.md), which `SDL_PIXELFORMAT_*` in `SDL_PixelFormat` does not
  satisfy. A name without lower-case letters is SCREAMING_CASE: its parts are words, lowered before
  they become PascalCase (`Up`, not `UP`); a lower-case part such as `vec3f` stays one word.
- The values of a signed enum that is not a set of flags, and any negative value, are written in
  decimal; the others in hexadecimal. A `float` is written with the shortest digits that give it
  back (`9.80665f`).

### Projection and emission

- C `bool` stays `bool` ([0027](0027-interop-mapping-rules.md)). In `LibraryImport` signatures it
  carries `[MarshalAs(UnmanagedType.U1)]`, which the generator requires, and its stub converts one
  byte; structure fields and function pointer types keep `bool` as is.
- A union is `[StructLayout(LayoutKind.Explicit)]` with `[FieldOffset(0)]` on every field
  ([0009](0009-interop-mapping-conventions.md)). A fixed-size array member gets its own
  `[InlineArray(N)]` type, `{Structure}{Member}`, in its structure's namespace.
- `NativeMethods` is split into one partial file per C header, `NativeMethods.{header}.g.cs`
  ([0027](0027-interop-mapping-rules.md)); the main file declares the class and the library name.
- `Jade.Sdl` and `Jade.MiniAudio` declare the assembly attributes of `Jade.Wgpu`
  ([0032](0032-webgpu-raw-layer-generation.md)), and import `SDL3` and `miniaudio`.

### SDL3

- Bound headers: `SDL3/SDL.h` and `SDL3/SDL_main.h`, parsed with `SDL_MAIN_HANDLED`, since a .NET
  application owns its entry point, and `SDL_DISABLE_OLD_NAMES`.
- `SDL3/SDL_audio.h` is excluded: SDL3 is built without its audio subsystem, so its device
  functions only fail, and audio goes through miniaudio ([0005](0005-sdl3-platform-layer.md),
  [0006](0006-miniaudio-for-audio.md)). The types other headers use, such as `SDL_AudioDeviceID`
  in `SDL_AudioDeviceEvent`, stay.
- Excluded declarations: `SDL_main` (the application's), `__debugbreak` (an MSVC intrinsic),
  `SDL_DUMMY_ENUM` and `SDL_alignment_test` (compile-time checks).
- The integer typedefs with macro values become enums (22, among them `InitFlags`, `WindowFlags`,
  `Keymod`, `Keycode`, `MouseButtonFlags` and the GPU usage flags); the hints, the property names,
  the version and a few sentinels (`SDL_WINDOWPOS_*`, `SDL_*_VSYNC_*`, `SDL_HAPTIC_INFINITY`) are
  constants.

### miniaudio

- Opaque: `ma_context` and `ma_device` (backend members), `ma_event`, `ma_mutex` and
  `ma_semaphore` (Win32 or pthread), and the structures that hold them by value:
  `ma_async_notification_event`, `ma_device_job_thread`, `ma_engine`, `ma_fence`, `ma_job_queue`,
  `ma_log`, `ma_resource_manager` and `ma_sound`. `ma_thread`, which only opaque structures hold,
  `ma_sound_inlined`, which only `ma_engine` holds, and the Windows-only
  `ma_IMMNotificationClient` are excluded.
- The shim `build/miniaudio/jade_miniaudio.h` is bound as a header of the library and declares
  `jade_{name}_alloc` and `jade_{name}_free` for every opaque type; the generator fails when one
  lacks them or has another signature ([0031](0031-native-build-definitions.md)).

### Verification

- `tests/Jade.Sdl.Tests` and `tests/Jade.MiniAudio.Tests` follow `Jade.Wgpu.Tests`: managed checks
  of the raw layer, an export test that every function imported for the host's platform is
  exported by the host's library, and smoke tests. SDL3's initialize video with the `dummy` driver,
  and with the host's display when there is one, and create a window; miniaudio's initialize a
  context in shim-allocated memory, compare the header's version string with the library's, and
  read a `Vector3` returned by value.
- `BuildConventionTests.InteropLibraryDisablesRuntimeMarshalling` covers the three interop
  assemblies.

## Consequences

- `Jade.Sdl` has 350 generated files (1,177 functions, 116 enums, 43 handles, 95 public and 30
  internal structures, 15 inline arrays, 588 constants) and 2,859 public declarations;
  `Jade.MiniAudio` 292 files (955 functions, 48 enums, 13 handles, 2 booleans, 59 public and 146
  internal structures, 21 inline arrays, 15 constants) and 871 public declarations.
- Platform-specific SDL3 functions carry platform attributes (`SDL_GetAndroidJNIEnv` on Android,
  `SDL_SetWindowsMessageHook` on Windows, `SDL_SetLinuxThreadPriority` on Linux).
- A miniaudio update that changes a layout on some target fails the generator until the
  configuration follows; a structure made opaque needs a shim allocator, so changing the opaque
  list means rebuilding the natives.
- The `_w` functions of miniaudio take `void*` for their `wchar_t*`: the idiomatic layer passes
  UTF-16 on Windows and UTF-32 elsewhere.
- SDL3's logging and formatting functions (`SDL_Log`, `SDL_SetError`, `SDL_snprintf` and others)
  are variadic and need hand-written alternatives, as [0027](0027-interop-mapping-rules.md) planned.
- Not verified: the regeneration and the export lists on Windows and macOS until the CI runs on the
  pull request; the `Vector3` return on arm64 (three registers of an HFA) and on WebAssembly until
  the layout tests (roadmap tasks 9 and 18); the layouts themselves, which the generator only
  checks against clang's, until those tests.
- Public names that collide with framework types (`Thread`, `Mutex`, `Semaphore`, `Process`,
  `Environment`, `DateTime`, `Guid`, `Condition`, `Timer`) resolve inside the interop namespaces but
  are ambiguous for applications that import both; roadmap task 13 decides, as task 12 does for
  `Buffer`.

## Alternatives considered

- **Group flag macros by position after their typedef, or by prefix alone**: both fail on SDL3 as
  verified above; explicit regular expressions keep the grouping in the configuration, where it is
  reviewed with the generated enums.
- **Read macro values from their tokens**: forbidden by [0027](0027-interop-mapping-rules.md), and
  wrong for macros built from other macros (`SDL_WINDOWPOS_CENTERED`, `SDL_UINT64_C`).
- **Skip every declaration that uses `wchar_t`**: would drop `ma_sound_config`,
  `ma_resource_manager_data_source_config` and `SDL_hid_device_info`, which only hold `wchar_t`
  pointers.
- **Bind `SDL_audio.h`**: its utilities work, but they duplicate miniaudio and sit next to device
  functions that always fail.
- **`byte` or a `Bool8` structure for C `bool`**: would keep the imports free of conversion stubs,
  but contradict [0027](0027-interop-mapping-rules.md) for a conversion of one byte.
- **Layout from clang's offsets with explicit offsets**: excluded by
  [0009](0009-interop-mapping-conventions.md); the check makes the same information an error
  instead.
