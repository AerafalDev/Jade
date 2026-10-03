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

- Summary: a pull request now gets its verdict in about 2 minutes, unless it changes a package
  recipe (a cold package build stays long), and in 14 seconds when it only touches docs. Each
  workflow starts with a `Changes` job that skips the heavy jobs of a docs-only pull request, and
  ends with a `CI result`, `CodeQL result` or `Native result` job that always reports. `native.yml`
  reuses the libraries of an earlier run built from the same native inputs and runs every check on
  them. It builds when no such run exists, on scheduled and manual runs, and on the first push to
  `main` of each set of inputs. The Linux image and xmake's own binary come from the Actions cache,
  osx-x64 cross-compiles on Apple silicon, and the package cache key carries each RID's xmake
  configuration instead of the hash of `Rids.cs`.
- Verification (commands and results):
  - Before/after, from `gh api repos/AerafalDev/Jade/actions/runs/<id>/jobs` (the same data as
    `gh run view --json jobs`, plus `created_at` for the queue). Verdict: from the first run's
    creation to the last job's completion. Job time: sum of the jobs' run times. The "after" pull
    requests target `ci/measure-base`, a copy of this branch whose triggers also accept that branch
    (see Deviations), after its push runs built and cached everything.

    | Pull request kind | Before | After |
    | --- | --- | --- |
    | Docs only (`design/architecture.md`) | #14: 112 s (CI 112 s, CodeQL 100 s, Native skipped), 409 s of jobs | #18: 14 s, 21 s of jobs (three `Changes` and three result jobs) |
    | C# only (`AssemblyTests.cs`) | #16: 316 s (Native 316 s, osx-x64 last at 313 s; CI 83 s, CodeQL 129 s), 1501 s of jobs | #19: 112 s (CI 112 s, Native 105 s, CodeQL 101 s), 815 s of jobs |
    | Native change (`native/shims/jade_native.c`) | #17: 262 s (Native 262 s, osx-x64 257 s; CI 104 s, CodeQL 135 s), 1504 s of jobs | #20: 140 s (CodeQL 140 s, Native 135 s, CI 111 s), 1042 s of jobs |

    Native jobs, before → after. C# only (reused libraries): linux-x64 186 → 54 s, linux-arm64
    171 → 59 s, win-x64 148 → 86 s, win-arm64 130 → 76 s, osx-x64 313 → 66 s, osx-arm64 159 → 81 s.
    Native change (build with every cache warm): linux-x64 201 → 111 s, linux-arm64 190 → 95 s,
    win-x64 140 → 119 s, win-arm64 147 → 121 s, osx-x64 257 → 83 s, osx-arm64 169 → 87 s. On C# and
    native pull requests, CI's `Code style` (92 to 101 s) and CodeQL's C# analysis (86 to 127 s) are
    now the longest jobs.
  - Reuse: every #19 job logged `Reusing <rid> from run 37096561719 (ci/measure-base, f02bd97),
    built from the same native inputs`, then passed the glibc check (`GLIBC_2.27`), the architecture
    and deployment target check, the smoke check, and on three RIDs the bindings drift check and the
    tests. #20 logged `Building <rid>: no earlier run built it from these native inputs`, restored the
    package cache by prefix and staged in 1.3 to 23.3 s; the input ID was the same on the three
    OSes. The lookup's jq filter, run locally against the real `jade-native-linux-x64` artifacts,
    picks the newest same-repository run for a pull request (37094927067), the newest `main` run for
    a push to `main` (37093573668), and nothing for an unknown branch.
  - Cold build with the new keys (`ci/measure-base`, run 37095209884, all green): linux-x64 1190 s,
    linux-arm64 722 s, win-x64 1353 s, win-arm64 1083 s, osx-x64 618 s, osx-arm64 541 s per job.
    The next push (run 37096561719, native.yml edited, every cache hit): 69 to 134 s per job.
  - Linux image: `docker save | zstd` gives 683 MB (x64) and 648 MB (arm64) cache entries; exporting
    and saving took 15 to 18 s. Restoring then loading took 32 and 38 s on linux-x64, 30 and 31 s on
    linux-arm64 (runs 37096561719 and 37097261718), against 2 to 3 minutes to build it. ghcr.io was
    not measured with our image (publishing a package was declined). Pulls of public ghcr.io images
    on the same runners, two runs each: 516 MB compressed in 27 to 28 s (x64) and 16 to 17 s
    (arm64), 1579 MB in 56 to 59 s (x64) and 42 to 54 s (arm64), so about 36 s (x64) and 25 s (arm64)
    estimated for our ~800 MB of gzip layers (`docker save` size with the containerd store locally).
    The runners use Docker 28.0.4 with overlay2 and have `zstd`.
  - osx-x64, 11 Intel runs (`macos-26-intel`, 37083256168 to 37094927067): queue 3 to 36 s (median
    5 s), `Setup xmake` 177 to 361 s (built from source every time), cold builds 859 to 1641 s, warm
    jobs 257 to 313 s, about 200 s of which was `Setup xmake`. The queue was never the problem; on
    #10 the job was last because its cold build ran 1641 s after 357 s of `Setup xmake`. Cross-built
    on `macos-26` (4 runs): queue 6 to 9 s, cold build 425 s, `Setup xmake` 111 s uncached and under
    3 s cached, warm jobs 71 and 83 s, 66 s when reusing. The library is `x86_64` (`lipo -archs`),
    `minos 12.0`, 11564 KiB as the Intel build of 103, and the smoke check passes in an x64 .NET
    10.0.12 under Rosetta 2 with the `Apple Paravirtual device (Metal)` adapter.
  - Package cache: `dotnet scripts/build-native.cs --rid <rid> --print-config` prints, for example,
    `-p macosx -a x86_64 -m release --target_minver=12.0`. A comment edit in `Rids.cs` leaves the
    six desktop configurations identical (same SHA-256); changing the macOS minimum changes only the
    two osx lines.
  - `actionlint` 1.7.12: clean, also with shellcheck 0.11.0 (`uvx --from shellcheck-py`), which first
    reported SC2010 on the Windows SDK lookup inherited from 103 (fixed with a glob).
  - Scripts: `dotnet build scripts/build-native.cs` and `scripts/smoke-native.cs`, 0 warnings; the
    style check of **Commands** passes on both. `dotnet scripts/smoke-native.cs` passes on the host;
    `--rid osx-x64` there fails with `osx-x64 cannot be loaded by this process (linux-x64)`.
- Decisions taken (and ADRs added): no ADR; everything here is workflow configuration.
  - Docs only means every file under `design/` or ending in `.md`, except `README.md` for `ci.yml`:
    the packages embed it (`PackageReadmeFile`), so it is an input of the Pack step. The `Changes`
    job diffs the merge commit against its first parent with `--no-renames`, so a file moved into
    `design/` still counts its old path. Pushes always run the jobs; `native.yml` keeps its
    `paths-ignore` on push only, since nothing gates a push.
  - Result jobs: a skipped matrix job reports under its unexpanded name (`Build and test (${{
    matrix.os }})` on #18), so a required check could never name it. `CI result`, `CodeQL result`
    and `Native result` run unless the run is cancelled and fail when a job they need failed or was
    cancelled; they pass on docs-only pull requests.
  - Native reuse is keyed on content rather than on a run: the first 16 hex digits of the SHA-256
    of `git ls-tree HEAD -- native scripts/build-native.cs scripts/build-native
    .github/workflows/native.yml`, the git object IDs of the native inputs. Once a job's
    library passes the glibc, architecture, deployment target and smoke checks, the job uploads a
    marker artifact `native-inputs-<rid>-<id>` (its `versions.json`). A later job looks the marker up
    through the artifacts API (`actions: read`), takes the newest unexpired one from a run of this
    repository (never a fork), and downloads `jade-native-<rid>` and its symbols from that run with
    `gh run download`. A failed lookup or download builds instead, with a warning. The bindings and
    tests run after the marker, so a commit that only fixes C# reuses its own pull request's
    libraries.
  - Which runs may be reused: a pull request, any branch of this repository; a push, only its own
    branch, so that `main`'s package cache, the one pull requests restore, gets each new set of
    inputs built once; scheduled and manual runs always build. A new weekly schedule (Monday
    01:00 UTC) builds `main` from source, so a runner image or toolchain change still shows up within
    a week, and its cache restores keep the entries clear of the 7-day eviction.
  - Reused libraries are uploaded again under the usual names, so every successful run carries the
    libraries it checked and `fetch-native.cs` keeps working unchanged.
  - Linux image: Actions cache rather than ghcr.io. Restore plus load measured 30 to 38 s, within the
    25 to 36 s estimated for ghcr.io; the cache needs no package, no `packages: write` and works
    the same for forks. Keyed on the RID and the hash of the Dockerfile, `global.json` and
    `Container.cs` (what the image tag depends on); `docker save` is streamed through `zstd` both ways
    so the image never sits on disk twice (the arm64 runners have about 14 GB).
  - osx-x64 cross-compiles on `macos-26` (arm64): its cold build is two to four times faster, and
    its warm jobs (71 to 83 s) are no longer than the Intel ones without their `Setup xmake` (75 to
    97 s). xmake configures CMake packages for another macOS architecture through its Apple
    path (`CMAKE_OSX_ARCHITECTURES=x86_64`, `CMAKE_SYSTEM_PROCESSOR=x86_64`, no empty sysroot;
    `modules/package/tools/cmake.lua`), unlike the generic cross path that broke 103. Sources: SDL
    3.4.16 derives its CPU from `CMAKE_OSX_ARCHITECTURES` (`cmake/sdlcpu.cmake`); Dawn runs no compiled
    tool during the build (Python generators; protobuf, tests and fuzzers off in our recipe); Abseil
    adds its x86 flags per architecture with `-Xarch_x86_64` (`AbseilConfigureCopts.cmake`);
    miniaudio is one C file. The tests still run on osx-arm64 only, as before; osx-x64 gets its smoke
    check in an x64 runtime under Rosetta 2, which the `macos-26` arm64 image installs
    (`install-rosetta.sh` in its packer template). A new `lipo -archs` check fails if the library
    is not the RID's architecture.
  - `smoke-native.cs` compares the RID with `RuntimeInformation.ProcessArchitecture`:
    `OSArchitecture` reports arm64 under Rosetta (`sysctl.proc_translated` in
    `pal_runtimeinformation.c`, release/10.0).
  - `setup-xmake` caches its build of xmake (`actions-cache-folder: native/build/setup-xmake`). The
    two macOS jobs share its key; the second one logs `Failed to save: Unable to reserve cache`,
    which does not fail the step.
  - The package cache key replaces the hash of `Rids.cs` with a hash of `build-native.cs
    --print-config`, the RID's whole xmake configuration minus the build directory.
- Checks and their inputs, for pull requests. Pushes to `main` still run every job (`native.yml`
  still skips docs-only pushes); only the native build itself follows the last row there too:

  | Check | Runs when the pull request changes | Before |
  | --- | --- | --- |
  | Build and test, Pack (3 OS), Code style | anything outside `design/` and `*.md`, or `README.md` | every pull request |
  | CodeQL C# and Actions analyses | anything outside `design/` and `*.md` | every pull request |
  | Script compilation on 6 runners, glibc baseline, macOS deployment target (and now architecture), smoke check, bindings drift and tests against jade_native (linux-x64, win-x64, osx-arm64) | anything outside `design/` and `*.md` | same |
  | Building jade_native | `native/`, `scripts/build-native.cs`, `scripts/build-native/`, `native.yml`, compared with every earlier run instead of with the base; plus weekly, manual, and the first `main` push of each set of inputs | anything outside `design/` and `*.md` |

  Nothing formerly checked goes unchecked: `design/` and `.md` files other than `README.md` are read
  by no build, test, analysis or native step (only `README.md` is referenced by
  `Directory.Build.props` and `.targets`); every check on the library runs on each pull request,
  whether the library is new or reused; and a build from source still happens for every change to
  its inputs. What a pull request without native changes no longer tests is whether the natives still
  build with that day's runner images; the weekly build covers that.
- Deviations from the brief:
  - Reuse is found by native inputs, not by the merge base's run: any run that built the same inputs
    is exact, whatever its commit, and the fallback when none exists is to build, with a log line.
  - `README.md` counts as code for `ci.yml` (see Decisions).
  - The bindings drift check and the tests still run on linux-x64, win-x64 and osx-arm64, as before
    this task, not on all six RIDs; the six get the smoke check. "Every desktop RID" was read as
    "every RID that ran them", given "coverage stays the same".
  - Added beyond the brief: the weekly native build, the explicit macOS architecture check, the
    setup-xmake cache, and the SC2010 fix.
  - Measuring the reuse path needs a run of the new workflow on the base branch, so the "after" pull
    requests (#18 to #20) target `ci/measure-base`: this branch plus a commit adding that branch to
    the three workflows' triggers (measurement only) and a workflow timing the ghcr.io pulls. Its two
    push runs did the cold and the warm builds.
  - ghcr.io was estimated from public images, not measured with ours (see Verification).
- Follow-ups:
  - When branch protection comes, require `CI result`, `CodeQL result` and `Native result`, not the
    matrix jobs. Whether a code scanning merge protection rule waits for an analysis that a
    docs-only pull request skips was not checked.
  - The first `main` run after this merges builds every RID from scratch (new key format; caches
    saved by pull requests are scoped to them): about 20 minutes, as run 37095209884.
  - `design/architecture.md` still says the native artifact is "cached by hash of native/**,
    scripts/build-native.cs and scripts/build-native/**" and does not mention reuse or the weekly
    build.
  - On C# pull requests the longest jobs are now `Code style` (about 100 s, mostly converting and
    formatting each script) and the CodeQL C# analysis (about 2 minutes).
  - linux-x64's cold build took 1108 s in run 37095209884 against 700 s in 37092056428 with the same
    recipes; nothing in this task touches that path, so likely runner variance. Worth watching.
  - `fetch-native.cs --branch <branch>` takes the latest successful run, which has no artifacts when
    the branch's pull request is docs only.
  - The measurement pull requests (#14, #16 to #20) are closed and their branches, with
    `ci/measure-base` and its caches, deleted.
