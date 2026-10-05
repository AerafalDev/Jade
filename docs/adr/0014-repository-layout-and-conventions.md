# 0014. Repository layout and content conventions

- Status: Accepted
- Date: 2026-10-05

## Context

The repository hosts managed libraries, Roslyn components, a binding generator, native build
definitions, samples for four platform families and their tests. A fixed layout keeps tooling,
CI and documentation predictable.

## Decision

```text
.github/                     workflows, issue/PR templates, dependabot, CODEOWNERS, labeler, labels
docs/
  architecture.md
  roadmap.md
  adr/                       one decision per file
native/                      versions.json, xmake package definitions, C shims
scripts/                     .NET file-based apps
  binding-generator.cs
  binding-generator/         files included with #:include
  build-native.cs
  fetch-native.cs
src/
  Jade/
  Jade.SourceGenerators/
  Jade.Analyzers/
  Jade.Wgpu/  Jade.Sdl/  Jade.MiniAudio/  Jade.Emscripten/      generated interop
  Jade.Native.Wgpu/  Jade.Native.Sdl/  Jade.Native.MiniAudio/   runtimes/ + buildTransitive/
samples/                     Desktop, Android, iOS, Browser
tests/
CLAUDE.md  README.md  LICENSE  CONTRIBUTING.md  CODE_OF_CONDUCT.md  SECURITY.md
CHANGELOG.md  THIRD-PARTY-NOTICES.md  .gitignore  .gitattributes
Jade.slnx  global.json  Directory.Build.props  Directory.Build.targets
Directory.Packages.props  .editorconfig
```

- Repository scripts are .NET file-based apps in `scripts/`.
- `Jade.SourceGenerators` and `Jade.Analyzers` exist from the start.
- In each interop project, generated code goes to `Generated/*.g.cs`, next to the hand-written
  partials and the generator configuration.
- `samples/` holds one sample per platform family (Desktop, Android, iOS, Browser). They validate
  the interop and the natives on each target.
- Everything in the repository is written in English: code, identifiers, comments, XML
  documentation, commit messages, pull requests, issues and documentation.
- MSBuild files (`.csproj`, `.props`, `.targets`) and `Jade.slnx` contain no comments.

## Consequences

- Repository tooling needs only the .NET SDK, plus the native toolchains for native work.
- Explanations that would otherwise sit in MSBuild comments belong in `docs/` or in the relevant
  ADR.
