# Roadmap

Status is tracked here and only here: `todo`, `in-progress`, `done` or `blocked`. Each linked
task has a self-contained brief. Entries without a link wait for their prerequisites to report
back before the orchestrator writes their brief.

## Phase 0: Foundation

| Task | Title | Depends on | Status |
| --- | --- | --- | --- |
| [001](tasks/001-repository-scaffold.md) | Repository scaffold and managed solution | - | done |
| [002](tasks/002-ci-baseline.md) | CI baseline: build, test, CodeQL, Dependabot | 001 | done |
| [003](tasks/003-coding-conventions.md) | Enforce the C# coding conventions, clean up existing code | 102, 201 | done |
| [004](tasks/004-ci-turnaround.md) | Faster CI turnaround: skip docs-only, reuse native artifacts, cache the container | 103 | done |
| [005](tasks/005-github-organization.md) | GitHub organization: templates, code owners, labels, settings, ruleset on main | 004 | done |

003 runs once 102 and 201 are merged, before any other task starts: 103, 202, 203, 204, 301 and
304 list it as a dependency in their briefs.

## Phase 1: Native library

| Task | Title | Depends on | Status |
| --- | --- | --- | --- |
| [101](tasks/101-native-build-skeleton.md) | xmake skeleton: jade_native with SDL3 and miniaudio, host RID | 001 | done |
| [102](tasks/102-native-dawn.md) | Dawn from source, linked into jade_native, host RID | 101 | done |
| [103](tasks/103-native-desktop-matrix.md) | Desktop RID matrix in CI, Linux glibc baseline, artifact cache | 102, 002 | done |
| [104](tasks/104-native-mobile.md) | Android and iOS RIDs, mobile platform glue proposal | 103 | todo |
| [105](tasks/105-native-browser.md) | browser-wasm: Emscripten alignment with .NET, emdawnwebgpu | 103 | todo |
| [106](tasks/106-native-packaging.md) | Jade.Native package, Jade dependency, size guard, local feed test | 103 | done |
| 107 | Release pipeline: tag → natives → pack → publish (trusted publishing) | 106 | todo |
| [108](tasks/108-jade-tools-library.md) | jade_tools: move Dear ImGui out of jade_native (ADR-0019), Jade.Tools packages | 310 | todo |

104 and 105 can run in parallel. 106 starts with desktop RIDs; 104 and 105 each extend its
`buildTransitive` targets for their platforms.

Notes for the 107 brief, collected from earlier Outcomes:

- Pack with every RID staged and `MSBuildTreatWarningsAsErrors=true` (JADENATIVE001 then fails on a
  missing RID), and push only `artifacts/packages/*.nupkg` plus the `Jade` `.snupkg` (106).
- `Jade.Native` is labelled MIT, but it bundles zlib, BSD-3-Clause, Apache-2.0, MIT-0 or public
  domain code and, on Linux, the GCC runtime under GPL-3.0 with the Runtime Library Exception.
  Decide between an SPDX expression and a `PackageLicenseFile` (106).
- Release notes come from `CHANGELOG.md`: the release moves `## [Unreleased]` to
  `## [x.y.z] - date`, adds the compare links, and uses that section as the GitHub release body
  (and possibly `PackageReleaseNotes`). Add the NuGet badge to the README with the first published
  version.
- `actions/checkout` is shallow by default, so MinVer computes `0.0.0-alpha.0`. Use
  `fetch-depth: 0`, as HostFxrSharp's `publish.yml` does (002).
- `THIRD-PARTY-NOTICES.md` lacks the notices of the 24 Wayland protocol glue files that SDL
  generates from `wayland-protocols/*.xml` (MIT-style). Add them before the first release (101).
- Publishing uses nuget.org trusted publishing (OIDC); the `nuget-trusted-publishing` skill covers
  the setup.
- `THIRD-PARTY-NOTICES.md` claims every license is reproduced in full, but the entry for the
  statically linked GCC runtime (GPLv3 with the GCC Runtime Library Exception 3.1) is not. Fix the
  wording or the entry (102).

## Phase 2: Bindings

