# 0018. MSTest as the test framework

- Status: Accepted
- Date: 2026-10-05

## Context

[0015](0015-build-and-packaging-conventions.md) put the tests on Microsoft.Testing.Platform (MTP)
and left the framework open. Jade's tests must eventually run the generated layout tests
(`sizeof`/`offsetof`, thousands of cases) on every target family: desktop under JIT and ideally
NativeAOT, Android, iOS and the browser (roadmap tasks 9 and 18). The repository builds with
`AnalysisLevel` `latest-all`, warnings as errors in CI and central package management.

Verified on 2026-10-05 with SDK `11.0.100-rc.1.26425.128`:

- Latest stable versions on nuget.org: MSTest 4.4.1, xUnit.net v3 (`xunit.v3`) 4.0.1, TUnit
  1.72.16, NUnit 5.0.0.
- The .NET 11 SDK documentation ("What's new in the SDK and tooling for .NET 11") states that
  `dotnet test` on MTP runs test projects on Android and iOS devices, emulators and simulators
  (`--device`), and that the workload templates `androidtest` and `iostest` use MSTest by default.
- The MSTest documentation lists Native AOT ("limited feature set") and browser WebAssembly
  through a custom host ("MTP execution support starts with MSTest 4.4") among supported
  platforms; its reflection-free source generator (`MSTest.SourceGeneration`) left experimental
  status in 4.4.
- An xUnit.net v3 test project for `browser-wasm` fails to build: "xUnit.net v3 test projects must
  build an app host".
- NUnit runs on MTP through the VSTest bridge, and its Native AOT issue (nunit/nunit#5358) is open.
- TUnit is developed in a personal repository (`thomhurst/TUnit`, MIT), whose owner wrote most of
  the commits, and published 18 releases in the last 30 days.
- A scratch comparison ran the same tests under MTP for the four frameworks, 3,000 generated test
  methods in under a second each, and the MSTest and TUnit tests under Node in `browser-wasm` and
  as Native AOT executables. Real Android, iOS and desktop-browser runs were not tried.
- With `ImplicitUsings`, the MSTest packages import `Microsoft.VisualStudio.TestTools.UnitTesting`
  globally. Internal test classes need `[assembly: DiscoverInternals]` and raise CA1812; public
  ones raise CA1515 (test projects are executables) and CS1591.

## Decision

- Tests use MSTest on MTP, referenced through the `MSTest.TestFramework`, `MSTest.TestAdapter` and
  `MSTest.Analyzers` packages, whose versions live in `Directory.Packages.props`. `MSTest.Sdk` is
  not used: as a project SDK, its version would live outside central package management.
- A test project is `tests/<Assembly>.Tests/`, targets `net11.0`, sets `OutputType` `Exe` and
  `EnableMSTestRunner`, and runs with `dotnet test`, which selects MTP through `global.json`.
- Test classes are `internal sealed`. `Properties/AssemblyInfo.cs` declares
  `[assembly: DiscoverInternals]` and method-level parallelization; `.editorconfig` disables CA1812
  under `tests/` only.
- Native AOT test runs use `MSTest.SourceGeneration`, Android and iOS test projects follow the
  workload templates, and browser tests use MSTest's custom host. Each is set up by the task that
  first needs it (9, 15 to 18).

## Consequences

- The device and browser test paths are the ones Microsoft documents and ships templates for,
  which matters most for task 18.
- MSTest releases about monthly and only its latest version is supported, so it is updated with
  the other packages.
- Assertions are less fluent than in TUnit or with assertion libraries; none is added.
- Native AOT runs only cover the test shapes supported by the source generator (plain synchronous
  `[TestMethod]` and `[DataRow]` methods run reflection-free); generated layout tests must stay
  within them.
- The Android, iOS and desktop-browser paths are documented but not yet exercised in this
  repository.

## Alternatives considered

- **TUnit.** Source-generated, Native AOT and WebAssembly capable, with mobile smoke tests in its
  own CI; rejected for its single-maintainer governance and release churn, which the project would
  have to absorb for years.
- **xUnit.net v3.** Mature, no analyzer friction, official Native AOT packages; rejected because it
  cannot build for `browser-wasm` and has no official mobile runner, which blocks task 18.
- **NUnit.** Runs through the VSTest bridge and has no Native AOT support.
