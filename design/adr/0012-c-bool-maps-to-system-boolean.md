# ADR-0012: C `bool` maps to `System.Boolean`

- Status: Accepted
- Date: 2026-10-02
- Amends: the "Booleans and widths" rule of ADR-0006, for C `bool` only

## Context

ADR-0006 and CLAUDE.md said "no `bool` in signatures" and mapped C booleans to explicitly sized
types. Following that rule, task 201 exposed SDL3's `bool` as `byte` (`Sdl.Init(...) == 0`), and
noted that a friendlier wrapper struct would need its argument passing checked on Apple arm64.

The rule came from runtime marshalling, where `bool` can become a 1-, 2- or 4-byte value. Jade
disables runtime marshalling. With `DisableRuntimeMarshallingAttribute`, `System.Boolean` maps to
native `bool`: one byte, passed as is and not normalized. Sources: Microsoft Learn, "Disabled
runtime marshalling" (type table) and "Native interoperability best practices" (blittable types),
both read on 2026-10-02.

## Decision

- C `bool`/`_Bool` (one byte, value 0 or 1) maps to C# `bool` in signatures and struct fields.
- Boolean typedefs over wider integers keep their integer width: `WGPUBool` (`uint32_t`) and
  miniaudio's `ma_bool32`, for example. Their exact C# type (the integer, or a same-size wrapper) is
  decided per library by tasks 202 and 204.
- No wrapper struct for C `bool`. Unlike a wrapper struct, `bool` is a primitive 1-byte type, so it
  is passed exactly like the current `byte`. The Apple arm64 question from 201 only concerned
  wrapper structs.
- Never map an integer-typed flag to `bool`. Values are not normalized: anything other than 0 or 1
  coming from native code would produce a non-canonical `bool`.

## Consequences

- Task 203 switches the SDL3 mapping from `byte` to `bool` in the generator and regenerates the
  slice. Calls become `if (!Sdl.Init(...))`.
- CLAUDE.md's interop rules are updated to match.

## Notes from task 203 (2026-10-03)

- `[LibraryImport]` rejects a bare `bool` even with runtime marshalling disabled (SYSLIB1051), since
  this ADR only reasoned about `[DllImport]`. Generated imports therefore mark `bool` parameters and
  returns `[MarshalAs(UnmanagedType.U1)]`. The generated stub passes one byte and reads a return as
  `!= 0`, so the public API and the ABI are as decided. Struct fields stay plain `bool`.