| Task | Title | Depends on | Status |
| --- | --- | --- | --- |
| [201](tasks/201-binding-generator-core.md) | Generator core: model, libclang reader, emitter, drift check | 101 | done |
| [202](tasks/202-bindings-webgpu.md) | WebGPU bindings from dawn.json | 201, 102 | done |
| [203](tasks/203-bindings-sdl3.md) | SDL3 bindings | 201 | done |
| [204](tasks/204-bindings-miniaudio.md) | miniaudio bindings | 201 | todo |
| [205](tasks/205-interop-smoke-and-aot.md) | Interop smoke tests and NativeAOT publish check | 202, 203, 204 | todo |
| [206](tasks/206-sample-hello-triangle.md) | Sample: SDL3 window + Dawn triangle on desktop | 205 | todo |
| 207 | Generator polish: `WGPU_*_INIT` defaults from dawn.json, XML docs from webgpu-headers' `webgpu.yml`, hexadecimal enum values (from 202) | 202 | todo |

202, 203 and 204 can run in parallel once 201 is done (202 also needs 102).

## Phase 3: Extra libraries

| Task | Title | Depends on | Status |
| --- | --- | --- | --- |
| [301](tasks/301-imgui-survey.md) | Survey: Dear ImGui, its extensions and backends | 201 | done |
| [302](tasks/302-physics-box2d.md) | Box2D v3: native and bindings | 201, 103 | todo |
| [303](tasks/303-text-stack.md) | FreeType + HarfBuzz + msdfgen: native, C shim, bindings | 201, 103 | todo |
| [304](tasks/304-assets-survey.md) | Survey: asset libraries for runtime and import pipeline | 201, 003 | done |
| [305](tasks/305-zstd.md) | zstd | 201, 103 | todo |
| [306](tasks/306-basisu-transcoder.md) | basis_universal transcoder | 305, 201, 103 | todo |
| [307](tasks/307-stb-image-vorbis.md) | stb_image and Vorbis through miniaudio | 201, 103, 204 | todo |
| [310](tasks/310-imgui-native.md) | Dear ImGui (docking), dear_bindings C API and backends in jade_native | 103, 003 | todo |
| [311](tasks/311-imgui-bindings.md) | Dear ImGui core bindings | 108, 201 | todo |
| [312](tasks/312-imgui-backends-sample.md) | ImGui SDL3 and WebGPU backend bindings, demo sample | 311, 202, 203, 206 | todo |
| [313](tasks/313-implot.md) | ImPlot | 311 | todo |
| [314](tasks/314-imgui-widgets.md) | Small ImGui widgets and imnodes | 311 | todo |
| [315](tasks/315-imgui-text-editor.md) | ImGuiColorTextEdit | 311 | todo |
| [316](tasks/316-imgui-freetype.md) | imgui_freetype | 303, 310 | todo |

Surveys (301, 304) produce a Proposed ADR and draft briefs. The orchestrator turns them into
implementation tasks.

Deferred from 304 (ADR-0015): `jade_tools` with the basisu encoder (draft E in 304's Outcome), until
the engine phase designs the asset pipeline. cgltf and ufbx (drafts D and F) are dropped: Jade is a 2D
engine (ADR-0018), as are Box3D, ImGuizmo, ImPlot3D (former task 317) and meshoptimizer.

Deferred from 301 (ADR-0014): imgui-node-editor, until
upstream compiles with the pinned ImGui or a fork is chosen; ImGuiFileDialog (optional, SDL3's
dialogs come first); `dcimgui_internal` for the DockBuilder API.

## Phase 4 and later: Engine

| Task | Title | Depends on | Status |
| --- | --- | --- | --- |
| [401](tasks/401-ecs-survey.md) | Survey: ECS design from the inspirations (storage, queries, scheduling, source generators) | - | done |

ADR-0017 (ECS architecture, from 401) stays Proposed until the user and the orchestrator review it
point by point, with 2D in mind (ADR-0018). Its draft briefs E1 to E8 become tasks only after that review, once Phase 2 is `done`
on desktop and at least one of 104 or 105 is `done`.

A survey of 2D-specific libraries comes with the engine phase (ADR-0018): tilemaps, skeletal
animation, vector graphics, geometry, atlas packing, particles, 2D lighting.
