# ADR-0008: Scripts are C# file-based apps

- Status: Accepted
- Date: 2026-10-02

## Context

The repository needs automation: native build wrapper, binding generator, fetching CI artifacts,
upstream bumps. It must work identically on Windows, macOS and Linux, in the same language as the
project.

## Decision

- All automation lives in `scripts/` as .NET 10 file-based apps.
- A single-file script is `scripts/<name>.cs` (kebab-case).
- A multi-file script is a kebab-case folder `scripts/<name>/` with entry point
  `scripts/<name>/<name>.cs`, so promoting a script to a folder does not rename its entry point.
  Helper files use PascalCase type names and are pulled in with `#:include`.
- Every entry point starts with `#!/usr/bin/env dotnet`.
- Invocation is `dotnet scripts/<name>.cs [args]`.
- NuGet dependencies use `#:package`. How that combines with Central Package Management is settled
  by the first task that needs a package.
- No bash or PowerShell scripts. CI workflow steps call the C# scripts; inline shell stays limited
  to one-liners.

Checked on SDK 10.0.401 (2026-10-02):

- `#:include` works.
- `dotnet <file>.cs args` passes `args` through.
- File-based apps import `Directory.Build.props` from parent directories.
- A multi-file (`#:include`) entry point without a leading `#!` triggers CA2266.

## Consequences

- Scripts inherit the repo's analyzers and warnings-as-errors. Task 001 decides whether
  `scripts/Directory.Build.props` adjusts that, for example by keeping analyzers while dropping
  library-only settings such as packaging metadata.
- Scripts have no test project of their own. Their outputs are verified by the tasks that use them.
