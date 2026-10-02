# 106: Jade.Native package

- Depends on: 103
- ADRs: 0002, 0003

## Goal

`src/Jade.Native` packs `artifacts/native/<rid>/` into `Jade.Native.nupkg`. `Jade` depends on it
with an exact version, a size guard protects the 250 MB limit, and a consumer project restored
from a local feed runs, and publishes with NativeAOT, on the host RID.

## Context

- ADR-0002 defines the layout: `runtimes/<rid>/native/`, `buildTransitive/Jade.Native.targets` and
  `THIRD-PARTY-NOTICES.md`, with no managed code.
- Desktop natives come from 103: a local build for the host plus `scripts/fetch-native.cs` for the
  others. 104 and 105 add their own `buildTransitive` wiring later, or hand it to this task if they
  finish first.

## Scope

- `src/Jade.Native/Jade.Native.csproj`: a packaging-only project (no assembly in the package) that
  includes every RID present under `artifacts/native/`. The release pipeline (107) requires all
  RIDs; local runs warn about missing ones.
- `buildTransitive/Jade.Native.targets` with placeholders for browser-wasm and iOS wiring, plus
  anything desktop needs (probably nothing; verify).
- `Jade` gains a package dependency on `Jade.Native` at `[$(Version)]`.
- Size guard: packing fails above a threshold (for example 240 MB) with a message pointing at the
  ADR-0002 split plan. Report the current size per RID.
- `tests/Jade.PackageTests` (or a sample): restores `Jade` from `artifacts/packages`, calls
  `jade_native_abi_version` through Jade.Interop (a hand-written import is fine until 205), runs,
  and publishes with NativeAOT on the host RID.

## Out of scope

- Publishing to nuget.org (107), mobile and web specifics beyond placeholders.

## Acceptance criteria

- [ ] `Jade.Native.nupkg` contains `runtimes/linux-x64/native/libjade_native.so` (plus any other RID
      available) and the notices file. Listed contents in the Outcome.
- [ ] The `Jade` nuspec shows `Jade.Native` with an exact version range.
- [ ] The consumer runs from the local feed with `dotnet run` and as a NativeAOT binary.
- [ ] The size guard is tested by temporarily lowering the threshold, then restored.

## Verification

Pack commands, the nupkg listing, consumer run output, NativeAOT publish and run output.

## Pitfalls

- NuGet caches packages by version: bump or clear the local cache for this package between
  iterations, or the consumer silently uses stale natives.
- NativeAOT with a shared native library still needs it next to the binary; check the publish
  output.
- From 001: the house `.gitignore` rule `[Bb]uild/` ignores any folder named `build`, for example
  `src/Jade.Native/build/`. `buildTransitive/` is not affected. Check new folders with
  `git check-ignore -v`.
- The package test project must set `<IsTestProject>true</IsTestProject>` (see CLAUDE.md).
- From 201: `tests/Jade.Interop.Tests` copies the staged library from `artifacts/native/<rid>/` to
  run its native tests. Once `Jade.Native` exists, decide whether that test project consumes it
  instead, and record the choice.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
