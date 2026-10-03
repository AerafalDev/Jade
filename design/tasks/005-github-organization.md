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
  - the description and topics say what Jade is now, a 2D engine (ADR-0018): for example the
    description "Cross-platform 2D game engine for .NET 10, built on Dawn (WebGPU), SDL3 and
    miniaudio." and the topics `dotnet`, `game-engine`, `2d`, `webgpu`, `sdl3`, `dawn`, `nativeaot`.
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

- Summary: the repository now has two issue forms (bug report, feature request) and a template
  chooser that sends security reports to `SECURITY.md`, a pull request template in the house style,
  `.github/CODEOWNERS`, and 25 labels declared in `.github/labels.yml` and applied by
  `scripts/sync-labels.cs`. A `Labeler` workflow (`actions/labeler@v7`, `pull_request_target`) adds
  `area:` and `platform:` labels from the changed paths, and `dependabot.yml` sets labels from the
  set. On GitHub: the labels are synced, the repository allows merge commits only and deletes
  branches on merge, the wiki is off, the description and topics say Jade is a 2D engine, and the
  ruleset `Protect main` (id 24425291) requires a pull request and the `CI result`, `CodeQL result`
  and `Native result` checks, and blocks force-pushes and deletion. `CONTRIBUTING.md` describes
  issues, pull requests, labels and the protected `main`, and points to `design/`.
- Verification (commands and results):
  - `dotnet build -c Release`: 0 warnings, 0 errors. `dotnet test -c Release`: 101 tests, 87
    passed, 14 skipped (they need a staged jade_native), 0 failed.
  - Style, as CI: `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'`
    exit 0; for each of the six scripts, `dotnet build scripts/<name>.cs` with no warning, then
    `dotnet project convert` and `dotnet format --verify-no-changes`, exit 0.
  - `actionlint` 1.7.12 with shellcheck 0.11.0 on PATH (`uvx --from shellcheck-py`): clean.
    Without shellcheck, `-verbose` reports its rule disabled four times; with it, none.
  - check-jsonschema 0.38.2: `--builtin-schema vendor.github-issue-forms` on both forms,
    `vendor.github-issue-config` on `config.yml`, `vendor.dependabot`, and `vendor.github-workflows`
    on the four workflows: `ok -- validation done`. Its first run caught a real bug:
    `$.body[7].attributes.options[5]: None is not of type 'string'`, the unquoted `Null` backend
    option read as a YAML null; it is quoted now.
  - Forms render: GitHub's blob view of both forms on a pushed branch embeds GitHub's own parse of
    the template, `"isIssueTemplate":true` with `structured: true`, `valid: true`, `errors: []`,
    labels `type: bug` and `type: feature`, and the expected inputs (bug report: What happened,
    Minimal repro, Jade version, Runtime identifier (RID), Operating system, .NET SDK, Native
    backend, GPU and driver, Logs and stack traces; feature request: What are you trying to do,
    What would help, Alternatives). `config.yml` shows `"isIssueTemplate":false`, as expected.
  - Labels, first run of `dotnet scripts/sync-labels.cs --delete`: `rename bug -> type: bug`,
    `enhancement -> type: feature`, `documentation -> type: docs`, 20 `create` lines, `delete`
    `duplicate`, `invalid`, `question`, `accessibility`, `wontfix`, then `25 declared: 20 new, 3
    renamed, 0 updated, 2 unchanged. 5 undeclared, deleted.` Second run:
    `AerafalDev/Jade` / `25 declared: 0 new, 0 renamed, 0 updated, 25 unchanged. 0 undeclared.`
    Before deleting, `gh issue list` and `gh pr list --state all` showed no issue or pull request
    carrying any label.
  - `sync-labels.cs` errors, each with exit code 1 and before any GitHub call: an undeclared label in
    `labeler.yml`, `dependabot.yml` and a form (comma-separated string form) gives one
    `error: <file>:<line> applies "<label>", which .github/labels.yml does not declare.` per label;
    a duplicate alias, a `#` color, a misspelled key and malformed YAML each name the file and line.
    An unknown argument exits 2 with the usage.
  - Repository settings, read back with `gh repo view --json ...`: `mergeCommitAllowed: true`,
    `squashMergeAllowed: false`, `rebaseMergeAllowed: false`, `deleteBranchOnMerge: true`,
    `hasWikiEnabled: false`, description `Cross-platform 2D game engine for .NET 10, built on Dawn
    (WebGPU), SDL3 and miniaudio.`, topics `2d`, `dawn`, `dotnet`, `game-engine`, `nativeaot`,
    `sdl3`, `webgpu`. Projects stay on and Discussions off, untouched.
  - `gh api repos/AerafalDev/Jade/rulesets`: `{"enforcement":"active","id":24425291,"name":"Protect
    main","source_type":"Repository","target":"branch"}`. The ruleset: `bypass_actors: []`,
    `ref_name.include: ["~DEFAULT_BRANCH"]`, rules `deletion`, `non_fast_forward`, `pull_request`
    (`required_approving_review_count: 0`, `allowed_merge_methods: ["merge"]`) and
    `required_status_checks` (`CI result`, `CodeQL result`, `Native result`, each with
    `integration_id: 15368`, the `github-actions` app of every existing check run;
    `strict_required_status_checks_policy: false`). `gh api repos/AerafalDev/Jade/rules/branches/main`
    lists the same four rules for `main`.
  - Ruleset test, draft #31 (a comment in `.github/dependabot.yml` only, base `main`): right after
    opening, `gh pr checks 31 --required` said `no required checks reported` and
    `mergeStateStatus` was `BLOCKED`, while draft #30, whose base has no ruleset, was `CLEAN`. All
    three workflows ran their real jobs (Build and test on three OSes, Code style, CodeQL `actions`
    and `csharp`, `jade_native` on six RIDs, reused) and reported 130 to 144 s after the runs were
    created; `gh pr checks 31 --required` then listed exactly `CI result`, `CodeQL result` and
    `Native result`, all `pass`, and `mergeStateStatus` became `CLEAN`.
  - Labeler test, draft #30 (base: this task's commit, head: one change per rule): run 37144382338,
    job `Label` green, added exactly `area: bindings`, `area: docs site`, `area: engine`,
    `area: generator`, `area: native`, `area: packaging`, `platform: linux`, `platform: windows`
    for `docs/index.md`, `native/linux/Dockerfile`, `scripts/generate-bindings/Naming.cs`,
    `src/Jade.Interop/NulTerminatedUtf8.cs`, `src/Jade.Native/Jade.Native.csproj` and
    `src/Jade/Window.Windows.cs`; no other platform label. See Deviations for its trigger.
  - #30 and #31 are closed, and their branches and `ci/labeler-base` deleted.
