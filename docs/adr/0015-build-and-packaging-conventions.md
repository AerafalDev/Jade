# 0015. Build and packaging conventions

- Status: Accepted
- Date: 2026-10-05

## Context

Jade's libraries are published as NuGet packages and must stay AOT-compatible, reproducible and
API-stable. These properties are cheap to enforce from the first project and expensive to add
later.

## Decision

### Build

- `Directory.Build.props` enables `Nullable`, `ImplicitUsings`, `GenerateDocumentationFile`,
  `AnalysisLevel` `latest-all`, `EnforceCodeStyleInBuild`, `Deterministic` and
  `UseArtifactsOutput`.
- `TreatWarningsAsErrors` and `ContinuousIntegrationBuild` are enabled in CI.
- Every library sets `IsAotCompatible=true`.
- `AllowUnsafeBlocks` and `SkipLocalsInit` are enabled only in interop projects.
- `LangVersion` is left to the TFM default (see [0003](0003-csharp-15-and-dotnet-11.md)).

### Roslyn components

- `Jade.SourceGenerators` and `Jade.Analyzers` target `netstandard2.0` and set
  `IsRoslynComponent` and `EnforceExtendedAnalyzerRules`.
- They are referenced with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` and shipped
  in `analyzers/dotnet/cs` of the `Jade` package.

### Dependencies

- Central package management with transitive pinning.
- NuGet lock files, restored in locked mode in CI.
- `NuGetAuditMode` set to `all`.

### Packaging

- SourceLink and `.snupkg` symbol packages.
- `EnablePackageValidation`.
- Public API tracked with the PublicApiAnalyzers (`PublicAPI.Shipped.txt` and
  `PublicAPI.Unshipped.txt`).

### Tests

- Tests run on Microsoft.Testing.Platform. The test framework is still open.

## Consequences

- Trimming and AOT warnings surface in every library build, not only when publishing an app.
- Any change to the public API shows up as a diff of the `PublicAPI.*.txt` files.
- Adding a package requires updating `Directory.Packages.props` and the lock files together.
