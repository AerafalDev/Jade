# 0001. Record architecture decisions

- Status: Accepted
- Date: 2026-10-05

## Context

Jade is built over many independent work sessions. Nothing carries over between them except the
repository itself, so every important decision, its reasons and its consequences must be written
down where the next session will find it.

## Decision

- Important or hard-to-reverse decisions are recorded as Architecture Decision Records in
  `docs/adr/`, one decision per file, named `NNNN-kebab-case-title.md` with a four-digit sequence.
- New records start from [`template.md`](template.md) and are listed in the [index](README.md).
- A record is written once the maintainer has validated the decision. Its status is then
  `Accepted`.
- An accepted record is not rewritten. Changing a decision means writing a new record that
  supersedes it; the old one only gets its status updated to `Superseded by NNNN`.
- Local, reversible decisions do not need a record.

## Consequences

- The reasoning behind the architecture can be recovered without the people or sessions that
  produced it.
- `CLAUDE.md` and `docs/architecture.md` summarise the current state and link to the records
  rather than repeating them.
