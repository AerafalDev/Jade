# Jade

Jade is a cross-platform 2D game engine for .NET 10 (ADR-0018). Work currently targets the **interop layer**: one
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
- Finishing a task: fill the brief's **Outcome**, set the task to `done` in `design/roadmap.md`,
  update **Commands** below if you added any, and add an entry under `## [Unreleased]` in
  `CHANGELOG.md` for anything a user of the packages would notice (public API, bundled libraries
  and their versions, supported RIDs, package layout). Internal changes (CI, scripts, design docs)
  get no entry. If the README's **Status** or **Platforms** tables change, update them too. Put all of this in the same commit as the work,
  following the `git-workflow` skill. No push, PR, tag or release unless asked.

## Repository layout

```text
CLAUDE.md                 this file
design/                   architecture, roadmap, ADRs, task briefs (docs/ is reserved for the
                          future public documentation site, as in the other AerafalDev repos)
native/                   xmake project for jade_native, local package repo, C shims,     (101)
                          Linux build container (native/linux/)                           (103)
scripts/                  C# file-based apps: build-native, generate-bindings, ...         (101, 201)
src/Jade/                 engine (later); the only package users reference                 (001)
src/Jade.Interop/         public bindings, Generated/ is generator output, <Lib>/ hand-written
                          helpers; packed into Jade                                        (001)
src/Jade.Native/          packaging-only project for runtimes/<rid>/native                 (106)
tests/                    xunit.v3 test projects; Jade.PackageTests, a package consumer    (001, 106)
samples/                  runnable samples                                                 (206)
artifacts/                build outputs (gitignored): native/<rid>/, packages/
```

## Architecture in one screen

- **Native**: per RID, one `jade_native` library that statically links every upstream (Dawn,
  SDL3, miniaudio, ...) and exports only their public C APIs plus our `jade_*` shims. It is a shared
  library on desktop and Android and a static archive on browser-wasm (iOS form decided by
  task 104). Built by xmake on each target's native OS.
- **Bindings**: `scripts/generate-bindings.cs` reads C headers (via libclang, once per target triple)
  and, from 202, Dawn's `dawn.json` into one model. It emits `src/Jade.Interop/Generated/<Lib>/*.g.cs`
  plus layout tests in `tests/Jade.Interop.Tests/Generated/<Lib>/`, both committed.
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
- Coding conventions: ADR-0011, the dotnet/runtime style with `var` everywhere and file-scoped
  namespaces. They apply to src, tests, scripts and generated code. The rules broken most often:
  no `this.`; private and internal fields `_camelCase`, static ones `s_camelCase`; accessibility
  always explicit and first; `using` outside the namespace, `System` first. `.editorconfig` makes
  them build errors, except `this.` (IDE0003), BCL type names (IDE0049) and `using` order, which
  only `dotnet format` reports; CI's style job runs it (see **Commands**). Naming rules cannot see
  `[ThreadStatic]`, so a `t_` field needs a justified `IDE1006` suppression. Before committing
  C#, run the style check commands from **Commands**: the build alone misses those three rules.

### Interop

- Never hand-edit generated files (`*.g.cs` under `src/Jade.Interop/Generated/` and
  `tests/Jade.Interop.Tests/Generated/`). Change the generator or its per-library config
  (`scripts/generate-bindings/<Lib>Config.cs`), then regenerate. CI fails when regeneration yields a diff.
- `DisableRuntimeMarshalling`: every native signature is blittable. No `string`, delegates or
  `SetLastError`. UTF-8 goes through `byte*` and `ReadOnlySpan<byte>`, callbacks through
  `delegate* unmanaged[Cdecl]`. C `bool` (1 byte) maps to C# `bool`; boolean typedefs over wider
  integers (`WGPUBool`, `ma_bool32`) keep their width (ADR-0012). The `[LibraryImport]` generator
  rejects a bare `bool` (SYSLIB1051), so imports mark it `[MarshalAs(UnmanagedType.U1)]`.
