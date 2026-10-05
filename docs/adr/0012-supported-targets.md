# 0012. Supported targets

- Status: Accepted
- Date: 2026-10-05

## Context

Every runtime identifier (RID) multiplies native builds, CI time and testing. The target list
should cover the platforms players actually use and nothing more.

## Decision

| Family | RIDs |
| --- | --- |
| Desktop | `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-arm64`, `osx-x64` |
| Mobile | `android-arm64`, `android-x64`, `ios-arm64`, `iossimulator-arm64`, `iossimulator-x64` |
| Web | `browser-wasm` |

- Managed code is AnyCPU. RIDs only concern native binaries and the publication of samples.
- macOS and the iOS simulator ship universal binaries (arm64 and x64 merged with `lipo`), published
  under `runtimes/osx/native` and in the iOS xcframework.
- Linux natives are built against an old glibc baseline.
- Deliberately excluded: `win-x86`, `linux-arm`, `android-arm`, `android-x86`, tvOS and Mac
  Catalyst.
- Adding a RID requires a new ADR.
- Minimum OS versions are derived from Dawn's actual requirements, then pinned in
  `native/versions.json`.

## Consequences

- No 32-bit native target except `browser-wasm`, which is still covered by the layout tests
  because pointers and `size_t` are 4 bytes there.
- Until minimum OS versions are pinned, no supported-OS list can be published.
