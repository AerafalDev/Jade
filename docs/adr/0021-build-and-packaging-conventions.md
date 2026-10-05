# 0021. Build and packaging conventions

- Status: Accepted
- Date: 2026-10-05

## Context

[0015](0015-build-and-packaging-conventions.md) set the build and packaging conventions before any
project existed, including NuGet lock files restored in locked mode. Scaffolding the solution
showed what the lock files actually bring and cost here, and settled details that 0015 left
implicit. This record restates the conventions as a whole and supersedes 0015.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`:

- Central package management with transitive pinning and exact versions already makes the
  resolved graph deterministic, and nuget.org neither deletes packages nor accepts a second upload
  of the same version (NuGet documentation, "Deleting packages" and "Publishing a NuGet package").
  NuGet's documentation also states that the lock file of a library is not used when another
  project consumes it.
- The lock files contained the SDK's implicit packages (`Microsoft.NET.ILLink.Tasks`
  `11.0.0-rc.1.26425.128`, added by `IsAotCompatible`), so every SDK update would rewrite every
  lock file; the `global.json` documentation asks for `rollForward` `disable` with lock files for
  that reason. Workloads and runtime identifiers of the samples would add more such entries.
- The SDK has no MSBuild property for `SkipLocalsInit`; it is the C# attribute
  `[module: SkipLocalsInit]`.
- SourceLink for GitHub ships with the SDK (`Sdks/Microsoft.SourceLink.GitHub`); no package is
  needed.
- A packaging project without build output fails to pack its symbol package with NU5017.

## Decision

### Build

- `Directory.Build.props` enables `Nullable`, `ImplicitUsings`, `GenerateDocumentationFile`,
  `AnalysisLevel` `latest-all`, `EnforceCodeStyleInBuild`, `Deterministic` and
  `UseArtifactsOutput`, and sets `IsPackable` to `false`; packages opt in.
- `TreatWarningsAsErrors` and `ContinuousIntegrationBuild` are passed on the command line in CI.
- Every .NET 11 library is AOT-compatible: `Directory.Build.targets` sets `IsAotCompatible` for
  every library project compatible with `net11.0`.
- `AllowUnsafeBlocks` is enabled only in interop projects. `[module: SkipLocalsInit]` is declared
  only in interop projects, with their first code.
- Assembly- and module-level attributes are declared in `Properties/AssemblyInfo.cs`, never with
  `AssemblyAttribute` items in project files.
- `LangVersion` is left to the TFM default, except in the Roslyn components (see
  [0019](0019-browser-and-roslyn-component-targeting.md)).

### Roslyn components

- `Jade.SourceGenerators` and `Jade.Analyzers` target `netstandard2.0` and set
  `IsRoslynComponent` and `EnforceExtendedAnalyzerRules`. They are not packages.
- `Jade` references them with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"
  PrivateAssets="all"` and ships them in `analyzers/dotnet/cs` of its package.

### Dependencies

- Central package management with transitive pinning; versions are exact and live only in
  `Directory.Packages.props`.
- No NuGet lock files.
- `NuGetAuditMode` is `all`.
- `global.json` pins the exact SDK (`rollForward` `disable`): the workload's Emscripten version
  ([0013](0013-browser-native-toolchain.md)), the SDK's implicit packages and the `latest-all`
  analyzer set all follow the SDK version.

### Packaging

- Common metadata, `README.md` (from `src/README.md`) and `icon.png` (from
  `docs/assets/package-icon.png`) come from `Directory.Build.props` and `Directory.Build.targets`
  ([0002](0002-license-and-public-identity.md)); each package has its own `Description`.
- SourceLink, embedded untracked sources and `.snupkg` symbol packages.
- `EnablePackageValidation`.
- Public API tracked with the PublicApiAnalyzers (`PublicAPI.Shipped.txt` and
  `PublicAPI.Unshipped.txt`) in every package that ships an assembly.
- The `Jade.Native.*` projects set `IncludeBuildOutput`, `IncludeSymbols` and their dependencies
  off: they ship native files only.
- Until a versioning scheme is chosen, packages are versioned `0.0.0-dev`.

### Tests

- Tests run on Microsoft.Testing.Platform, selected for `dotnet test` in `global.json`. The test
  framework is chosen in [0018](0018-test-framework.md).

## Consequences

- An SDK update changes no dependency file; a package update changes `Directory.Packages.props`
  only.
- Restores are not checked against recorded content hashes; the repository relies on nuget.org
  versions being immutable and on the package sources configured on each machine.
- Trimming and AOT warnings surface in every library build, not only when publishing an app.
- Any change to the public API shows up as a diff of the `PublicAPI.*.txt` files.
- Developers and CI need exactly the SDK version of `global.json`.

## Alternatives considered

- **Lock files restored in locked mode** (the choice of 0015). They add content-hash checks to a
  graph that is already deterministic, at the cost of one file per project and script rewritten at
  every SDK update and of locked-mode failures on machines whose SDK or workloads differ slightly.
- **`rollForward` `latestPatch`.** Convenient for developers, but a newer SDK than the one used in CI can
  bring another Emscripten version and other analyzer warnings.
