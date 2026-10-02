# Jade

Jade is a cross-platform game engine for .NET 10. Work currently targets the **interop layer**: one
native library, `jade_native`, that bundles Dawn (WebGPU), SDL3, miniaudio and, later, more C/C++
libraries, plus public C# bindings generated over it. The engine itself is planned once the
interop layer is green on every target.

Progress lives in [design/roadmap.md](design/roadmap.md). Keep the **Commands** section in sync
with what actually exists.

## How work is organized

- Talk to the user in French. Everything committed (code, comments, docs, commit messages) stays
  in English.
- **Orchestrator sessions** make decisions, write ADRs, task briefs and the roadmap. They do not
  implement.
- **Task sessions** implement exactly one brief from `design/tasks/`. Before writing code:
  1. Read this file, `design/roadmap.md`, your brief, and every ADR the brief links.
  2. Check that each task under "Depends on" is `done` in the roadmap. Otherwise stop and report.
  3. Re-check upstream versions quoted in the brief before pinning: they are snapshots.
- Stay inside the brief's scope. Anything noticed outside it goes to **Outcome → Follow-ups**, not
  into the code.
- A hard-to-reverse decision that the brief does not cover becomes a new ADR with
  `Status: Proposed`, flagged in the Outcome. Never rewrite an `Accepted` ADR; supersede it.
- Finishing a task: fill the brief's **Outcome**, set the task to `done` in `design/roadmap.md`, and
  update **Commands** below if you added any. Put all of this in the same commit as the work,
  following the `git-workflow` skill. No push, PR, tag or release unless asked.

## Repository layout

```text
CLAUDE.md                 this file
design/                   architecture, roadmap, ADRs, task briefs (docs/ is reserved for the
                          future public documentation site, as in the other AerafalDev repos)
native/                   xmake project for jade_native, local package repo, C shims      (101)
scripts/                  C# file-based apps: build-native, generate-bindings, ...         (101, 201)
src/Jade/                 engine (later); the only package users reference                 (001)
src/Jade.Interop/         public bindings, Generated/ is generator output; packed into Jade (001)
src/Jade.Native/          packaging-only project for runtimes/<rid>/native                 (106)
tests/                    xunit.v3 test projects                                           (001)
samples/                  runnable samples                                                 (206)
artifacts/                build outputs (gitignored): native/<rid>/, packages/
```

## Architecture in one screen

- **Native**: per RID, one `jade_native` library that statically links every upstream (Dawn,
  SDL3, miniaudio, ...) and exports only their public C APIs plus our `jade_*` shims. It is a shared
  library on desktop and Android and a static archive on browser-wasm (iOS form decided by
  task 104). Built by xmake on each target's native OS.
- **Bindings**: `scripts/generate-bindings/` reads Dawn's `dawn.json` and C headers (via libclang)
  into one model. It emits `src/Jade.Interop/Generated/<Lib>/*.g.cs`, which is committed.
- **Packages** (one shared version): `Jade` (managed: Jade.dll, Jade.Interop.dll, later analyzers
  and source generators) depends on `Jade.Native` (natives and `buildTransitive` targets). Users
  only reference `Jade`.
- Details: [design/architecture.md](design/architecture.md). Decisions: [design/adr/](design/adr/).

## Conventions

### .NET

- `net10.0`, `LangVersion latest`, nullable, implicit usings, unsafe allowed. Warnings are errors,
  `AnalysisMode Recommended`, code style enforced at build. These shared settings live in
  `Directory.Build.props`/`.targets`; never repeat them in a project file.
- Central Package Management with transitive pinning. Add packages with `dotnet add package` or the
  `nuget` MCP, never with a version from memory.
- MinVer with `v` tag prefix. Every package ships the same version.
- Tests: xunit.v3 on Microsoft.Testing.Platform (no VSTest packages), Shouldly, CsCheck. Every test
  project sets `<IsTestProject>true</IsTestProject>`. Without it, `Directory.Build.targets` adds
  MinVer and SourceLink to it as if it shipped.
