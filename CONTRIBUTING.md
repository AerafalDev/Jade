# Contributing to Jade

Thanks for taking the time to contribute! Jade is a cross-platform game engine for .NET, currently in early
interop work. Issues, bug reports and pull requests are all welcome.

By participating you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Getting started

You need the **.NET 10 SDK**. The minimum version is pinned in [`global.json`](global.json), so a matching SDK
is selected automatically.

```sh
git clone https://github.com/AerafalDev/Jade.git
cd Jade

dotnet build -c Release                           # build the solution
dotnet test -c Release                            # run the tests (Microsoft.Testing.Platform)
dotnet pack -c Release -o artifacts/packages      # produce the Jade and Jade.Native packages
```

## How work is organized

Architecture, decisions and planned work live in [`design/`](design/), next to the code:

- [`design/architecture.md`](design/architecture.md) gives the overall picture.
- [`design/roadmap.md`](design/roadmap.md) lists every task with its status, phase by phase.
- Each task has a brief under [`design/tasks/`](design/tasks/): goal, scope, acceptance criteria, and an
  Outcome filled in when it lands.
- Decisions that are hard to reverse are recorded as ADRs under [`design/adr/`](design/adr/).

Before starting on something larger than a fix, open an issue or check the roadmap: the work may already be
planned, or an ADR may rule it out.

## Coding conventions

Style is enforced by [`.editorconfig`](.editorconfig) and checked at build time; please don't fight it. The
points that matter most here:

- **C# style**: file-scoped namespaces, 4-space indentation, Allman braces, `var` when the type is apparent,
  `_camelCase` private fields.
- **No primary constructors**: declare constructors explicitly.
- **XML documentation**: public types and members of the libraries carry `///` docs; the build generates the
  XML doc file, so a missing comment on public API is a build error.
- **Warnings are errors**: the build runs with `TreatWarningsAsErrors` and `AnalysisMode` `Recommended`, so a
  green build means zero warnings. Libraries are Native-AOT and trim compatible; an AOT or trim warning is a
  bug.
- **Generated bindings**: never hand-edit files under `src/Jade.Interop/Generated/`. Change the generator or
  its configuration and regenerate.
- **Interop hygiene**: `Jade.Interop` disables runtime marshalling, so every native signature is blittable:
  UTF-8 strings as `byte*`, C booleans as explicitly sized integers, callbacks as
  `delegate* unmanaged[Cdecl]`.

Files are **UTF-8 (no BOM)**, stored with LF line endings and checked out with your platform's native ones (see
`.gitattributes`).

## Tests

New behaviour ships with a test. Tests live under `tests/` (xUnit v3 on Microsoft.Testing.Platform, with
[Shouldly](https://github.com/shouldly/shouldly) and [CsCheck](https://github.com/AnthonyLloyd/CsCheck)).

Run `dotnet test -c Release` before opening a pull request.

## Issues

Open an issue from one of the forms:

- **Bug report** asks for the Jade version, the RID, the OS, the .NET SDK and the native (graphics) backend,
  plus a minimal repro if you can share one.
- **Feature request** asks what you are trying to do before what would help.

A blank issue works for anything else, such as a question. For security-sensitive reports, follow the
[Security Policy](SECURITY.md) instead of opening a public issue.

## Pull requests

- Branch off `main`; keep each change small and self-contained.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/):
  `<type>: <summary>`, imperative and lowercase, where the type is `feat`, `fix`, `refactor`, `perf`, `test`,
  `docs`, `build`, `ci`, `chore` or `style`. One logical change per commit.
- Make sure `dotnet build -c Release` and `dotnet test -c Release` are green.
- Describe *what* changed and *why* in a sentence or two, and end with a `Tested: ...` line that says what you
  ran; the pull request template shows how. When you touch interop, cite the native header or API being bound.

`main` is protected: every change reaches it through a pull request, which can merge once the `CI result`,
`CodeQL result` and `Native result` checks pass. Pull requests merge with a merge commit, and their branch is
deleted afterwards.

## Labels

Labels come in groups: `type:` (bug, feature, docs, performance, ci, build, chore), `area:`, `platform:` and
`status:`, plus `good first issue`, `help wanted`, and `dependencies` and `github_actions` on Dependabot's pull
requests. A workflow adds `area:` and `platform:` labels to pull requests from the paths they change;
maintainers set the others. The set is declared in [`.github/labels.yml`](.github/labels.yml) and applied with
`dotnet scripts/sync-labels.cs`, so a change to the labels goes through that file.
