# 0003. C# 15 and .NET 11, without preview features

- Status: Accepted
- Date: 2026-10-05

## Context

Jade targets desktop, mobile and the browser from a single code base and relies on recent runtime
and language features for interop and performance (`LibraryImport`, `DisableRuntimeMarshalling`,
`InlineArray`, extension members, `params ReadOnlySpan<T>`). .NET 11 reaches general availability in
November 2026; .NET 12, the next long-term support release, is expected in November 2027.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`: for `net11.0` the default `LangVersion`
computed by `Roslyn/Microsoft.CSharp.Core.targets` is `15.0`, which is also the maximum available
version of that compiler.

## Decision

- The whole repository uses C# 15 and .NET 11: `net11.0`, `net11.0-android`, `net11.0-ios`, and
  the `browser-wasm` runtime identifier for the browser. The exact TFM and RID combination of
  browser-specific projects is settled when they are created.
- `Jade.SourceGenerators` and `Jade.Analyzers` target `netstandard2.0`, as Roslyn components must.
- `LangVersion` is left to the TFM default.
- `global.json` pins the .NET 11 SDK: the release candidate until the November 2026 GA, then the
  GA release.
- No preview language or runtime feature is used: no `LangVersion=preview`, no
  `EnablePreviewFeatures`. Union types and the new unsafe model stay excluded while they are in
  preview. Their status is re-checked at every SDK update.
- The move to .NET 12 LTS happens after its release, through a new ADR.

## Consequences

- Consumers of the Jade packages must target .NET 11 or later.
- Until GA, builds run on a release-candidate SDK; CI installs the SDK from `global.json`.
- .NET 11 is a standard-term support release, so the .NET 12 migration is planned work, not an
  option.
