# Task briefs

Each brief is the complete assignment for one Claude Code session. Start a session with:

> Implémente `design/tasks/NNN-....md` en suivant CLAUDE.md.

The kickoff is in French on purpose: the session answers in the language it is addressed in, and
the user works in French. Repository content stays in English.

Rules (see [ADR-0009](../adr/0009-task-workflow.md) and [CLAUDE.md](../../CLAUDE.md)):

- Status lives in [../roadmap.md](../roadmap.md), not in the brief.
- Versions quoted in briefs are snapshots. Re-check them before pinning.
- The task session fills **Outcome** before committing. The next briefs are written from it.

## Template

```markdown
# NNN: Title

- Depends on: NNN, NNN
- ADRs: 000N, 000N

## Goal
One paragraph: what exists when this task is done.

## Context
Facts the session needs and cannot cheaply rediscover.

## Scope
- In scope items.

## Out of scope
- Items explicitly left to other tasks.

## Acceptance criteria
- [ ] Checkable statements.

## Verification
Commands to run and what they must show.

## Pitfalls
Known traps.

## Outcome
_Filled by the task session._
- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
```
