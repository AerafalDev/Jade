# ADR-0009: Task workflow

- Status: Accepted
- Date: 2026-10-02

## Context

The project is built with Claude Code sessions. A long-lived orchestrator session decides and
plans. Implementation happens in separate sessions, one per task, each starting with no context
beyond the repository.

## Decision

- **Orchestrator**: owns `design/` (architecture, roadmap, ADRs, briefs). It does not implement.
- **Task session**: implements exactly one brief, `design/tasks/NNN-kebab-title.md`, then fills the
  brief's Outcome section and marks the task `done` in `design/roadmap.md`, in the same commit as the
  work. One branch per task, named by the `git-workflow` skill.
- **Numbering by phase**: `0xx` foundation, `1xx` native, `2xx` bindings, `3xx` extra libraries,
  `4xx` and above for the engine (later).
- **Status** lives only in `design/roadmap.md`: `todo`, `in-progress`, `done` or `blocked`.
- **Briefs are self-contained**: goal, context, scope, out of scope, deliverables, acceptance
  criteria, verification commands, pitfalls, Outcome. The orchestrator writes detailed briefs only
  for tasks whose inputs are known. Later tasks stay one-line roadmap entries until their
  prerequisites report back.
- `docs/` is reserved for the future public documentation site (fumadocs, as in the other AerafalDev
  repositories). That is why internal design lives in `design/`.

## Consequences

- A task session can start from a clean context with: "Implement design/tasks/NNN-....md".
- The Outcome sections are the orchestrator's input for writing the next briefs. They must state
  facts with commands and results, not impressions.
