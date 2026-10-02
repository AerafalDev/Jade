# 002: CI baseline

- Depends on: 001
- ADRs: none specific

## Goal

GitHub Actions builds and tests the managed solution on Linux, Windows and macOS. CodeQL and
Dependabot are configured as in the other AerafalDev repositories.

## Context

- References: `../HostFxrSharp/.github` and `../Palforge/.github` (ci.yml, codeql.yml, dependabot,
  and their conventions for SDK setup from `global.json`, caching and concurrency).
- Native workflows come in 103, the release pipeline in 107. This task only covers the managed
  solution.
- From 001: `global.json` pins SDK `10.0.401` with `rollForward: latestMinor`. Check, in the
  `actions/setup-dotnet` docs or sources, which SDK it installs from that file; do not assume.
- From 001: SourceLink warns when the repository has no remote. CI checkouts have one; a warning in
  CI is a real problem, not this one.

## Scope

- `.github/workflows/ci.yml`: build (Release, warnings as errors), test, and pack as a smoke step
  on `ubuntu-latest`, `windows-latest` and `macos-latest`. SDK version comes from `global.json`.
- `.github/workflows/codeql.yml` for C#.
- `.github/dependabot.yml` for `nuget` and `github-actions`, aligned with the other repositories.
- Pin actions as the other repositories do.

## Out of scope

- Native builds, publishing, docs site.

## Acceptance criteria

- [ ] Workflows mirror the house style of HostFxrSharp/Palforge, with deviations explained.
- [ ] Every command CI runs is the one recorded in CLAUDE.md and passes locally.

## Verification

- Run the CI commands locally on Linux.
- Validate workflow syntax with `actionlint` if available. It is not installed: ask the user rather
  than installing it.
- The workflows cannot run before the user pushes. Say so in the Outcome; do not claim CI is green.

## Pitfalls

- CodeQL for C# needs a build mode that works with `TreatWarningsAsErrors` and the MTP runner.
  Check what HostFxrSharp does.

## Outcome

- Summary: `.github/workflows/ci.yml` builds, tests and packs on `ubuntu-latest`, `windows-latest` and
  `macos-latest` with the commands from CLAUDE.md and uploads the TRX results. `.github/workflows/codeql.yml`
  analyzes C# (manual build) and the workflows themselves (`actions`). `.github/dependabot.yml` covers
  `nuget` and `github-actions`. All three follow HostFxrSharp's files; the differences are listed below.
