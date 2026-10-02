# 001: Repository scaffold and managed solution

- Depends on: none
- ADRs: 0002, 0008

## Goal

The repository builds, tests and packs an empty managed solution that follows the AerafalDev house
conventions. `dotnet pack` produces a `Jade` package that already contains `Jade.Interop.dll`.

## Context

- The orchestrator already ran `git init` (branch `main`); `CLAUDE.md` and `design/` exist.
- The reference for house conventions is the sibling repository `../HostFxrSharp`, with
  `../Palforge` as a second example: `.editorconfig`, `.gitignore`, `.gitattributes`, `global.json`
  (MTP test runner), `Directory.Build.props`/`.targets`, `Directory.Packages.props` (CPM +
  transitive pinning, MinVer, SourceLink), `.slnx`, LICENSE (MIT), README, CONTRIBUTING, SECURITY,
  CODE_OF_CONDUCT.
- Local SDK: 10.0.401. HostFxrSharp pins `10.0.112` with `rollForward: latestMinor`. Choose the
  pin for Jade, and say why in the Outcome.
- Metadata: Product `Jade`, author and company `Aerafal`, repository `github.com/AerafalDev/Jade`,
  MIT.

## Scope

- Hygiene files adapted from HostFxrSharp. `.gitignore` additionally covers `artifacts/`, xmake
  outputs (`.xmake/`, `native/build/`) and anything else these tools leave behind.
- `global.json`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`.
  Resolve package versions now with `dotnet add package` or the `nuget` MCP; do not copy versions
  from HostFxrSharp.
- `Jade.slnx` with:
  - `src/Jade.Interop/Jade.Interop.csproj`: `net10.0`, `IsAotCompatible`, `IsPackable=false`, and
    `[assembly: DisableRuntimeMarshalling]`. Leave `Generated/` empty except for a `.gitkeep`.
  - `src/Jade/Jade.csproj`: `PackageId=Jade`. References Jade.Interop privately and packs its
    assembly (and XML docs, if enabled) into the `Jade` package (ADR-0002). No dependency on
    `Jade.Interop` may appear in the nuspec.
  - `tests/Jade.Interop.Tests`: xunit.v3 on MTP, Shouldly and CsCheck, with one trivial test.
- `THIRD-PARTY-NOTICES.md` skeleton (header only; entries come with each library).
- `README.md`: short, stating that the project is in early interop work.
- `scripts/`: decide whether `scripts/Directory.Build.props` is needed (ADR-0008). Validate with a
  throwaway multi-file script under the repo's analyzer settings, then delete it. Record the result
  in the Outcome.
- Update **Commands** in `CLAUDE.md` with the exact build, test and pack commands.

## Out of scope

- CI workflows (002), `src/Jade.Native` (106), anything native (101).

## Acceptance criteria

- [ ] `dotnet build -c Release` on the solution succeeds with 0 warnings.
- [ ] Tests run through Microsoft.Testing.Platform and pass.
- [ ] `dotnet pack -c Release -o artifacts/packages` produces `Jade.<version>.nupkg` containing
      `lib/net10.0/Jade.dll` and `lib/net10.0/Jade.Interop.dll`, with no `Jade.Interop` package
      dependency.
- [ ] The MinVer version shows up in the package name (for example `0.0.0-alpha.0.N` without tags).
- [ ] Settings shared by every project live only in `Directory.Build.props`/`.targets`.

## Verification

Run the build, test and pack commands you recorded in CLAUDE.md. List the nupkg contents (it is a
zip) and show the relevant lines, including the nuspec dependencies.

## Pitfalls

- Packing a private ProjectReference's output needs explicit targets
  (`TargetsForTfmSpecificBuildOutput` or equivalent). Check the package contents; do not trust the
  build output.
- `DisableRuntimeMarshalling` is an assembly-level attribute; put it in one obvious file.
- MinVer needs at least one commit to compute a height; the orchestrator's commit exists.

## Outcome

- Summary: hygiene files adapted from HostFxrSharp (`.editorconfig`, `.gitignore`, `.gitattributes`,
  LICENSE, CODE_OF_CONDUCT, CONTRIBUTING, SECURITY), README, `THIRD-PARTY-NOTICES.md` skeleton,
  `global.json`, `Directory.Build.props`/`.targets`, `Directory.Packages.props` and `Jade.slnx` with
  `src/Jade.Interop` (no code, `DisableRuntimeMarshalling` in `AssemblyInfo.cs`, `Generated/.gitkeep`),
  `src/Jade` (packs Jade.Interop) and `tests/Jade.Interop.Tests` (one test). Commands are in CLAUDE.md.
- Verification (commands and results), on HEAD `96602dd` before this commit:
  - `dotnet build -c Release`: succeeded, 0 errors. In this checkout, 4 warnings, all from SourceLink
    because the local repository has no remote (`Microsoft.Build.Tasks.Git.targets(25,5)`: no remote;
    `Microsoft.SourceLink.Common.targets(56,5)`: source control information not available; once per
    library). In a copy of the working tree with `git remote add origin
    https://github.com/AerafalDev/Jade.git`: `0 Warning(s)`, `0 Error(s)`.
  - `dotnet test -c Release`: runs in MTP mode; `total: 1, failed: 0, succeeded: 1`. With the
    attribute removed, the test failed (exit code 2); the attribute was restored.
  - `dotnet pack -c Release -o artifacts/packages`: produces only `Jade.0.0.0-alpha.0.1.nupkg` and
    `Jade.0.0.0-alpha.0.1.snupkg` (no tag, height 1). The nupkg holds `README.md`,
    `lib/net10.0/Jade.dll`, `lib/net10.0/Jade.xml`, `lib/net10.0/Jade.Interop.dll` and
    `lib/net10.0/Jade.Interop.xml`. The snupkg holds `lib/net10.0/Jade.pdb` and
    `lib/net10.0/Jade.Interop.pdb`. The nuspec's dependencies are `<group targetFramework="net10.0" />`
    (empty: no `Jade.Interop`).
  - Generated AssemblyInfo: Jade and Jade.Interop both carry `0.0.0-alpha.0.1+96602dd…` as
    informational version (assembly version `0.0.0.0`); the test project stays at `1.0.0.0` (no MinVer).
