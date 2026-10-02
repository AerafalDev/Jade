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
- Remove `src/Jade.Interop/Generated/.gitkeep`, which is redundant now that 201 generates files
  there.
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

- Summary: `.editorconfig` sets every ADR-0011 rule that has an analyzer to `warning`: IDE0003, IDE0005,
  IDE0007, IDE0011, IDE0036, IDE0040, IDE0044, IDE0049, IDE0055, IDE0065, IDE0161, IDE2000, IDE2001 and CA1852.
  IDE1006 naming rules cover `_camelCase` and `s_camelCase` non-public fields, PascalCase constants (fields
  and locals), visible fields, types and members, `I` interfaces, `T` type parameters and camelCase
  parameters and locals. Both `Generated/` folders opt out of Roslyn's generated-code exemption.
  `GenerateDocumentationFile` moved from the two library projects to `Directory.Build.props`, because
  IDE0005 needs it everywhere. Hand-written fixes: 2 private fields in `NulTerminatedUtf8`, 35 private and
  static fields in `scripts/generate-bindings/`, `Staged` to `s_staged` in `Sdl3Tests`, one `var`. Everything
  else already conformed, including `build-native` and `smoke-native`. The emitter names pinned locals
  `<param>Ptr` and `<param>Utf8` instead of `__<param>`, and fails if one clashes with a parameter; the
  regenerated bindings differ only by those names (3 files). `ci.yml` gains a `style` job: it builds every
  script and runs `dotnet format --verify-no-changes` on the solution and on each script.
  `src/Jade.Interop/Generated/.gitkeep` is gone. CLAUDE.md lists 3 new commands.
- Verification (commands and results), on linux-x64 (CachyOS, .NET SDK 10.0.401):
  - Throwaway file with the brief's violations, in `src/Jade.Interop/` (since deleted).
    `dotnet build -c Release` fails with:

    ```text
    ConventionCheck.cs(1,1): error IDE0161     block-scoped namespace
    ConventionCheck.cs(3,5): error IDE0065     using inside the namespace
    ConventionCheck.cs(5,11): error IDE0040    class without accessibility
    ConventionCheck.cs(5,11): error CA1852     the same class, not sealed
    ConventionCheck.cs(7,21): error IDE1006    private field x: missing prefix '_'
    ConventionCheck.cs(8,28): error IDE1006    private static _y: missing prefix 's_'
    ```

    `this.x` and the public `Int32` do not fail the build (see Deviations). The CI command
    `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'` reports all of the above
    plus `(10,31): error IDE0003` and `(10,16): error IDE0049`.
  - The same file as `src/Jade.Interop/Generated/ConventionCheck.g.cs`, with an `// <auto-generated/>`
    header, gives the same 6 build errors and the same 2 extra `dotnet format` errors. Without
    `--include-generated`, `dotnet format` skips it.
  - The same types in a throwaway script (`scripts/convention-check.cs` plus a `#:include`d
    `scripts/convention-check/Violations.cs`): `dotnet build scripts/convention-check.cs` gives the same 6
    errors, and the converted-project check of the `style` job adds IDE0003 and IDE0049. An entry point with
    top-level statements cannot have a file-scoped namespace, so IDE0161 rightly stays quiet there.
  - Second throwaway file, for the other rules. These fail the build: IDE0005, IDE0007, IDE0011 (multi-line
    body without braces; single-line body whose `else` has braces), IDE0036 (`readonly static`), IDE0044,
    IDE0055 (K&R brace, `if (x) return;`), IDE2000, IDE2001, and IDE1006 for an interface without `I`, a type
    parameter without `T` and a camelCase local constant. These pass: private and internal constants in
    PascalCase, `private static readonly s_x`, a local constant in PascalCase, `if (x)` with a single-line
    body on the next line. Only `dotnet format` reports unsorted usings (`IMPORTS`). A
    `[ThreadStatic] private static int t_x` fails with `missing prefix 's_'`.
  - `dotnet build -c Release --no-incremental`: `0 Avertissement(s)`, `0 Erreur(s)`.
    `dotnet test -c Release`: total 19, failed 0, succeeded 19, skipped 0 (as before the task).
  - `dotnet build scripts/<name>.cs --no-incremental` with `MSBuildTreatWarningsAsErrors=true`: exit 0 for
    `build-native`, `generate-bindings` and `smoke-native` (the build fails on any warning). `dotnet
    scripts/smoke-native.cs` still passes.
  - Generator: two runs give `0 file(s) changed` and the same SHA-256 over the 25 generated files. Against
    `main`, the removed lines with `__x` and `__xUtf8` rewritten to `xPtr` and `xUtf8` equal the added lines,
    so the 35 field renames changed nothing else. A throwaway config rename of `SDL_GetWindowSize.h` to
    `wPtr` fails generation with `SDL_GetWindowSize: the generated local wPtr clashes with a parameter`.
  - The 3 steps of the `style` job, replayed with `bash -e` and `MSBuildTreatWarningsAsErrors=true`: exit 0.
    `actionlint .github/workflows/ci.yml`: no findings. `dotnet pack -c Release` still ships `Jade.xml` and
    `Jade.Interop.xml`.
  - CI itself has not run: nothing is pushed.
