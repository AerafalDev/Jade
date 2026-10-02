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

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