- Decisions taken (and ADRs added):
  - SDK `10.0.401`, `rollForward: latestMinor`, `allowPrerelease: false`. 10.0.401 is the only SDK
    installed here and the band on which ADR-0008's file-based app checks (`#:include`, props import,
    CA2266) were made; earlier bands are unverified for those features. `latestMinor` (as in
    HostFxrSharp) then accepts any newer 10.x SDK: highest installed minor, feature band and patch with
    the same major, at or above the pin (Microsoft Learn, global.json overview).
  - Package versions, resolved with the `nuget` MCP on 2026-10-02: Microsoft.SourceLink.GitHub
    10.0.401, MinVer 8.0.0, xunit.v3 4.0.1, Shouldly 4.3.0, CsCheck 4.9.1.
  - The test project is MTP-only: xunit.v3 4.x brings `xunit.v3.mtp-v2`, and `Microsoft.NET.Test.Sdk`
    and `xunit.runner.visualstudio` only serve VSTest (xunit.net MTP guide), so unlike HostFxrSharp it
    references neither.
  - `Directory.Build.targets` adds MinVer and SourceLink to every project except
    `IsTestProject == true` and `FileBasedProgram == true`, instead of HostFxrSharp's
    `IsPackable != false`: Jade.Interop is not packable but ships inside Jade, so it needs the same
    version and SourceLink data. No referenced package sets `IsTestProject`, so the test project sets it.
  - Package readme: the root `README.md`, added to every packable project by `Directory.Build.targets`
    (as in Palforge). Its links are absolute so they also work on nuget.org.
  - Packing Jade.Interop: `ProjectReference` with `PrivateAssets="all"`, plus target
    `AddInteropToPackage` in `TargetsForTfmSpecificBuildOutput`, which adds the project's
    `ReferenceCopyLocalPaths` to `BuildOutputInPackage`. The target depends on `BuildOnlySettings`: pack
    runs it outside a build, where reference resolution follows `$(BuildingProject)` (false) for
    `FindRelatedFiles`. Without it, only the .dll was packed (no .xml, no .pdb in the snupkg).
  - `GenerateDocumentationFile` is on in both libraries: a public member without `///` docs is a build
    error (CS1591), generated code included.
  - The single test asserts that Jade.Interop carries `DisableRuntimeMarshallingAttribute`.
  - `.editorconfig`: HostFxrSharp's interop-specific suppressions (CA1708, CA1711, CA1720) are not
    carried over since there is no code yet; the explicit-constructor rule is kept as house style; the
    test glob is `tests/*.Tests/**.cs`.
  - `.gitignore`: house file plus `*.binlog`, `.xmake/`, `native/build/` (xmake's default build
    directory is `build`, per `xmake f --help`), `native/vs20*/`, `native/vsxmake20*/`,
    `compile_commands.json` (xmake project generators, per `xmake project --help`) and `.cache/` (clangd).
  - `scripts/Directory.Build.props` is not needed. Checked with a throwaway
    `scripts/scaffold-check/scaffold-check.cs` + `#:include Greeter.cs`, deleted afterwards. Facts on SDK
    10.0.401:
    - The virtual project has `FileBasedProgram=true`, `IsPackable=true`, `PackAsTool=true` and
      `PublishAot=true`, hence `EnableAotAnalyzer` and `EnableTrimAnalyzer`. It inherits
      `TreatWarningsAsErrors`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild` and CPM.
    - Analyzers fail the build: `int.Parse("1")` gives error CA1305, `JsonSerializer.Serialize(...)`
      gives errors IL2026 and IL3050, a missing `#!` gives error CA2266.
    - Before the `FileBasedProgram` exclusion, scripts pulled in MinVer and SourceLink (with SourceLink's
      no-remote warnings). After it, the script has no PackageReference and builds with 0 warnings.
      Arguments pass through: `one "two words" --three` prints `[one, two words, --three]`.
    - `#:property PublishAot=false` removes IL2026/IL3050 and keeps CA1305. A
      `scripts/Directory.Build.props` that imports the root one and sets `PublishAot=false` also wins
      over the file-based default. The root packaging metadata is inert for scripts (only pack reads it).
    The one adjustment scripts needed lives in the root targets; AOT analysis stays on for scripts until
    one needs otherwise.
  - No ADR added.
- Deviations from the brief: "0 warnings" holds once the repository has a remote. Without one,
  SourceLink warns (see Verification); the warnings are not suppressed.
- Follow-ups:
  - User: add the remote (`git remote add origin https://github.com/AerafalDev/Jade.git` or the SSH
    URL) to get a warning-free local build.
  - 002: check which SDK `actions/setup-dotnet` installs from `global.json` with
    `rollForward: latestMinor` (not verified here).
  - 201: Jade.Interop generates XML docs, so generated public members need `///` docs, or the generator
    task decides how CS1591 applies to `*.g.cs`.
  - 101, 201: scripts run AOT and trim analyzers by default. A script that needs reflection (for example
    reflection-based `System.Text.Json`) adds `#:property PublishAot=false`; if several do, move it to
    a `scripts/Directory.Build.props`. `#:package` with CPM is still open (ADR-0008).
  - 106: the house `.gitignore` rule `[Bb]uild/` ignores any folder named `build`, such as
    `src/Jade.Native/build/`; `buildTransitive/` is not affected (checked with `git check-ignore`).
  - New test projects must set `<IsTestProject>true</IsTestProject>`, or they get MinVer and SourceLink.
  - Orchestrator: the status line at the top of CLAUDE.md still says "Nothing builds yet".