- Decisions taken (and ADRs added): no ADR; everything is configuration within ADR-0011 and ADR-0006.
  - Renames went through LSP `findReferences`, each reference position checked before editing. The C# LSP
    has no rename operation, and `dotnet format` cannot load file-based apps.
  - IDE0003 and IDE0049 are `EnforceOnBuild.Never` in Roslyn (`src/Analyzers/Core/Analyzers/
    EnforceOnBuildValues.cs`), as Microsoft Learn also states. `dotnet format` runs them and also checks
    using order, so CI adopts it. It needs `--include-generated` to see the bindings, and `--exclude
    '**/obj/**'` to skip xunit's generated sources. On SDK 10.0.401 it rejects a `.cs` path, so the `style`
    job converts each script with `dotnet project convert` into `artifacts/format/<name>/`, inside the
    repository so `.editorconfig` and `Directory.*.props` still apply. The job runs on Ubuntu only: style is
    OS-independent.
  - Braces: `csharp_prefer_braces = when_multiline`. `true` would forbid the single-line bodies rule 18
    allows, and `false` would allow multi-line bodies without braces. It also requires braces when another
    block of the chain has them. IDE2001 covers rule 18's ban on `if (x) return;`, which
    `csharp_preserve_single_line_statements = false` also reports through IDE0055.
  - Formatting uses dotnet/runtime's new-line and indentation values. Two of them changed:
    `csharp_new_line_before_members_in_object_initializers` from `false` to `true`, and case blocks are not
    indented. Spacing keeps the defaults, which are runtime's. The modifier order is runtime's, which is
    also Roslyn's default, instead of HostFxrSharp's. No existing code changed because of either.
  - `dotnet_style_require_accessibility_modifiers = for_non_interface_members`, as in runtime, where
    interface members carry no `public`.
  - The generated layout tests also opt out of the exemption: ADR-0011 covers `tests/`.
  - With the user's approval, `src/Jade.Interop/Generated/` turns off the four CA rules that ADR-0006
    contradicts: CA1401 (93 public 1:1 imports), CA1069 (12 aliased C enum constants), CA1711 (`InitFlags`,
    `WindowFlags`, `SurfaceFlags`) and CA1716 (`Event`). Also with the user's approval, tests turn off CS1591:
    they generate the documentation file only for IDE0005, and their classes are public for xunit.
  - CA1852 is set explicitly even though `AnalysisMode Recommended` already enables it, so the ADR-0011 row
    does not depend on the analysis mode.
- Deviations from the brief:
  - Acceptance criterion 1 is only partly reachable: `this.x` (IDE0003) and `Int32` (IDE0049) cannot fail the
    build. They fail CI's `style` job instead. So does using order (`System` first, then alphabetical).
  - `t_` cannot be a naming rule: symbol groups cannot select `[ThreadStatic]`, so a thread-static field hits
    the `s_` rule. The repository has none; CLAUDE.md says such a field needs a justified `IDE1006`
    suppression.
  - CLAUDE.md already had the ADR-0011 bullet (added by the orchestrator after the brief), so no bullet was
    added. Its last sentence, "Until task 003 lands, `.editorconfig` does not enforce them yet", now
    describes the enforcement instead.
  - Two suppressions the brief did not foresee, both approved by the user: the four CA rules in the generated
    bindings and CS1591 in tests (see Decisions).
- Follow-ups:
  - 203: `Naming.Identifier` turns digit-leading C names into `_1` (for example `SDL_SCANCODE_1`). With the
    exemption gone, such members fail CA1707 (checked with a throwaway enum); IDE1006 accepts them. The
    generator needs another scheme or a decision on CA1707.
  - Scripts compile in CI on Ubuntu only. They must run on all three desktop OSes, and RID-specific restores
    (ClangSharp's libclang) are not compiled on Windows or macOS anywhere. Consider building them in the
    `build` matrix.
  - IDE2000 and IDE2001 are experimental in Roslyn. If one is removed, its `.editorconfig` lines become
    no-ops and the rule silently stops being enforced.
  - Drop the script conversion in the `style` job once `dotnet format` loads file-based apps.
  - ADR-0011 says accessibility is "always explicit"; the configuration exempts interface members, as
    runtime does. The orchestrator may want the ADR to say so.
