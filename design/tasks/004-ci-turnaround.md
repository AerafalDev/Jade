# 004: Faster CI turnaround

- Depends on: 103
- ADRs: 0004, 0013

## Goal

A pull request gets its verdict in minutes. Docs-only changes run nothing heavy, native jobs build
only when native inputs change, and slow runners no longer gate every merge. Coverage stays the
same: each check still runs whenever its inputs change.

## Context

Measured on PRs #9 to #12 (2026-10-03):

- `ci.yml` and `codeql.yml` run on every PR, docs-only ones included (1 to 2 minutes of jobs).
- `native.yml` has `paths-ignore: ['design/**', '**.md']`. On `pull_request`, filters apply to the
  whole PR diff, not the last commit, so a code PR plus a docs commit still runs everything.
- Native jobs take 2 to 5 minutes with a warm cache and 10 to 25 minutes cold. The osx-x64 job runs
  on Intel macOS runners, which queue the longest: on #10 it was the last job by a wide margin.
- From 103:
  - the Linux baseline container image (about 3 GB) is rebuilt in every Linux job;
  - any edit to `scripts/build-native/Rids.cs` invalidates the package cache of every RID.
- The bindings drift check and the native tests run inside the `native.yml` build jobs. They need
  `artifacts/native/<rid>/`, which `scripts/fetch-native.cs` can download from the latest successful
  run on `main` (checked by the orchestrator on 2026-10-03, run 37092056428).
- No branch protection or required checks are configured. Keep the design compatible with adding
  required checks later: a skipped workflow must not leave a required check pending forever.

## Scope

- Docs-only PRs (`design/**`, `**.md`): `ci.yml` and `codeql.yml` skip, or run a trivial job.
- `native.yml`:
  - build natives only when native inputs change: `native/**`, `scripts/build-native.cs`,
    `scripts/build-native/**` and the workflow itself;
  - otherwise download `main`'s latest artifacts and still run the bindings drift check and the
    native tests on every desktop RID. Changes to the generator or `src/Jade.Interop` must still be
    checked against real binaries.
- Linux container image: cache it, keyed by the hash of `native/linux/Dockerfile`, with a registry
  (ghcr.io) or `docker save`/`load` through the actions cache. Choose by measured restore time.
- osx-x64: measure queue and run time over several runs. Compare with cross-compiling osx-x64 on an
  arm64 macOS runner (Dawn, SDL3 and miniaudio must support it; check in their sources). Keep the
  faster option that still produces a verified x86_64 library. If tests can only run on Intel, say
  where they run.
- Narrow what invalidates the package cache: a `Rids.cs` edit should only invalidate the RIDs it
  changes, if that is cheap.

## Out of scope

- Removing any check, or weakening what a check verifies.

## Acceptance criteria

- [ ] A before/after timing table for three PR kinds (docs only, C# only, native change), from real
      runs on draft PRs.
- [ ] For each check, the Outcome states the inputs that trigger it and confirms that nothing
      formerly checked now goes unchecked.
- [ ] `actionlint` is clean.

## Verification

Draft PRs pushed by the session for each PR kind; the timings come from `gh run view --json jobs`.
The session may push its branch and open draft PRs, but not merge.

## Pitfalls

- Artifacts of the latest `main` run may come from a commit older than the PR's base. Fetch by the
  merge base's run when possible, and fall back with a clear log line.
- Downloading artifacts from another run needs `actions: read` permission on the token.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
