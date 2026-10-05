# 0017. GitHub repository settings and supply-chain baseline

- Status: Accepted
- Date: 2026-10-05

## Context

Jade is a public repository that will publish native binaries and NuGet packages. Its settings
and workflows are part of the supply chain and must be secure by default. Some settings depend on
CI workflows that do not exist yet, so they are applied in two phases.

## Decision

### Repository

- Public repository, default branch `main`, created and configured with the `gh` CLI.
- Description and topics follow [0002](0002-license-and-public-identity.md). Topics: `dotnet`,
  `csharp`, `game-engine`, `game-development`, `2d`, `cross-platform`, `webassembly`, `android`,
  `ios`.
- Squash merge only. The squash commit takes the pull request title as its subject and has no
  body, so `main` reads as a list of Conventional Commit subjects linked to their pull requests.
- Head branches are deleted automatically after merge; the "update branch" button is enabled.
- Wiki and projects are disabled; documentation lives in `docs/`.
- Labels are defined in `.github/labels.yml` and synchronized from it.
- The social preview image is kept in `docs/assets/`. The REST API has no endpoint to upload it, so
  it is uploaded by hand in the repository settings.
- README badges: CI, CodeQL, OpenSSF Scorecard, license and .NET version; a NuGet badge is added
  at the first publication.

### Security

- `SECURITY.md` and GitHub private vulnerability reporting.
- Dependabot alerts and security updates; `.github/dependabot.yml` covers `nuget` and
  `github-actions`.
- Secret scanning with push protection.
- CodeQL for C#, GitHub Actions, and C/C++ for the shims.
- A ruleset on `main`: pull request required, required status checks, no force push, no deletion,
  linear history. No bypass actors. No approval is required while the project has a single
  maintainer.
- Workflows: the default `GITHUB_TOKEN` is read-only and cannot approve pull requests; each job
  declares its permissions explicitly; actions are pinned to a full commit SHA, enforced by the
  repository's "require SHA pinning" Actions setting and kept up to date by Dependabot.
- Provenance attestations for native binaries and published packages.
- An OpenSSF Scorecard workflow.

### Phasing

| Phase | Settings |
| --- | --- |
| Repository creation | Repository, description, topics, merge settings, wiki/projects, labels (one-off sync), private vulnerability reporting, Dependabot alerts and security updates, secret scanning and push protection, Actions permissions and SHA pinning, ruleset without required checks, license and .NET badges |
| CI setup | `dependabot.yml`, CodeQL, required checks in the ruleset, Scorecard, attestations, label synchronization workflow, labeler, CI/CodeQL/Scorecard badges |

`dependabot.yml` waits for the CI phase because there is nothing to update before the first
projects and workflows exist.

## Consequences

- Every change to `main` goes through a pull request, including the maintainer's own changes.
- The repository settings live outside git; this record and `CLAUDE.md` describe them, and any
  change to them is made with `gh` and recorded here.
- OpenSSF Scorecard checks that expect reviews by a second person score low while the project has
  a single maintainer.

## Applied settings

Changes to the repository settings, as the consequences above require. The decision itself is
unchanged.

- 2026-10-05, repository creation phase: applied as listed above. The ruleset API also enabled
  `require_extra_approval_for_unattributed_changes` on the `main` ruleset by default. It stays
  enabled and is declared explicitly in every ruleset update, since an omitted value is reset to
  `true`. GitHub's documentation ("Available rules for rulesets") limits it to pull requests that
  Copilot opens under its own identity and states that it has no effect when the ruleset requires
  zero approvals, so Dependabot pull requests are not affected.
- 2026-10-05, CI setup phase (roadmap task 3): `dependabot.yml`, the CI, CodeQL (C#, GitHub
  Actions), Scorecard, label synchronization and labeler workflows, and the README badges. The
  `main` ruleset requires the status checks `format`, `build (linux)`, `build (windows)`,
  `build (macos)`, `analyze (csharp)` and `analyze (actions)`, reported by GitHub Actions, on a
  branch that is up to date with `main`. Runners and caching are decided in
  [0022](0022-ci-runners-and-caching.md). Two items of this phase wait for something to apply to:
  provenance attestations come with the native binaries (task 10) and the packages (task 19), and
  CodeQL for C/C++ with the first C shim (task 10).