- Verification (commands and results):
  - Fresh `git clone --depth 1` of `origin/main` (`18174d8`, shallow like `actions/checkout`'s default),
    with `GITHUB_ACTIONS=true` and `MSBuildTreatWarningsAsErrors=true` as in the CI job:
    - `dotnet build -c Release`: `0 Warning(s)`, `0 Error(s)`, exit 0.
    - `dotnet test -c Release --results-directory TestResults --report-xunit-trx
      --report-xunit-trx-filename test-results.trx`: `total: 1, failed: 0, succeeded: 1`, exit 0, writes
      `TestResults/test-results.trx`.
    - `dotnet pack -c Release -o artifacts/packages`: exit 0, `Jade.0.0.0-alpha.0.nupkg` and `.snupkg`.
  - In this checkout: `dotnet build -c Release --no-incremental` gives `0 Warning(s)`, `0 Error(s)` with and
    without `MSBuildTreatWarningsAsErrors=true`; `dotnet test -c Release` passes (1/1); `dotnet pack -c
    Release -o artifacts/packages` produces `Jade.0.0.0-alpha.0.2.nupkg` and `.snupkg`.
  - `MSBuildTreatWarningsAsErrors` as an environment variable, in a shallow clone with the remote
    removed: without it, `4 Warning(s)` (SourceLink, no remote) and exit 0; with it, the same no-remote
    message from `Microsoft.Build.Tasks.Git.targets(25,5)` is reported as `error`, and exit 1.
  - `uvx check-jsonschema --builtin-schema vendor.github-workflows` on both workflows and
    `--builtin-schema vendor.dependabot` on `dependabot.yml`: `ok -- validation done`.
  - `actionlint .github/workflows/ci.yml .github/workflows/codeql.yml` (1.7.12, installed by the user):
    no findings, exit 0. shellcheck is not installed, so actionlint did not lint the `run:` scripts; each is
    a single `dotnet` command.
  - Not verified: the workflows have not run on GitHub. They first run when the branch is pushed; CI is
    not claimed green on Windows or macOS.
- Decisions taken (and ADRs added):
  - SDK from `global.json` through `actions/setup-dotnet@v6` `global-json-file: global.json` (HostFxrSharp
    passes `dotnet-version: 10.0.x`). From the v6.0.0 sources: `getVersionFromGlobalJson`
    (`src/setup-dotnet.ts`) turns `10.0.401` with `rollForward: latestMinor` into `10`;
    `DotnetVersionResolver.createChannelArgument` (`src/installer.ts`) turns `10` into channel `10.0`; with
    no `dotnet-quality`, `install-dotnet.sh` downloads `aka.ms/dotnet/10.0/dotnet-sdk-<os>-<arch>`, the
    channel's current GA SDK. On 2026-10-02 that link redirects to `dotnet-sdk-10.0.401`, and the 10.0
    `releases.json` reports `latest-sdk` 10.0.401. CI therefore gets the newest 10.0 SDK, which `latestMinor`
    accepts, and the same one as the local machine today. `global.json`'s `test.runner` is not read by
    setup-dotnet; the SDK reads it from the checkout.
  - CI runs the CLAUDE.md commands as recorded (`dotnet build -c Release`, `dotnet test -c Release`,
    `dotnet pack -c Release -o artifacts/packages`) instead of the house `dotnet restore` + `--no-restore` +
    `--no-build` sequence, so a local run matches CI. `dotnet test` and `dotnet pack` re-run an incremental
    build first.
  - `MSBuildTreatWarningsAsErrors: true` at job level in `ci.yml`. `TreatWarningsAsErrors` in
    `Directory.Build.props` does not cover MSBuild task warnings (SourceLink, MinVer, SDK): 001 saw
    SourceLink warnings with a successful build. In CI any such warning now fails the job. It stays out of
    `Directory.Build.props` so that a local clone without a remote still builds (001's decision). CodeQL's
    build does not set it; CI already enforces it.
  - Pack runs on all three OSes as a smoke step and its output is not uploaded. The release pipeline (107)
    owns publishing.
  - No NuGet cache, as in HostFxrSharp and Palforge (neither caches). setup-dotnet's `cache: true` needs a
    `packages.lock.json` or a `cache-dependency-path`: `restoreCache` (`src/cache-restore.ts`) throws
    "Dependencies lock file is not found" otherwise, and the repo has no lock files.
  - CodeQL C# `build-mode: manual` with `dotnet build -c Release`, as in HostFxrSharp, whose setup matches
    Jade's (`TreatWarningsAsErrors`, `AnalysisMode Recommended`, xunit.v3 on MTP): its CodeQL run
    36909771972 built under the tracer with `0 Warning(s)`, `0 Error(s)` and succeeded. That run and PR run
    36908527009 also emit a `##[warning]Cannot build an overlay(-base) database because build-mode is set
    to "manual" instead of "none"` annotation. It comes from `validateOverlayDatabaseMode`
    (`src/config-utils.ts`, codeql-action v4.38.2), which falls back to a full database for any traced
    language whose build mode is not `none`. Jade will show the same annotation. It is not a build
    warning, and the analysis still runs on a full database.
  - Actions pinned to their major tag as in the house files. On 2026-10-02 these are the latest majors
    (`gh release list`): `actions/checkout@v7` (v7.0.1), `actions/setup-dotnet@v6` (v6.0.0),
    `actions/upload-artifact@v7` (v7.0.1), `github/codeql-action/*@v4` (v4.38.2).
  - `dependabot.yml` is HostFxrSharp's file without the `npm` entry (no docs site yet).
  - No ADR added.
- Deviations from the brief:
  - The CI test step adds report-only options to the recorded command (`--results-directory TestResults
    --report-xunit-trx --report-xunit-trx-filename test-results.trx`) to keep the house TRX upload. Both
    forms were run locally (see Verification).
  - The brief points to the reference repositories for "SDK setup from `global.json`" and caching. Neither
    reads `global.json` (both use `dotnet-version: 10.0.x`) and neither caches. Jade follows the brief for
    the SDK and the house files for caching.
- Follow-ups:
  - User: push the branch and check that CI is green on the three OSes and that CodeQL succeeds. The only
    expected annotation is CodeQL's overlay fallback.
  - Orchestrator: decide, for all AerafalDev repositories at once, whether C# CodeQL should move to
    `build-mode: none` (overlay databases, no annotation, build-less extraction) or stay on `manual`.
  - Orchestrator: actionlint 1.7.12 is now installed locally (shellcheck is not). CLAUDE.md's
    environment facts do not list it yet.
  - 107: `actions/checkout` is shallow by default, so MinVer computes `0.0.0-alpha.0` (no history, no
    warning). The release pipeline needs `fetch-depth: 0`, as in HostFxrSharp's `publish.yml`.
  - 103: native jobs that want a NuGet cache need lock files (`RestorePackagesWithLockFile`) or a
    `cache-dependency-path` for setup-dotnet's `cache` input. They can reuse the
    `MSBuildTreatWarningsAsErrors` job variable.