- Libraries generate XML docs, and CS1591 is an error: every public member needs `///` docs.
- A local clone without an `origin` remote gets SourceLink warnings. Add the remote rather than
  suppressing them.
- Libraries are AOT- and trim-compatible (`IsAotCompatible`). An AOT or trim warning is a bug.

### Interop

- Never hand-edit generated files (`*.g.cs` under `src/Jade.Interop/Generated/`). Change the
  generator or its per-library config, then regenerate. CI fails when regeneration yields a diff.
- `DisableRuntimeMarshalling`: every native signature is blittable. No `string`, `bool`, delegates
  or `SetLastError`. UTF-8 goes through `byte*` and `ReadOnlySpan<byte>`, C booleans through
  explicitly sized types, callbacks through `delegate* unmanaged[Cdecl]`.
- Every import names the single library `jade_native`.
- Friendly overloads (`Span`, `ReadOnlySpan`, `in`, `ref`, `out`) never allocate on the GC heap.
  Anything that allocates or owns resources belongs to the engine layer, not the interop layer.
- Generated code throws no exceptions and returns native results as they are.
- A C type whose size or layout differs between targets (`long`, platform `#ifdef` fields) is
  never exposed by value under a single definition. See ADR-0006.

### Native

- Every upstream is pinned to an exact version or commit plus SHA-256 in our own xmake package
  definitions under `native/`. Nothing floats, nothing comes from the system.
- `jade_native` exports only the bundled public C APIs and `jade_*` shims. Everything else stays
  hidden.
- A C++-only library gets a thin C shim in `native/shims/`, with functions named `jade_<lib>_*`.
- Adding an upstream library includes adding its license to `THIRD-PARTY-NOTICES.md`.

### Scripts

- Automation is C# file-based apps: `scripts/<name>.cs`, or a kebab-case folder
  `scripts/<name>/` whose entry point is `scripts/<name>/<name>.cs`, with helper files (PascalCase)
  pulled in with `#:include`.
- The entry point starts with `#!/usr/bin/env dotnet`. Without it, a multi-file entry point
  triggers CA2266, which is an error here.
- Run with `dotnet scripts/<name>.cs [args]`; arguments pass through as-is. Scripts inherit the
  repo's `Directory.Build.props` (verified), so analyzers apply to them too.
- File-based apps default to `PublishAot=true`, so AOT and trim analyzers run on scripts. A script
  that needs reflection (for example reflection-based `System.Text.Json`) adds
  `#:property PublishAot=false`. Once several scripts need it, move it to a
  `scripts/Directory.Build.props`.
- Scripts must run unchanged on Windows, macOS and Linux: no shelling out to bash-only tools.

## Commands

Run from the repository root. Each task adds its commands here, with their exact syntax, when it
lands.

| Purpose | Command | Introduced by |
| --- | --- | --- |
| Build the managed solution | `dotnet build -c Release` | 001 |
| Run the tests (Microsoft.Testing.Platform) | `dotnet test -c Release` | 001 |
| Pack the `Jade` package | `dotnet pack -c Release -o artifacts/packages` | 001 |
| Build jade_native for one RID | `dotnet scripts/build-native.cs --rid <rid>` (planned) | 101 |
| Regenerate bindings | `dotnet scripts/generate-bindings/generate-bindings.cs` (planned) | 201 |

## Environment facts (verified 2026-10-02)

- Local machine: CachyOS, .NET SDK 10.0.401, xmake 3.1.1, CMake 4.4.3, clang, gcc, zig 0.16. No
  emsdk, no Android NDK, no `wasm-tools`/`android`/`ios` workloads. Do not install system packages;
  give the user the command instead.
- GitHub arm64 runners (`windows-11-arm`, `ubuntu-24.04-arm`) are free only on public repositories
  and have about 14 GB of disk.
- nuget.org rejects packages over 250 MB.
- Dawn's official prebuilts miss several RIDs and are static-only, so we build Dawn from source.
- xmake-repo has no Dawn package and lags upstream (`libsdl3` 3.4.12 there vs 3.4.16 upstream).
