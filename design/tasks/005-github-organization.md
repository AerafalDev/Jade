# 005: GitHub organization: community files, labels, ruleset

- Depends on: 004
- ADRs: 0009

## Goal

The GitHub repository is organized and self-explanatory:

- issue and pull request templates;
- code owners;
- a declarative label set kept in sync by a script;
- automatic labels on pull requests;
- repository settings that match how the project works;
- a ruleset that protects `main`.

AerafalDev's other repositories have none of this yet (checked on 2026-10-03), so Jade sets the
reference.

## Context

- Today `.github/` holds `dependabot.yml` and three workflows: `ci.yml`, `codeql.yml` and
  `native.yml`. After 004, each workflow ends with an aggregate job: `CI result`, `CodeQL result`
  or `Native result`.
- The repository has GitHub's default labels plus `accessibility`. Dependabot adds `dependencies`
  and `github_actions` on its own when it opens a PR (see Palforge's labels).
- Repository settings on 2026-10-03:
  - merge commits, squash and rebase are all allowed, and branches are not deleted on merge;
  - wiki and projects are on, discussions are off;
  - there are no topics and no homepage.

  The project merges with merge commits only (`git-workflow` skill), and the orchestrator merges
  with `gh pr merge --merge --delete-branch`.
- House style for pull request text (`git-workflow` skill): short and plain, first person is fine,
  no `## Summary`/`## Test plan` headings, no checkboxes, and a last line `Tested: ...`. A pull
  request template must guide toward that, for example with HTML comments, not impose headings.
- Decided with the user on 2026-10-03: protect `main` with a ruleset. Changes go through a pull
  request; the required checks are `CI result`, `Native result` and `CodeQL result`; force-push and
  deletion are blocked. The orchestrator keeps merging pull requests itself.
- `CODE_OF_CONDUCT.md`, `CONTRIBUTING.md` and `SECURITY.md` exist at the root (from 001).

## Scope

- `.github/CODEOWNERS`: `@AerafalDev` for everything. Finer ownership only if it adds something.
- `.github/PULL_REQUEST_TEMPLATE.md`, following the house style above.
- `.github/ISSUE_TEMPLATE/`: issue forms (`.yml`) for bugs and feature requests, plus a
  `config.yml` that points security reports to `SECURITY.md`. For bugs, collect RID, OS, .NET SDK,
  Jade version and native backend.
- Labels:
  - a declarative file (for example `.github/labels.yml`) covering:
    - type (bug, feature, docs, ci, build, chore, performance);
    - area (native, bindings, generator, packaging, engine, docs site);
    - platform (windows, linux, macos, android, ios, browser);
    - status (blocked, needs-decision);
    - `good first issue`, `help wanted`, `dependencies`, `github_actions`;
  - `scripts/sync-labels.cs`, which applies the file through `gh`: creates, updates, and deletes
    only with a flag. Run it once against the repository;
  - `dependabot.yml` labels aligned with the set.
- Automatic area and platform labels on pull requests from changed paths (`actions/labeler` or
  equivalent; pin it like the other actions).
- Repository settings through `gh repo edit` and `gh api`:
  - merge commits only, and delete branches on merge;
  - wiki off unless the user wants it;
  - topics (for example `dotnet`, `game-engine`, `webgpu`, `sdl3`, `dawn`, `nativeaot`).
- The ruleset on `main` as decided above, through `gh api`.
- Update `CONTRIBUTING.md` where it describes issues, pull requests or labels, and point it to
  `design/` for how work is organized.

## Out of scope

- A documentation site, release automation (107), Discussions or Projects setup.

## Acceptance criteria

- [ ] Templates render on GitHub. Check the issue forms through the "New issue" page after pushing;
      the session may push its branch and open a draft PR to see them.
- [ ] `scripts/sync-labels.cs` is idempotent: a second run changes nothing (show its output).
- [ ] The ruleset exists and its required checks resolve. Show `gh api repos/AerafalDev/Jade/rulesets`
      and a test PR whose merge waits for the three checks.
- [ ] The labeler adds the expected labels on that test PR.
- [ ] The style job and `actionlint` pass.

## Verification

The session may push its branch, open draft PRs, run `scripts/sync-labels.cs` and change repository
settings and rulesets as listed in Scope: the user asked for them. It must not merge, and must not
change anything outside this list without asking.

## Pitfalls

- A ruleset that requires checks blocks every merge until they report. A docs-only PR gets its
  verdict in about 14 s (004). Check that a PR touching only `.github/` still produces all three
  verdicts.
- Rulesets cannot be bypassed by the owner unless a bypass actor is set. Decide whether to keep an
  admin bypass for emergencies, and record the choice in the Outcome.
- Issue forms are YAML with a strict schema. Validate them (`check-jsonschema` has a built-in
  schema for GitHub issue forms; check the exact name).

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
