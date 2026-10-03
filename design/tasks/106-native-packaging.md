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

- Summary: `src/Jade.Native` packs `lib/` of every RID staged under `artifacts/native/` into
  `runtimes/<rid>/native/`, with `THIRD-PARTY-NOTICES.md` and `buildTransitive/Jade.Native.targets`
  (placeholders for browser-wasm and iOS; desktop needs nothing). Packing reports the size per RID,
  warns about a missing desktop RID (JADENATIVE001) and fails when the `.nupkg` exceeds 240 MB
  (JADENATIVE002). `Jade` depends on `Jade.Native` at `[<its own version>]`. `tests/Jade.PackageTests`,
  a console app outside the solution, restores `Jade` from `artifacts/packages` and calls jade_native
  through a hand-written import and through Jade.Interop's SDL bindings; `scripts/test-package.cs`
  packs, checks the nuspec range, runs it with `dotnet run`, publishes it with NativeAOT for the host
  RID and runs the binary. Verified on linux-x64 only, with the six desktop libraries of native.yml run
  37092056428 (commit `9a7393c`; `native/` has not changed since).
- Verification (commands and results), CachyOS host, .NET SDK 10.0.401:
  - `dotnet scripts/fetch-native.cs --rid linux-x64 --rid linux-arm64 --rid win-x64 --rid win-arm64
    --rid osx-x64 --rid osx-arm64`: six RIDs in 16.5 s.
  - `dotnet build -c Release`: 0 warnings, 0 errors. `SDL_VIDEO_DRIVER=dummy dotnet test -c Release`:
    101/101.
  - `dotnet pack -c Release -o artifacts/packages` (no warning) prints the size per RID: linux-arm64
    10.9 MB, linux-x64 11.9 MB, osx-arm64 10.8 MB, osx-x64 11.8 MB, win-arm64 10.4 MB, win-x64 10.1 MB
    (66.0 MB uncompressed), then `Jade.Native: package 24.2 MB, limit 240 MB` (24,165,691 bytes).
    `Jade.Native.0.0.0-alpha.0.14.nupkg` contains `Jade.Native.nuspec`, `README.md`,
    `THIRD-PARTY-NOTICES.md`, `buildTransitive/Jade.Native.targets`,
    `runtimes/linux-arm64/native/libjade_native.so`, `runtimes/linux-x64/native/libjade_native.so`,
    `runtimes/osx-arm64/native/libjade_native.dylib`, `runtimes/osx-x64/native/libjade_native.dylib`,
    `runtimes/win-arm64/native/jade_native.dll`, `runtimes/win-x64/native/jade_native.dll` and NuGet's
    `_rels/.rels`, `[Content_Types].xml` and `package/services/metadata/core-properties/*.psmdcp`. Its
    nuspec has no dependency group. `Jade.0.0.0-alpha.0.14.nupkg` still holds `README.md` and
    `lib/net10.0/` `Jade.dll`, `Jade.xml`, `Jade.Interop.dll`, `Jade.Interop.xml` (no
    `Jade.Native.dll`), and its nuspec has `<dependency id="Jade.Native" version="[0.0.0-alpha.0.14]"
    exclude="Build,Analyzers" />`. The `.snupkg` holds `Jade.pdb` and `Jade.Interop.pdb`.
  - `dotnet scripts/test-package.cs`, from an empty `artifacts/packages` and `artifacts/package-tests`:
    exit code 0 in 10.8 s. Output: `Jade.nuspec depends on Jade.Native [0.0.0-alpha.0.14]`; with
    `dotnet run`, `Runtime: JIT, native search directories
    .../tests/Jade.PackageTests/bin/Release/net10.0/runtimes/linux-x64/native/:...`,
    `jade_native_abi_version: 1`, `SDL_GetVersion: 3.4.16`; after `Generating native code`,
    `Runtime: NativeAOT`, `jade_native_abi_version: 1`, `SDL_GetVersion: 3.4.16`.
  - Build output without a RID: `runtimes/<rid>/native/` for all six RIDs, and the `.deps.json` lists
    them as `runtimeTargets` of `Jade.Native`. NativeAOT publish output: `Jade.PackageTests` (ELF, NEEDED
    only `libm.so.6`, `libc.so.6`, `ld-linux-x86-64.so.2`), `Jade.PackageTests.dbg`,
    `Jade.PackageTests.xml` and `libjade_native.so` next to the binary.
  - The consumer's `obj/Jade.PackageTests.csproj.nuget.g.targets` imports
    `jade.native/0.0.0-alpha.0.14/buildTransitive/Jade.Native.targets`, despite the `exclude="Build"`
    that pack writes by default.
  - Stale cache: with `artifacts/native/linux-x64/lib/libjade_native.so` replaced by a local build of the
    same commit (SHA-256 `04f83fae...` instead of CI's `7c6b6fa9...`), `test-package.cs` put `04f83fae...`
    in the extracted package, the build output and the NativeAOT publish folder. After restoring CI's
    library and repacking, a plain `dotnet build` of the consumer kept `04f83fae...` while the `.nupkg`
    held `7c6b6fa9...`: NuGet does not re-extract a version it has, which is what the script's cleanup
    is for.
  - Size guard: `dotnet pack -c Release -o artifacts/packages -p:JadeNativeMaxPackageSize=20000000`
    prints `package 24.2 MB, limit 20 MB` and fails, exit code 1, with `error JADENATIVE002:
    .../Jade.Native.0.0.0-alpha.0.14.nupkg is 24165555 bytes, over the 20000000-byte limit kept below
    nuget.org's 250 MB. Split Jade.Native by platform family as ADR-0002 plans
    (design/adr/0002-package-layout.md).` The threshold was lowered through a global property, so the
    project file never changed; the next pack reports `limit 240 MB` again. `dotnet pack src/Jade.Native`
    without `-o` finds the package in `bin/Release/` too.
  - Missing RIDs (win-arm64 and osx-x64 moved away, then back): `warning JADENATIVE001: Jade.Native has
    no jade_native for win-arm64, osx-x64. ...`; with `MSBuildTreatWarningsAsErrors=true` it is `error
    JADENATIVE001`, exit code 1. With no natives at all and `-p:JadeNativeRequiredRids=` (the new
    `ci.yml` pack step), pack succeeds under `MSBuildTreatWarningsAsErrors=true` and `Jade.Native` holds
    only the readme, the notices and the targets.
  - The nuspec check of `test-package.cs` works: with `PinJadeNativeVersion` hooked to a nonexistent
    target, pack wrote `version="0.0.0-alpha.0.14"` and `test-package.cs --no-pack` failed with `error:
    Jade.nuspec depends on Jade.Native 0.0.0-alpha.0.14, not [0.0.0-alpha.0.14].` (restored afterwards).
  - Without `JadePackageVersion`, `dotnet run --project tests/Jade.PackageTests` fails before restore
    with an error that names the property and `scripts/test-package.cs`.
  - Style: `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'`, the converted
    `test-package` script and `JadePackageVersion=0.0.0-alpha.0.14 dotnet format
    tests/Jade.PackageTests/Jade.PackageTests.csproj --verify-no-changes` report nothing (a swapped
    `using` pair in the consumer makes the last one fail with `IMPORTS`, so it does check it). All five
    scripts build. `actionlint` passes on the three workflows.
- Decisions taken (and ADRs added):
  - No ADR. `Jade.Native` is a `Microsoft.NET.Sdk` project with `IncludeBuildOutput=false`: its empty
    assembly is compiled and never packed, which avoids pinning `Microsoft.Build.NoTargets` in
    `global.json`. `IncludeSymbols=false` (otherwise NU5017, empty symbols package) and
    `SuppressDependenciesWhenPacking=true` (otherwise NU5128, a net10.0 group without `lib/`) are both
    needed, as checked by overriding each.
  - RIDs are discovered from `artifacts/native/*/lib/*`, so any staged RID ships, and
    `JadeNativeRequiredRids` (the six desktop RIDs) lists what a release needs. A missing one is a
    warning locally and an error wherever `MSBuildTreatWarningsAsErrors=true`, as `ci.yml` and
    `native.yml` set it, so a release workflow that does the same needs no extra switch. `-warnAsMessage:JADENATIVE001` does not override
    `MSBuildTreatWarningsAsErrors` (tried), hence the empty `JadeNativeRequiredRids` in `ci.yml`, whose
    build job has no natives. That step now runs under bash, so that PowerShell never parses the empty
    `-p:` value (pwsh is not installed here to check its behaviour).
  - The guard measures the `.nupkg`, the file nuget.org limits, after `Pack`. The limit is 240,000,000
    bytes, below 250 MB whether nuget.org counts decimal or binary megabytes (not verified which).
    MSBuild property functions cannot read a file size (`System.IO.FileInfo` is refused, MSB4185), so a
    small `RoslynCodeTaskFactory` task does. Per-RID sizes are the uncompressed library sizes.
  - Exact version: restore drops a `ProjectReference` with `ReferenceOutputAssembly="false"` (read in
    the decompiled `NuGet.Build.Tasks.GetRestoreProjectReferencesTask`), so `Jade` references
    `Jade.Native` normally with `Private="false"`, which keeps `Jade.Native.dll` out of its output and out
    of `AddInteropToPackage`; `Jade.dll` does not reference it. Pack writes a project reference as a
    minimum version, so target `PinJadeNativeVersion` wraps `ProjectVersion` of NuGet's internal
    `_ProjectReferencesWithVersions` item in brackets after `_GetProjectReferenceVersions`. Because both
    names are internal, `test-package.cs` fails when the range is not exact. MinVer already hooks
    `GetPackageVersionDependsOn`, so the version is Jade.Native's MinVer version.
  - The default `PrivateAssets` stays: the `exclude="Build,Analyzers"` on the dependency does not stop
    the `buildTransitive` import (checked above).
  - The consumer is a console app, not an xunit project: it must publish and run as NativeAOT.
    `IsTestProject=true` keeps MinVer and SourceLink away. It is not in `Jade.slnx`, since restoring needs
    the packed version. Its `nuget.config` maps `Jade` and `Jade.*` to the local feed only (nuget.org
    serves the ILCompiler and runtime packs) and gives it its own packages folder,
    `artifacts/package-tests/packages/`, so local builds never enter the global cache. The version comes
    from `JadePackageVersion` as `VersionOverride` (CPM stays on); `test-package.cs` reads it with `dotnet
    msbuild src/Jade/Jade.csproj -t:MinVer -getProperty:PackageVersion`, then deletes the extracted
    `jade`/`jade.native` folders of that version and the consumer's `bin` and `obj` before running.
  - The consumer prints JIT or NativeAOT from the host's `NATIVE_DLL_SEARCH_DIRECTORIES`:
    `RuntimeFeature.IsDynamicCodeSupported` and `IsDynamicCodeCompiled` are both false under the JIT too,
    because `PublishAot=true` writes the `IsDynamicCodeSupported=false` switch into the runtimeconfig.
  - `tests/Jade.Interop.Tests` keeps copying the staged library from `artifacts/native/<rid>/`: taking it
    from the package would need a pack before every `dotnet test` and a version handshake like the
    consumer's, and `Jade.PackageTests` already covers the package path. Its comment says so.
  - Windows' `d3dcompiler_47.dll` (103's follow-up) is not shipped: `build-native.cs` does not stage it,
    and Dawn falls back to the system copy that Windows 10 and later carry (not verified here, see
    Follow-ups).
  - `Jade.Native` keeps the repository's `MIT` license expression; the bundled licenses travel in
    `THIRD-PARTY-NOTICES.md` inside the package (see Follow-ups).
