# 0037. GitHub repository settings and supply-chain baseline

- Status: Accepted
- Date: 2026-10-05

## Context

[0017](0017-github-repository-baseline.md) set the repository settings and the supply-chain
baseline of this public repository, which will publish native binaries and NuGet packages, and
applied them in two phases. Among them was an OpenSSF Scorecard workflow, published for a README
badge and uploaded to code scanning. The maintainer chose to drop it: with a single maintainer, the
checks that expect reviews by a second person keep the score low, and the rest of what it measures
is either enforced by this baseline already or a practice the project does not adopt. This record
restates the whole baseline without Scorecard, so that it can be read on its own, and supersedes
0017.

Verified on 2026-10-05:

- The `main` ruleset (id `24485772`) requires `format`, `build (linux)`, `build (windows)`,
  `build (macos)`, `analyze (csharp)` and `analyze (actions)`; Scorecard's `analysis` job is not
  among them (`gh api`).
- `scorecard.yml` ran on pushes to `main` and weekly, never on pull requests. Its 18 analyses left
  9 open code scanning alerts, one per failing check: Branch-Protection, CI-Tests,
  CII-Best-Practices, Code-Review, Fuzzing, Maintained, Pinned-Dependencies, SAST and
  Security-Policy (`gh api`).

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
- README badges: CI, CodeQL, license and .NET version; a NuGet badge is added at the first
  publication.

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
- No OpenSSF Scorecard workflow.

### Phasing

| Phase | Settings |
| --- | --- |
| Repository creation | Repository, description, topics, merge settings, wiki/projects, labels (one-off sync), private vulnerability reporting, Dependabot alerts and security updates, secret scanning and push protection, Actions permissions and SHA pinning, ruleset without required checks, license and .NET badges |
| CI setup | `dependabot.yml`, CodeQL, required checks in the ruleset, attestations, label synchronization workflow, labeler, CI and CodeQL badges |

## Consequences

- Every change to `main` goes through a pull request, including the maintainer's own changes.
- The repository settings live outside git; this record and `CLAUDE.md` describe them, and any
  change to them is made with `gh` and recorded here.
- No external score is published for the repository. The rules of this baseline (pinned actions,
  explicit token permissions, the ruleset, Dependabot, CodeQL) stay enforced by the settings and
  workflows above.
- The badge URL of the Scorecard API keeps serving the last published score; nothing links to it
  any more.
- [0022](0022-ci-runners-and-caching.md) still names Scorecard among the jobs on `ubuntu-24.04`;
  that rule has no workflow left to apply to, and 0022 is not rewritten
  ([0001](0001-record-architecture-decisions.md)).

## Applied settings

Changes to the repository settings, as the consequences above require. Entries before this
record's date were made under 0017.

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
- 2026-10-05, this record: `scorecard.yml` and the README's Scorecard badge are removed, and the
  9 open Scorecard alerts are dismissed as "won't fix" with a comment that points here. The ruleset
  is unchanged.
- 2026-10-07, roadmap task 10 ([0038](0038-native-ci.md)): the `main` ruleset also requires
  `natives` (the native workflow, green when its builds are skipped) and `analyze (c-cpp)` (CodeQL
  for the C shim), both from GitHub Actions; the other rules are unchanged. Provenance attestations
  now cover the native artifacts.

## Alternatives considered

- **Keeping Scorecard**: an external, comparable score and a weekly re-check of the settings, at
  the cost of a workflow whose low review score says nothing while the project has one maintainer.
- **Keeping the workflow without publishing**: results in code scanning only, still for a score
  the project does not act on.
