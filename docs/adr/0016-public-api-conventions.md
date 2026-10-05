# 0016. Public API conventions

- Status: Accepted
- Date: 2026-10-05

## Context

Jade's public API spans the engine and the idiomatic interop layer. It has to be efficient on hot
paths, trimmable, explicit about platform support and stable once published.

## Decision

- Argument lists use `params ReadOnlySpan<T>`. No array overloads.
- Strings are accepted as `ReadOnlySpan<byte>` (UTF-8 `u8` literals) with a `string` overload,
  disambiguated with `[OverloadResolutionPriority]`.
- Types we do not own are extended with extension members (`extension` blocks).
- Unstable APIs are marked `[Experimental("JADExxxx")]`. Obsolete APIs use `[Obsolete]` with
  `DiagnosticId` and `UrlFormat`.
- Platform-specific APIs carry `[SupportedOSPlatform]` / `[UnsupportedOSPlatform]`
  (`Jade.Emscripten` is `"browser"`), checked by the CA1416 analyzer and guarded with
  `OperatingSystem.IsBrowser()`, `OperatingSystem.IsAndroid()` and similar.
- Validation and debug code sits behind feature switches (`[FeatureSwitchDefinition]`,
  `[FeatureGuard]`) so the trimmer removes it from release builds.
- Arguments are validated with the throw helpers (`ArgumentNullException.ThrowIfNull`,
  `ArgumentOutOfRangeException.ThrowIf…`, `ObjectDisposedException.ThrowIf`).

## Consequences

- Callers on hot paths can pass stack-allocated or UTF-8 data without allocating.
- Diagnostic IDs in the `JADE` range need a registry; it is created with the first experimental
  or obsolete API.
