# ADR-0018: Jade is a 2D engine

- Status: Accepted
- Date: 2026-10-03
- Amends: ADR-0014 (ImGui extensions) and ADR-0015 (asset libraries)

## Context

Until now the plan left the engine's dimension open, and the surveys kept 3D candidates: Box3D for
physics (302), ImGuizmo and ImPlot3D for ImGui (301, ADR-0014), meshoptimizer for meshes and cgltf
and ufbx for model import (304, ADR-0015). On 2026-10-03 the user stated that Jade is a 2D engine.

## Decision

- Jade is a 2D game engine. Rendering still goes through WebGPU (Dawn, ADR-0001), which suits
  sprite batching, 2D lighting and post-processing as well as 3D.
- Removed from the plan, with the user's agreement:
  - Box3D: task 302 binds Box2D v3 only;
  - ImGuizmo, a 3D transform gizmo: task 313 binds ImPlot only. A 2D gizmo belongs to the editor;
  - ImPlot3D: task 317 is dropped;
  - meshoptimizer: task 305 binds zstd only;
  - cgltf and ufbx, the deferred model importers of `jade_tools` (ADR-0015).
- Kept, because they serve 2D as well:
  - SDL3, miniaudio, Dawn;
  - Box2D, ImGui with ImPlot, imnodes and the small widgets;
  - FreeType, HarfBuzz and msdfgen for text;
  - zstd, stb_image, Vorbis, and the basis_universal transcoder for compressed sprite atlases and
    GPU memory;
  - the deferred basisu encoder for texture cooking.
- A survey of 2D-specific libraries comes later, with the engine phase. Candidates: tilemaps (Tiled,
  LDtk), skeletal animation (Spine, whose runtime license is restrictive, DragonBones, Rive), vector
  graphics (ThorVG, Lottie), geometry (Clipper2, earcut), atlas packing, particles, 2D lighting.

## Consequences

- ADR-0017 (ECS architecture, Proposed) is reviewed with the user with 2D in mind: 2D transforms,
  hierarchy, sorting by layer and depth.
- The platform list (ADR-0007) and the interop layer do not change.
