# Architecture decision records

One file per decision: `NNNN-kebab-title.md`. Never rewrite an `Accepted` ADR. Write a new one
that supersedes it and set the old one's status to `Superseded by ADR-NNNN`. Task sessions may add
ADRs with status `Proposed`; the orchestrator, together with the user, accepts or rejects them.

| ADR | Decision | Status |
| --- | --- | --- |
| [0001](0001-webgpu-backend-dawn.md) | Dawn on every native RID, emdawnwebgpu on the web | Accepted |
| [0002](0002-package-layout.md) | `Jade` references `Jade.Native`; Jade.Interop is packed into `Jade` | Accepted |
| [0003](0003-single-combined-native-library.md) | One combined `jade_native` library per RID | Accepted, amended by 0019 |
| [0004](0004-native-build-with-xmake.md) | xmake drives the native build, with our own pinned packages | Accepted |
| [0005](0005-in-house-binding-generator.md) | In-house binding generator in `scripts/generate-bindings/` | Accepted |
| [0006](0006-binding-api-shape.md) | .NET-style public bindings, methods on handles, span overloads | Accepted, C `bool` amended by 0012 |
| [0007](0007-target-platforms.md) | Desktop, mobile and browser RIDs | Accepted |
| [0008](0008-scripts-as-file-based-apps.md) | Scripts are C# file-based apps | Accepted, multi-file layout superseded by 0010 |
| [0009](0009-task-workflow.md) | Orchestrator, one session per task, briefs in `design/tasks/` | Accepted |
| [0010](0010-script-entry-point-next-to-its-folder.md) | Script entry point `scripts/<name>.cs`, helpers in `scripts/<name>/`, one type per file | Accepted |
| [0011](0011-csharp-coding-conventions.md) | C# conventions: dotnet/runtime style, `var` everywhere, file-scoped namespaces, enforced at build | Accepted |
| [0012](0012-c-bool-maps-to-system-boolean.md) | C `bool` maps to `System.Boolean`; wider boolean typedefs keep their width | Accepted |
| [0013](0013-linux-glibc-baseline.md) | Linux RIDs build in an AlmaLinux 8 container: glibc 2.28 baseline, clang 21 with gcc-toolset-15 | Accepted |
| [0014](0014-imgui.md) | Dear ImGui docking, dear_bindings C API, native SDL3/WebGPU backends, selected extensions | Accepted, amended by 0018 |
| [0015](0015-asset-libraries.md) | Asset libraries: runtime set in `jade_native`, import set in a desktop-only `jade_tools` | Accepted for the runtime set; `jade_tools` deferred |
| [0016](0016-function-pointers-in-import-signatures.md) | Function pointers are `nint` in import signatures (browser-wasm), typed in the public API | Accepted |
| [0017](0017-ecs-architecture.md) | ECS: chunked archetype storage, generated components, queries and systems, one schedule with single- and multi-threaded executors | Proposed |
| [0018](0018-2d-engine.md) | Jade is a 2D engine: Box3D, ImGuizmo, ImPlot3D, meshoptimizer, cgltf and ufbx leave the plan | Accepted |
| [0019](0019-runtime-and-tools-native-libraries.md) | Two native libraries: `jade_native` for the runtime, `jade_tools` for tools (ImGui, import-time libraries) | Accepted |

## Template

```markdown
# ADR-NNNN: Title

- Status: Proposed | Accepted | Superseded by ADR-NNNN
- Date: YYYY-MM-DD

## Context

## Decision

## Consequences
```
