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

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
