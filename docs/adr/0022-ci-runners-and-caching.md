# 0022. CI runners and caching

- Status: Accepted
- Date: 2026-10-05

## Context

The managed CI builds, tests and packs the solution on Linux, Windows and macOS, checks the
formatting, runs CodeQL and OpenSSF Scorecard, and automates the repository (label
synchronization, pull request labeling). It needs runners and a caching policy. The native builds
of roadmap task 10 have their own runner and caching needs and are decided there.

Verified on 2026-10-05:

- GitHub documentation ("GitHub-hosted runners" reference): standard GitHub-hosted runners are free
  and unlimited for public repositories. Linux and Windows x64 runners have 4 vCPUs and 16 GB of
  memory; macOS arm64 runners have 3 vCPUs (M1) and 7 GB. `ubuntu-slim` is a single-CPU runner
  that runs the job in an unprivileged container, with a 15-minute job timeout.
- `actions/runner-images` README: `ubuntu-latest` is `ubuntu-24.04`, `windows-latest` is
  `windows-2025` and `macos-latest` is `macos-26` (arm64); `ubuntu-26.04` is already available, so
  `ubuntu-latest` will move to it on GitHub's schedule.
- GitHub documentation recommends self-hosted runners for private repositories only, because pull
  requests from forks of a public repository can run code on them.
- `ossf/scorecard-action` 2.4.4 is a Docker action, and its README requires an Ubuntu hosted
  runner when results are published.
- `actions/setup-dotnet` 6.0.0 installs the exact `global.json` version for prerelease SDKs. Its
  `cache` input requires `packages.lock.json` files, which
  [0021](0021-build-and-packaging-conventions.md) rules out.
- A cold restore of the solution (empty NuGet global-packages folder, local machine) took about
  5 seconds and downloaded 196 MB, mostly the Roslyn and ILLink packages. Each job also downloads
  the SDK, which is not preinstalled on the runners. On the first CI run (pull request #3), setting
  up the SDK took 5 to 15 seconds and the restore 4 to 15 seconds per job, Windows being the
  slowest.
- GitHub's dependency caching reference: workflow runs can restore caches from the default branch,
  and the pull request runs of forks can read the caches of their base branch; caches written by a
  `pull_request` run are scoped to its merge ref.

## Decision

- The managed CI runs on GitHub-hosted standard runners with pinned image labels: `ubuntu-24.04`,
  `windows-2025` and `macos-26` (arm64). `-latest` labels are not used.
- Jobs that only call the GitHub API (label synchronization, pull request labeling) run on
  `ubuntu-slim`. Scorecard and CodeQL run on `ubuntu-24.04`.
- Check names never contain a runner label: the build jobs are `build (linux)`, `build (windows)`
  and `build (macos)`, so moving to a newer image does not change the checks required by the
  ruleset.
- No self-hosted or larger runners.
- The managed CI uses no cache: no `actions/cache`, no `setup-dotnet` cache, and CodeQL's dependency
  caching is turned off. Every job downloads the SDK and the packages.
- Caching is reconsidered when the SDK setup and restore of a job take more than a minute, or with
  the native builds of task 10.

## Consequences

- An image change is a visible commit in the workflows rather than a silent switch. When GitHub
  deprecates a pinned image, the labels are bumped in one pull request, which is also the moment to
  re-check the toolchain facts of `CLAUDE.md`.
- Every job pays the SDK download. In exchange, there is no cache to poison, invalidate or keep
  within the repository's cache storage, and a run never depends on what an earlier run left.
- Managed code is built and tested on x64 (Linux, Windows) and arm64 (macOS). Linux and Windows
  arm64 runners come with the native matrix of task 10.
- `ubuntu-slim` jobs must stay short and cannot run Docker actions.

## Alternatives considered

- **`-latest` labels.** No upkeep, but image migrations land without a commit and can break CI on
  GitHub's schedule.
- **Larger runners.** Paid, and not needed for the managed build.
- **Self-hosted runners.** Would run code from fork pull requests of a public repository.
- **NuGet cache with `actions/cache`**, keyed on `global.json`, `Directory.Packages.props` and the
  project files. Saves a few seconds per job for about 200 MB of cache per operating system, and
  adds a cache that pull requests read from the default branch.
- **NuGet and SDK cache.** Saves the SDK download too, at the cost of more keys to maintain and a
  larger cache.
