# ADR-0010: A script's entry point sits next to its helper folder

- Status: Proposed
- Date: 2026-10-02
- Supersedes: the multi-file layout in ADR-0008 (the rest of ADR-0008 stands)

## Context

ADR-0008 puts a multi-file script's entry point inside its folder, `scripts/<name>/<name>.cs`, yet
also says scripts are invoked as `dotnet scripts/<name>.cs`. ADR-0004 and the briefs for 103 and
105 name `scripts/build-native.cs` as the native build entry point. Task 101 first wrote
`scripts/build-native.cs` as one file of about 300 lines and eleven types, then split it at the
user's request.

## Decision

- Every script's entry point is `scripts/<name>.cs` (kebab-case), single-file or not.
- A multi-file script keeps its helper files in `scripts/<name>/`, next to the entry point, and
  pulls each one in with `#:include <name>/<Type>.cs`.
- One class, struct, record or enum per file, named after the type. The entry point holds only
  top-level statements.
- Every `internal` or `public` type and member of a script has `///` docs.

Checked on SDK 10.0.401 with `scripts/build-native.cs` and `scripts/smoke-native.cs`: `#:include`
accepts a path into the sibling folder, and the scripts build with 0 warnings under the repository
analyzers.

## Consequences

- Promoting a single-file script to a multi-file one never renames its entry point or changes its
  command.
- `#:include` lines list every helper file, so adding a type means adding a line.
- Scripts do not set `GenerateDocumentationFile`, so the compiler does not enforce the doc rule
  (CS1591); review does.
- ADR-0008's status becomes "Superseded by ADR-0010" for its layout bullet once this ADR is
  accepted. The 201 brief still names `scripts/generate-bindings/generate-bindings.cs`.
