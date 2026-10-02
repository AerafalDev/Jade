# Security Policy

## Supported versions

Jade is in early development and has no release yet. It is developed on the `main` branch; once releases
start, security fixes will be applied to `main` and shipped in the next release, and only the **latest
published version** will be supported.

## Reporting a vulnerability

**Please do not report security issues in public GitHub issues or pull requests.**

Jade loads a native library (`jade_native`) and calls into the C libraries it bundles (Dawn, SDL3,
miniaudio and more later). The security surface that matters most is therefore **the boundary between managed
and native code**: memory-safety issues in the bindings (wrong signatures, sizes or lifetimes), the way
`jade_native` is located and loaded, and vulnerabilities in a bundled upstream version that Jade fails to pick
up.

Report privately through either channel:

- **GitHub Security Advisories**: open the repository's **Security → Report a vulnerability** tab to start a
  private advisory (preferred).
- **Email**: <aerafal.github@gmail.com>.

Please include:

- a description of the issue and its impact,
- a minimal repro (OS, architecture, .NET version, and the smallest code/inputs that trigger it),
- the version (or commit) you observed it on.

## What to expect

- We aim to acknowledge a report within a few days.
- We'll confirm the issue, keep you updated as we work on a fix, and credit you in the release notes unless you
  prefer to stay anonymous.
- Once a fix is released, the advisory is published.

Thank you for helping keep Jade and its users safe.
