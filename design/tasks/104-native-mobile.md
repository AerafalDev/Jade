# 104: Android and iOS RIDs

- Depends on: 103
- ADRs: 0003, 0004, 0007

## Goal

`jade_native` builds for android-arm64, android-x64, ios-arm64, iossimulator-arm64 and
iossimulator-x64 in CI. Two Proposed ADRs settle how Jade delivers the mobile platform glue that
SDL3 requires.

## Context

- SDL3 on Android needs its Java side (`SDLActivity` and related classes) in the app. On iOS, it
  must own or hook the UIKit application lifecycle. A .NET app that only P/Invokes into
  `jade_native` is not enough.
- That likely means `net10.0-android` and `net10.0-ios` target frameworks somewhere (in Jade, or in
  a platform glue project packed into Jade) and the matching workloads. None are installed locally.
- How a .NET iOS app consumes native code from a NuGet package (static `.a`, `.xcframework` or
  dynamic framework, through `NativeReference` or `runtimes/`) has not been verified. Settle it from
  the official docs (Microsoft Learn MCP) and a real build.

## Scope

- Android: NDK version and minimum API level chosen and recorded; `libjade_native.so` per ABI with
  static libc++; Vulkan backend for Dawn; 16 KB page-size alignment (check the current Google Play
  requirement); export list as on Linux.
- iOS: choose the form of `jade_native` (static, xcframework or dynamic framework) and the minimum
  iOS version; Metal backend; device and simulator slices.
- Extend `native.yml` with these RIDs. Android builds on Linux; iOS needs macOS runners.
- Two Proposed ADRs:
  1. Android glue: where SDL3's Java code lives, and how it reaches the app (AAR, Java sources in a
     binding project, ...).
  2. iOS glue: lifecycle ownership (SDL main callbacks vs a .NET-owned `UIApplication`), and how
     `jade_native` is linked.
- Extend `src/Jade.Native` `buildTransitive` targets for iOS linking, if 106 is done. Otherwise
  hand the details to 106 in the Outcome.

## Out of scope

- Building the glue projects themselves (follow-up tasks after the ADRs are accepted), browser
  (105).

## Acceptance criteria

- [ ] android-arm64 and android-x64 build locally if the user provides an NDK (ask), otherwise in
      CI. The Outcome says which.
- [ ] iOS builds exist in the workflow. They cannot be verified on this Linux machine; say so.
- [ ] Both ADRs are written, with the alternatives considered and sources linked.

## Verification

For Android: `llvm-readelf -d` and `-l` on the `.so` (no libc++_shared dependency, alignment), plus
the export list. For iOS: whatever CI provides once pushed.

## Pitfalls

- xmake's simulator selection for `iphoneos` could not be checked from Linux; check it on macOS.
- Dawn on Android requires a minimum API level for Vulkan. Align it with what .NET for Android
  supports.
- From 101: the NDK links with lld, which rejects a version script naming an undefined symbol
  (checked with LLD 23.1.1; GNU ld accepts it). Every exported name must exist on Android, so the
  export lists may need per-platform filtering. `JNI_OnLoad` must be exported for `SDLActivity`.
- From 101: a static `jade_native` (iOS, if chosen) needs the upstream archives merged into it.
  `native/rules/bundle.lua` raises for non-shared targets today.
- From 102: `-static-libstdc++ -static-libgcc` and `--gc-sections` are added on Linux only.
  Android's static libc++ comes from the NDK toolchain: check it with `llvm-readelf -d`. Dawn on
  Android needs its own options (Vulkan, maybe OpenGL ES) and dependency list.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
