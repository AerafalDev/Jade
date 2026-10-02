# ADR-0011: C# coding conventions

- Status: Accepted
- Date: 2026-10-02

## Context

Task sessions wrote code with `this.` qualification, private fields without `_` and private static
fields without `s_`. The repository's `.editorconfig`, inherited from HostFxrSharp, has no naming
rules and sets its style rules to `suggestion` or `none`, so `EnforceCodeStyleInBuild` stops nothing.
The other AerafalDev repositories are not consistent either: static fields use `s_` in Palforge but
PascalCase in HostFxrSharp and Cytrus.

## Decision

The reference is the dotnet/runtime C# coding style
(`docs/coding-guidelines/coding-style.md` in github.com/dotnet/runtime, rules 1 to 20), with these
house deviations:

- `var` wherever possible, which replaces runtime rule 10. This is the existing `.editorconfig`
  choice.
- File-scoped namespaces, as in every AerafalDev repository.
- No primary constructors (`IDE0290` off, as today), which makes runtime rule 20 moot.
- No interop exception to rule 12: generated constants follow ADR-0006 naming, so they are
  PascalCase like any other constant.

What this means in practice:

| Element | Rule |
| --- | --- |
| Private and internal instance fields | `_camelCase`, `readonly` where possible |
| Private and internal static fields | `s_camelCase`, written `static readonly`, never `readonly static` |
| Thread-static fields | `t_camelCase` |
| Constants (fields and locals) | `PascalCase` |
| Public fields (rare) | `PascalCase`, no prefix |
| Types, methods, local functions, properties, events | `PascalCase`; interfaces `IName`, type parameters `TName` |
| Parameters and locals | `camelCase` |
| `this.` | Never, unless the code cannot compile without it |
| Accessibility | Always explicit and first in the modifier list |
| `using` directives | At the top, outside the namespace, `System.*` first, then alphabetical |
| Braces | Allman. Braces may be omitted only when every block of the `if`/`else` chain is a single line (runtime rule 18) |
| Blank lines | Never two in a row |
| Types | Internal and private types are `sealed` or `static` unless derivation is needed |
| Field placement | Fields first in a type declaration |
| Keywords | Language keywords instead of BCL type names (`int`, `string`) |
| Names in strings | `nameof(...)` instead of string literals |
| Non-ASCII characters | `\uXXXX` escapes, not literal characters |

The rules apply to all C# in the repository: `src/`, `tests/`, `scripts/` and `samples/`. They also
cover generated bindings, which are public API (ADR-0006). `src/Jade.Interop/Generated/` therefore
opts out of Roslyn's generated-code exemption, so the generator must emit conforming code.

Enforcement is mechanical. Every rule that has an analyzer is configured in `.editorconfig` at
`warning` severity, which `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` turn into build
errors. Rules without an analyzer, such as field placement, are checked in review.

## Consequences

- A violation fails the local build and CI. Sessions cannot drift silently anymore.
- Task 003 converts the existing code and wires the rules. Later tasks inherit them through
  `.editorconfig` and CLAUDE.md.
- The other AerafalDev repositories may adopt the same `.editorconfig` later. That is the user's
  call, outside this repository.
