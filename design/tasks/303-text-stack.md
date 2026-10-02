# 303: Text stack: FreeType, HarfBuzz, msdfgen

- Depends on: 201, 103
- ADRs: 0003, 0004, 0005, 0006

## Goal

FreeType, HarfBuzz and msdfgen are built into jade_native from pinned packages. FreeType and
HarfBuzz are bound directly, and msdfgen (C++) is reachable through a `jade_msdf_*` C shim, enough
to load a font, shape a string and generate an MSDF glyph.

## Context

- HarfBuzz `14.5.1` and msdfgen `v1.13` on 2026-10-02. Check FreeType's latest release on its
  official site; the GitHub mirror tags are not useful.
- msdf-atlas-gen (`v1.4`) builds atlases on top of msdfgen. Whether it is needed at runtime or only
  in tooling is a question for 304 and the engine. Do not include it unless the user says so.
- FreeType and HarfBuzz can depend on each other (HarfBuzz's FreeType integration, FreeType's
  HarfBuzz-based autohinting). Choose the configuration deliberately.
- FreeType is dual-licensed (FTL or GPLv2). Use FTL, which requires attribution in the notices.
  Verify against the license files.

## Scope

- Pinned xmake packages, configuration flags recorded (FreeType modules, compression libraries
  such as zlib, brotli and png for color fonts: decide), and exports.
- `native/shims/jade_msdf.{h,cpp}`: a minimal C API over msdfgen (glyph shape from FreeType outline
  → MSDF bitmap), designed together with 201's config.
- Generator configs for FreeType (macro-heavy: check what libclang sees), HarfBuzz and the shim.
- Tests with a small font under a permissive license committed under `tests/`, license included:
  load, shape `"Jade"`, check glyph count and advances, generate one MSDF glyph and check its
  dimensions.

## Out of scope

- Font atlas management, text layout and rendering in the engine.

## Acceptance criteria

- [ ] All three libraries are in jade_native for linux-x64, with the desktop RIDs in CI.
- [ ] Deterministic generated output; clean build; the export cross-check passes.
- [ ] Tests pass.

## Verification

Native build, generator run, test run.

## Pitfalls

- FreeType exposes many structs with `FT_Long` (C `long`), which differs between Windows and the
  other platforms. This is where ADR-0006's layout-variance rule matters.

## Outcome

_Filled by the task session._

- Summary:
- Verification (commands and results):
- Decisions taken (and ADRs added):
- Deviations from the brief:
- Follow-ups:
