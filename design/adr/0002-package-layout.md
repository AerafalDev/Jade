# ADR-0002: Package layout

- Status: Accepted
- Date: 2026-10-02

## Context

Users should reference a single package, `Jade`. The natives for about twelve RIDs, some of them
static archives (browser-wasm, possibly iOS), may not fit in one package: nuget.org rejects packages
over 250 MB. Analyzers and source generators will come later and belong with the managed code.

## Decision

Only two packages are published, both with the same version:

- **Jade**: the package users reference. It contains `lib/<tfm>/Jade.dll` and
  `lib/<tfm>/Jade.Interop.dll`. The `Jade.Interop` project is packed into `Jade` and is not
  published separately. Analyzers and source generators will go under `analyzers/dotnet/cs/`. It
  depends on `Jade.Native` with an exact version range (`[x.y.z]`).
- **Jade.Native**: contains no managed code, only `runtimes/<rid>/native/`, the `buildTransitive/`
  targets that wire up static linking where required (browser-wasm, iOS), and
  `THIRD-PARTY-NOTICES.md`.

Packing fails if `Jade.Native` exceeds a safety threshold below 250 MB. If it ever does, it is split
by platform family (`Jade.Native.Desktop`, `.Android`, `.Apple`, `.Browser`). `Jade` then depends on
all of them, and users still reference only `Jade`.

## Consequences

- With a single version line, `Jade.Native` is repacked, and downloaded again by users, on every
  release, even when the natives did not change. The CI cache keeps rebuilding cheap; the extra
  download is the price of one version.
- Packing `Jade.Interop.dll` into `Jade` needs explicit pack targets: the ProjectReference is
  private and its output is added to the package (task 001).
- Release builds only. Debug symbols for natives are CI artifacts, not package content.