- Decisions taken (and ADRs added): no ADR; all of this is repository configuration.
  - Label names are `<group>: <name>` for type, area, platform and status, so the groups sort
    together and `type: docs` cannot be confused with `area: docs site`. `good first issue`,
    `help wanted`, `dependencies` and `github_actions` keep the names GitHub and Dependabot know.
    Each group has one color, except type, whose labels differ. The default `bug`, `enhancement` and
    `documentation` are renamed through `aliases`, which keeps them on their issues. `duplicate`,
    `invalid`, `question`, `wontfix` and `accessibility` were deleted: none was in use, and GitHub's
    close reasons (duplicate, not planned) cover `duplicate`, `invalid` and `wontfix`.
  - `sync-labels.cs` parses YAML with YamlDotNet 18.1.0's representation model, which maps nothing
    to types, so the script keeps `PublishAot` and builds without AOT or trim warnings. Before
    calling GitHub it checks that every label `labeler.yml`, `dependabot.yml` and the issue forms
    apply is declared: the labeler's token cannot create a missing label and Dependabot skips one
    silently. `--dry-run` prints the plan. `Gh.cs` is a copy of `fetch-native/Gh.cs`, since ADR-0010
    gives each script its own helper folder.
  - Dependabot: NuGet updates get `dependencies` and `type: build`, Actions updates `dependencies`,
    `github_actions` and `type: ci`, matching the commit prefixes. Setting `labels` replaces
    Dependabot's defaults, which add an ecosystem label when several ecosystems are configured.
  - Labeler: `pull_request_target` so that pull requests from forks get labels too (a `pull_request`
    run from a fork has a read-only token). Nothing checks out or runs the pull request's code; the
    action lists changed files through the API. Only `pull-requests: write`. No `sync-labels`, so a
    platform label set by hand on a shared-code change stays. No branch filter, and the concurrency
    group is keyed on the pull request number because `github.ref` is the default branch for every
    run. Platform paths follow two conventions: a `native/<platform>/` folder, as `native/linux/`
    already is, and the .NET file suffix (`*.Windows.cs`, `*.Linux.cs`, `*.OSX.cs`/`*.macOS.cs`,
    `*.Android.cs`, `*.iOS.cs`, `*.Browser.cs`); `scripts/build-native/Container.cs` counts as Linux.
  - Ruleset without bypass actor. The orchestrator merges with the owner's account, so an admin
    bypass would apply to exactly the merges the checks are meant to gate. In an emergency, the owner
    can still disable or edit the ruleset in Settings → Rules or through the rulesets API.
  - `strict_required_status_checks_policy: false`: requiring up-to-date branches would make every
    open pull request update after each merge; the checks already run on the merge commit.
  - Required checks name `integration_id` 15368 (GitHub Actions), so no other app or commit status
    can satisfy them.
  - GitHub fills `require_extra_approval_for_unattributed_changes: true` into the pull request rule
    when the payload leaves it out, as the API's response showed. The REST docs read on 2026-10-03
    do not describe it. According to issues and pull requests in other repositories (not GitHub's
    docs), it asks for one approval from someone with write access when a commit's author is not
    linked to a GitHub account, even with zero required approvals. All 64 commits on `main` and the
    commits of #23 and #26 are attributed to `AerafalDev`, so it does not affect the current
    workflow, and it is kept as GitHub set it.
  - Issue forms: blank issues stay enabled, since Discussions are off and a question needs a place;
    the security contact link opens `security/policy`, GitHub's rendering of `SECURITY.md` with its
    report button (private vulnerability reporting is enabled). The RID dropdown lists the twelve
    RIDs ADR-0007 builds plus "Other". "Native backend" is the Dawn graphics backend: D3D12, Vulkan,
    Metal, Null as in `CHANGELOG.md`, WebGPU for the browser, plus "Not related to rendering" and
    "I don't know". An optional "GPU and driver" field follows.
  - Wiki off: it had no page (`git ls-remote https://github.com/AerafalDev/Jade.wiki.git`:
    repository not found). Description and topics as the brief suggested.
  - No `CHANGELOG.md` entry: nothing here changes the packages.
- Deviations from the brief:
  - Since 2025-12-08, `pull_request_target` takes the workflow file and `GITHUB_SHA` from the
    default branch whatever the pull request's base (GitHub changelog, 2025-11-07), and the labeler
    reads its configuration at `GITHUB_SHA`. The committed workflow therefore cannot run before it
    is on `main`. The labeler test used a draft pull request (#30) whose test commit switched the
    trigger to `pull_request`: same action, configuration and labels, other trigger. The ruleset
    test used a second draft (#31) that touches only `.github/`, as the Pitfalls ask.
  - Issue templates are offered on the "New issue" page only from the default branch (GitHub docs:
    "Once these changes are merged into the default branch, the template will be available"). The
    forms were checked through GitHub's own parse in the blob view of a pushed branch instead.
  - Added beyond the brief: the label reference check and `--dry-run` in `sync-labels.cs`, and the
    optional "GPU and driver" field.
- Follow-ups:
  - After this merges: open "New issue" to see the two forms, the blank issue and the security
    link; check that the next pull request gets a `Labeler / Label` check and its labels from
    `pull_request_target`. #23 and #26 get labels on their next push.
  - The orchestrator's `gh pr merge --merge --delete-branch` must now wait for the three checks
    (about 14 s for a docs-only pull request, about 2.5 minutes for #31), for example with
    `gh pr checks <n> --watch --required` first. Auto-merge is off (`allow_auto_merge: false`);
    turning it on would allow `gh pr merge --auto`. That is the user's call.
  - If a commit ever comes from an author email not linked to a GitHub account, the unattributed
    changes rule asks for an approval that the owner cannot give on their own pull request: set
    `require_extra_approval_for_unattributed_changes` to `false` then.
  - CI does not run the label reference check; `dotnet scripts/sync-labels.cs --dry-run` needs
    `gh`. A `--check` mode without GitHub could join the style job.
  - `CONTRIBUTING.md` still opens with "a cross-platform game engine" (not 2D, ADR-0018), and its
    "`var` when the type is apparent" contradicts ADR-0011's `var` everywhere.
  - `scripts/fetch-native/Gh.cs` and `scripts/sync-labels/Gh.cs` are identical; sharing helpers
    between scripts would need an amendment to ADR-0010.
