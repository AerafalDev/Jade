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

Architecture, decisions and planned work live in [`design/`](design/): start with
[`design/architecture.md`](design/architecture.md) and [`design/roadmap.md`](design/roadmap.md).

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

## Pull requests

- Branch off `main`; keep each change small and self-contained.
- Write clear, present-tense commit messages, one logical change per commit.
- Make sure `dotnet build -c Release` and `dotnet test -c Release` are green.
- Describe *what* changed and *why*. When you touch interop, cite the native header or API being bound.

## Reporting bugs

Open an issue with the OS and architecture, the .NET version, what you expected and what happened, and a
minimal repro if you can share one. For security-sensitive reports, follow the [Security Policy](SECURITY.md)
instead of opening a public issue.
