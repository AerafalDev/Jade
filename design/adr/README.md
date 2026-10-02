# Architecture decision records

One file per decision: `NNNN-kebab-title.md`. Never rewrite an `Accepted` ADR. Write a new one
that supersedes it and set the old one's status to `Superseded by ADR-NNNN`. Task sessions may add
ADRs with status `Proposed`; the orchestrator, together with the user, accepts or rejects them.

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-webgpu-backend-dawn.md) | Dawn on every native RID, emdawnwebgpu on the web | Accepted |
| [0002](0002-package-layout.md) | `Jade` references `Jade.Native`; Jade.Interop is packed into `Jade` | Accepted |
| [0003](0003-single-combined-native-library.md) | One combined `jade_native` library per RID | Accepted |
| [0004](0004-native-build-with-xmake.md) | xmake drives the native build, with our own pinned packages | Accepted |
| [0005](0005-in-house-binding-generator.md) | In-house binding generator in `scripts/generate-bindings/` | Accepted |
| [0006](0006-binding-api-shape.md) | .NET-style public bindings, methods on handles, span overloads | Accepted |
| [0007](0007-target-platforms.md) | Desktop, mobile and browser RIDs | Accepted |
| [0008](0008-scripts-as-file-based-apps.md) | Scripts are C# file-based apps | Accepted |
| [0009](0009-task-workflow.md) | Orchestrator, one session per task, briefs in `design/tasks/` | Accepted |

## Template

```markdown
# ADR-NNNN: Title

- Status: Proposed | Accepted | Superseded by ADR-NNNN
- Date: YYYY-MM-DD

## Context

## Decision

## Consequences
```
