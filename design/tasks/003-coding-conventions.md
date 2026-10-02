# 003: Enforce the C# coding conventions and clean up existing code

- Depends on: 102, 201
- ADRs: 0006, 0011

## Goal

Every rule in ADR-0011 that has an analyzer fails the build when violated, in libraries, tests,
scripts and generated bindings. All existing C# in the repository conforms, and CI checks it.

## Context

- ADR-0011 holds the rules: the dotnet/runtime coding style plus house deviations (`var`
  everywhere, file-scoped namespaces, no primary constructors).
- The current `.editorconfig` comes from HostFxrSharp. It has no naming rules, its `.NET code style`
  entries are `suggestion` or `none`, and it marks `*.g.cs` as generated code. Keep the ReSharper
  and Rider entries; they do not affect the build.
- `Directory.Build.props` already sets `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild` and
  `AnalysisMode Recommended`. Scripts inherit them (ADR-0008); CI does not build scripts today.
- 102 and 201 are done when this starts. The generator (`scripts/generate-bindings.cs`) and its
  output under `src/Jade.Interop/Generated/` exist. So do `scripts/build-native.cs` and
  `scripts/smoke-native.cs`.
- The `dotnet:csharp-refactoring` skill covers behavior-preserving renames verified by build and
  tests. Use it.

## Scope

- `.editorconfig`:
  - Naming rules (`dotnet_naming_rule.*`, `IDE1006`) for every row of the ADR-0011 table: `_`,
    `s_` and `t_` prefixes, PascalCase constants, `I` and `T` prefixes, camelCase parameters and
    locals.
  - Style rules at `warning`. Expected rule IDs, each to be verified on Microsoft Learn before use:
    - `IDE0003`/`IDE0009` for `this.`;
    - `IDE0040` and `IDE0036` for accessibility modifiers and modifier order;
    - `IDE0065` for using placement, with `dotnet_sort_system_directives_first`;
    - `IDE0161` for file-scoped namespaces;
    - `IDE0007`/`IDE0008` for `var`;
    - `IDE0049` for keywords over BCL type names;
    - `IDE0011` for braces (see below);
    - `IDE0044` for readonly fields;
    - `IDE0005` for unnecessary usings (check what it needs to run at build);
    - `IDE0055` for formatting, with Allman `csharp_new_line_*` options;
    - multiple blank lines;
    - `CA1852` for sealed internal types.
  - Runtime rule 18 (braces) has no exact analyzer. Pick the closest `csharp_prefer_braces` value and
    explain the choice.
  - Remove `generated_code = true` for `src/Jade.Interop/Generated/`, or override it there, so the
    rules apply to generated bindings. Check the result on a `.g.cs` file, since Roslyn also
    detects generated code by file name.
- Fix all existing code: `src/`, `tests/`, `scripts/` and `samples/` if present. Use `dotnet format`
  where it applies, then review by hand. Behavior must not change; the build and tests prove it.
- Generated bindings: fix the **generator's emitter** so its output conforms, then regenerate. Never
  edit `*.g.cs` by hand. Regenerating twice must still give byte-identical output.
- CI: make violations fail CI for everything, scripts included. That means building each script
  entry point in `ci.yml` (check how `dotnet build <file>.cs` behaves on SDK 10.0.401), and adding
  `dotnet format --verify-no-changes` if it catches something the build does not. Justify either
  way.
- CLAUDE.md: replace nothing. Add one bullet under **Conventions → .NET** pointing to ADR-0011,
  with the five rules sessions break most often (`this.`, `_`, `s_`, explicit accessibility,
  file-scoped namespaces).

## Out of scope

- Other AerafalDev repositories.
- Any behavior change or API redesign. A public rename that ADR-0011 forces (for example a public
  field) is listed in the Outcome before doing it.

## Acceptance criteria

- [ ] A throwaway file containing each violation (`this.x`, private field `x`, private static
      `_x`, block-scoped namespace, `using` inside the namespace, missing accessibility, public
      `Int32`) fails the build with the expected rule IDs. Quote the IDs in the Outcome, then delete
      the file.
- [ ] The same check passes for a throwaway script under `scripts/` and for a `.g.cs` file under
      `src/Jade.Interop/Generated/`.
- [ ] `dotnet build -c Release`, `dotnet test -c Release` and every script build pass with
      0 warnings.
- [ ] The generator's output is deterministic and conformant after regeneration.
- [ ] CI covers scripts. Say plainly that CI itself has not run before push.

## Verification

Run the throwaway violation files (show the error lines), then the build, tests, script builds,
generator run (twice, compare hashes) and `dotnet format --verify-no-changes` if adopted.

## Pitfalls

- `IDE` rules only fail the build with `EnforceCodeStyleInBuild` and a `warning` (or higher)
  severity. Some of them also need `GenerateDocumentationFile`; check which.
- Naming rules are matched in order of specificity. Declare the `s_` rule (static) before the
  `_` rule (instance), or static fields will match the instance rule.
- Renaming `private` fields is safe. Renaming anything `public` or `internal` that other files use
  must go through a real rename (refactoring skill), not text replacement.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