- Deviations from the brief:
  - `tests/Jade.PackageTests` is a console app driven by the new `scripts/test-package.cs` rather than a
    test project; it also calls `SDL_GetVersion` through the generated bindings, so the package's
    `Jade.Interop.dll` is what loads jade_native.
  - `ci.yml`'s pack step changed (`-p:JadeNativeRequiredRids=`, `shell: bash`): without it the CI build
    job would fail on JADENATIVE001. `CONTRIBUTING.md`'s pack line now names both packages.
  - Only linux-x64 ran the consumer. Windows and macOS (executable name, dylib next to the NativeAOT
    binary) are untested.
- Follow-ups:
  - 205: replace the hand-written import, run `scripts/test-package.cs` in CI on the desktop runners
    after `fetch-native` (or the native.yml job), and give `tests/Jade.PackageTests` a style check in CI:
    the style job only covers the solution and the scripts.
  - 104 and 105: add your RIDs to `JadeNativeRequiredRids`, fill the placeholders in
    `buildTransitive/Jade.Native.targets`, and mind that everything in `artifacts/native/<rid>/lib/`
    ships (wasm JavaScript libraries included).
  - 107: pack with every RID staged and `MSBuildTreatWarningsAsErrors=true`, then push only
    `artifacts/packages/*.nupkg` and the `Jade` `.snupkg`. Decide whether `Jade.Native`'s license should
    stay `MIT` or become an SPDX expression of the bundled licenses (zlib, BSD-3-Clause, Apache-2.0,
    MIT-0 or public domain, GPL-3.0 with the GCC Runtime Library Exception on Linux) or a
    `PackageLicenseFile`.
  - 206 on Windows: Dawn compiles shaders with FXC there; confirm that the system `d3dcompiler_47.dll`
    works, or ship it from the Windows SDK with its notice.
  - 004: if `ci.yml` gets the natives, drop `-p:JadeNativeRequiredRids=` and run
    `scripts/test-package.cs` there.
  - Orchestrator: `design/architecture.md` does not list `scripts/test-package.cs` or
    `tests/Jade.PackageTests`.
