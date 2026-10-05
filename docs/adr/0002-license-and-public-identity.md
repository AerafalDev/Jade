# 0002. MIT license and a technology-neutral public identity

- Status: Accepted
- Date: 2026-10-05

## Context

Jade is a public, open-source project published as NuGet packages. Its public face (repository page,
README, package listings) should describe what Jade does for its users. The libraries it builds on
are implementation details that may change, but their licenses impose attribution obligations.

## Decision

- Jade is licensed under the MIT license (`LICENSE`).
- Public-facing texts are professional and generic: the repository description and topics, the
  README introduction, and NuGet package descriptions and tags. They present Jade as a
  cross-platform 2D game engine for .NET (desktop, mobile, web) and do not name the underlying
  libraries or implementation details.
- Only the technical documentation (`CONTRIBUTING.md`, `docs/`) and `THIRD-PARTY-NOTICES.md`
  (a legal obligation) name the libraries Jade uses.
- Common NuGet metadata lives in `Directory.Build.props`: `Authors`, `Copyright`,
  `PackageLicenseExpression` (`MIT`), `PackageProjectUrl`, `RepositoryUrl`, `PackageReadmeFile`,
  `PackageIcon` and generic `PackageTags`.
- Each package has its own `Description`, focused on its role (for example
  "Graphics interop layer for Jade.").

## Consequences

- Replacing a backend library does not require rewriting public texts.
- `THIRD-PARTY-NOTICES.md` must be created and kept in sync with the pinned native dependencies,
  from the license files of the pinned sources, as soon as native binaries are distributed.
- Package IDs are not covered by this rule and can still hint at a technology (see
  [0004](0004-webgpu-via-dawn.md)).