- Every import names the single library `jade_native`.
- Friendly overloads (`Span`, `ReadOnlySpan`, `in`, `ref`, `out`) never allocate on the GC heap.
  Anything that allocates or owns resources belongs to the engine layer, not the interop layer.
  They only lend memory for the call: a pointer the C library returns or keeps stays raw.
- Generated code throws no exceptions and returns native results as they are.
- A C type whose size or layout differs between targets (`long`, platform `#ifdef` fields) is
  never exposed by value under a single definition. See ADR-0006.

### Native

- Every upstream is pinned to an exact version or commit plus SHA-256 in our own xmake package
  definitions under `native/`. Nothing floats, nothing comes from the system.
- `jade_native` exports only the bundled public C APIs and `jade_*` shims. Everything else stays
  hidden.
- Bundling a library: its package's `on_install` calls `native/modules/stage.lua`, which records the
  exports (exact names or `*` patterns), staged headers, licenses and upstream metadata; then one
  line in the `bundled` list of `native/xmake.lua`. The `jade.bundle` rule
  (`native/rules/bundle.lua`) does whole-archive linking and export control.
- Native build prerequisites: xmake 3.1.1, CMake, Ninja (xmake 3.1.1 builds every CMake package
  with it), clang (MSVC on Windows, Xcode on macOS), Python 3, git and, on Linux, unzip (xmake
  extracts zip archives with it there).
- Linux libraries that CI builds or anyone ships come from the glibc 2.28 container of
  `native/linux/Dockerfile` (`--container`, ADR-0013), which needs Docker with a rootful daemon.
  The Dockerfile holds the toolchain and the only list of system headers. A host build needs the
  equivalent development packages (SDL's `docs/README-linux.md` list, plus `libx11-xcb-dev` on
  Ubuntu) and gives a library tied to the host's glibc.
- Managed-only work can download CI's jade_native with `scripts/fetch-native.cs` instead of
  building it.
- Only `scripts/build-native.cs` runs xmake. It passes `--require=y` because xmake does not notice
  edits to recipes under `native/packages/` on its own.
- A C++-only library gets a thin C shim in `native/shims/`, with functions named `jade_<lib>_*`.
- Adding an upstream library includes adding its license to `THIRD-PARTY-NOTICES.md`.
- `src/Jade.Native` packs `lib/` of every RID staged under `artifacts/native/` into
  `runtimes/<rid>/native/`. `JadeNativeRequiredRids` lists the RIDs a release ships: packing without
  one warns (JADENATIVE001, an error in CI). Packing fails over `JadeNativeMaxPackageSize`, 240 MB
  (JADENATIVE002, ADR-0002). Platform link wiring goes in `buildTransitive/Jade.Native.targets`.

### Scripts

- Automation is C# file-based apps. The entry point is always `scripts/<name>.cs` (kebab-case).
  A multi-file script keeps its helper files in a kebab-case folder next to it, `scripts/<name>/`,
  pulled in with `#:include <name>/<Type>.cs` (ADR-0010, which supersedes the layout in
  ADR-0008).
- One class, struct, record or enum per file, named after the type. The entry point holds only
  top-level statements.
- Every `internal` or `public` type and member of a script has `///` docs, as in the libraries.
- The entry point starts with `#!/usr/bin/env dotnet`. Without it, a multi-file entry point
  triggers CA2266, which is an error here.
- Run with `dotnet scripts/<name>.cs [args]`; arguments pass through as-is. Scripts inherit the
  repo's `Directory.Build.props` (verified), so analyzers apply to them too.
- File-based apps default to `PublishAot=true`, so AOT and trim analyzers run on scripts. A script
  that needs reflection (for example reflection-based `System.Text.Json`) adds
  `#:property PublishAot=false`. Once several scripts need it, move it to a
  `scripts/Directory.Build.props`.
- NuGet packages: `#:package <Id>` without a version, plus a `PackageVersion` in
  `Directory.Packages.props` (Central Package Management applies to scripts; a version on the
  directive fails with NU1008). A package whose native assets come through `runtime.json` (ClangSharp's
  libclang) also needs `#:property RuntimeIdentifier=$(NETCoreSdkRuntimeIdentifier)`, or restore skips
  them and a system copy may load instead.
- Scripts must run unchanged on Windows, macOS and Linux: no shelling out to bash-only tools.

## Commands

Run from the repository root. Each task adds its commands here, with their exact syntax, when it
lands.

| Purpose | Command | Introduced by |
| --- | --- | --- |
| Build the managed solution | `dotnet build -c Release` | 001 |
| Run the tests (Microsoft.Testing.Platform) | `dotnet test -c Release` | 001 |
| Pack `Jade` and `Jade.Native`, the latter with every RID staged under `artifacts/native/`, and report the size per RID | `dotnet pack -c Release -o artifacts/packages` | 001, 106 |
| Pack, then restore `tests/Jade.PackageTests` from `artifacts/packages`, run it, and run it again as a NativeAOT binary of the host RID | `dotnet scripts/test-package.cs [--no-pack]` | 106 |
| Build jade_native and stage it into `artifacts/native/<rid>/`, symbols into `artifacts/native-symbols/<rid>/` (RID defaults to the host); `--print-config` only prints the RID's xmake configuration | `dotnet scripts/build-native.cs [--rid <rid>] [--config release\|debug] [--container] [--prune-packages] [--print-config]` | 101, 103, 004 |
| Build a Linux RID in the glibc baseline container, as CI does | `dotnet scripts/build-native.cs --rid linux-x64 --container` | 103 |
| Download CI's jade_native (latest successful `native.yml` run on main) into `artifacts/native/<rid>/` | `dotnet scripts/fetch-native.cs [--rid <rid>]... [--branch <branch>] [--run <run-id>]` | 103 |
| Smoke-check the staged jade_native of the host RID | `dotnet scripts/smoke-native.cs [--rid <rid>]` | 101 |
| Regenerate bindings from the staged headers (RID defaults to the host) | `dotnet scripts/generate-bindings.cs [--rid <rid>]` | 201 |
| Apply `.github/labels.yml` to the repository's labels through `gh`, after checking that the labeler, Dependabot and the issue forms only use declared labels; undeclared labels are deleted only with `--delete` | `dotnet scripts/sync-labels.cs [--repo <owner/name>] [--delete] [--dry-run]` | 005 |
| Build a script without running it | `dotnet build scripts/<name>.cs` | 003 |
| Check the code style of the solution, generated bindings included, as CI does | `dotnet format --verify-no-changes --include-generated --exclude '**/obj/**'` | 003 |
| Check the code style of `tests/Jade.PackageTests`, which is not in the solution (packages from `test-package.cs` first; MSBuild reads the version from the environment) | `JadePackageVersion=<version> dotnet format tests/Jade.PackageTests/Jade.PackageTests.csproj --verify-no-changes` | 106 |
| Check the code style of a script as CI does (delete `artifacts/format/<name>` first: convert refuses an existing folder) | `dotnet project convert scripts/<name>.cs --output artifacts/format/<name>`, then `dotnet format artifacts/format/<name>/<name>.csproj --verify-no-changes` | 003 |

## Environment facts (verified 2026-10-02)

- Local machine: CachyOS, .NET SDK 10.0.401, xmake 3.1.1, CMake 4.4.3, clang, gcc, zig 0.16. No
  emsdk, no Android NDK or SDK, no `wasm-tools`/`android`/`ios` workloads. Docker 29.8.2 works
  without sudo. OpenJDK 25 is installed. actionlint 1.7.12 is installed, shellcheck is not. Do not install system packages; give the user the command instead.
- CI sets `MSBuildTreatWarningsAsErrors=true`, so MSBuild task warnings (SourceLink, MinVer, SDK)
  that a local build only reports fail the job.
- GitHub arm64 runners (`windows-11-arm`, `ubuntu-24.04-arm`) are free only on public repositories
  and have about 14 GB of disk.
- nuget.org rejects packages over 250 MB.
- Dawn's official prebuilts miss several RIDs and are static-only, so we build Dawn from source.
- xmake-repo has no Dawn package and lags upstream (`libsdl3` 3.4.12 there vs 3.4.16 upstream).
