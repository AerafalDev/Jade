# Security Policy

## Supported versions

Jade is in early development and has no release yet. Security fixes are made on the `main`
branch. Once packages are published, only the latest release will be supported.

## Reporting a vulnerability

Please do not report security issues in public issues, discussions or pull requests.

Report them privately through GitHub: open the repository's **Security** tab and choose
**Report a vulnerability**.

Useful details:

- a description of the issue and its impact;
- the affected platform (OS, architecture, browser if relevant) and .NET version;
- the version or commit, and the smallest code or input that reproduces it.

Jade loads and calls native code, so issues in the native interop (memory safety, library
loading), in the native binaries we build and ship, and in the build and release pipeline are all
in scope.

## What to expect

- An acknowledgement within a week.
- Updates while the issue is investigated and fixed.
- Credit in the advisory and the release notes, unless you prefer to stay anonymous.
- The advisory is published once a fix is available.
