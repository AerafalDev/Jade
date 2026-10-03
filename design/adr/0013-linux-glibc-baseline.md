# ADR-0013: Linux glibc baseline: AlmaLinux 8 container, glibc 2.28

- Status: Accepted (2026-10-03)
- Date: 2026-10-03

## Context

ADR-0007 asks for Linux binaries built against an old glibc, chosen by task 103. A shared library
built on a recent distribution requires that distribution's glibc: the host build of 102 needed
`GLIBC_2.44`, so it would not load on most distributions still in use.

- .NET 10 supports glibc 2.27 and later on x64 and arm64 (`release-notes/10.0/supported-os.md` in
  dotnet/core, built against Ubuntu 18.04). Going below 2.27 would gain nothing for Jade.
- Dawn needs C++20: clang 19 or GCC 12 and their standard library (Dawn's `docs/building.md`). Old
  distributions ship older compilers, so the baseline needs a recent toolchain on an old system.
- SDL compiles against the system headers of X11, Wayland, xkbcommon, libdecor and others, then
  loads those libraries at runtime. Their build-time versions decide which code SDL contains and
  the oldest runtime versions it accepts.
- Candidates, by glibc version: CentOS 7 (2.17, end of life, no C++20 toolchain), Ubuntu 18.04
  (2.27, end of standard support), Debian 10 and AlmaLinux 8 (2.28; Debian 10 is end of life),
  Debian 11 and Ubuntu 20.04 (2.31), RHEL 9 clones (2.34).
- AlmaLinux 8.10, a RHEL 8 rebuild supported until 2029, ships clang 21 (`llvm-toolset`) configured
  to use gcc-toolset-15: GCC 15's libstdc++, including a complete `libstdc++.a`, and binutils 2.44.
  It keeps older package versions in its repositories, so exact versions can be pinned. The same
  image exists for x86-64 and arm64. It lacks `libdecor-devel` and has PipeWire 0.3.6.

## Decision

- Linux RIDs build in a container from `almalinux:8.10`, pinned by digest, defined in
  `native/linux/Dockerfile`. Its glibc, 2.28, is the baseline: `jade_native` must not need a newer
  `GLIBC_` symbol version.
- The toolchain is clang 21.1.8 with gcc-toolset-15 (GCC 15.2.1's libstdc++ and libgcc, binutils
  2.44), CMake 3.26.5 and Ninja 1.8.2, each pinned to that version in the Dockerfile. libstdc++ and
  libgcc are linked statically, as ADR-0003 requires.
- The headers are the Fedora list of SDL's `docs/README-linux.md` as AlmaLinux 8 provides it, plus
  libdecor 0.2.5 built from its release archive (SHA-256 pinned) because AlmaLinux 8 has none. The
  Dockerfile is the only copy of that list.
- `dotnet scripts/build-native.cs --rid <linux-rid> --container` builds the image and runs the build
  inside it. CI uses that command, and it is the reference build for Linux; a host build remains
  possible for development.
- Every CI build checks the highest `GLIBC_` version in `objdump -T` against 2.28.

## Consequences

- `jade_native` loads on any distribution with glibc 2.28 or later: RHEL 8 and its clones, Debian 10,
  Ubuntu 20.04 and everything newer. Ubuntu 18.04 (2.27) is not covered, although the first build
  only needed `GLIBC_2.27`.
- SDL is built against Wayland 1.21 and xkbcommon 0.9.1. It clamps some Wayland protocol versions
  (`wl_compositor` 4 instead of 6, `wl_seat` 8 instead of 10, no `wl_shm` 2 or `wl_fixes`) and uses its
  own fallback for the xkbcommon 1.0 and 1.10 helpers. In exchange, its Wayland backend accepts any
  libwayland from 1.20 and any xkbcommon from 0.5; older systems fall back to X11.
- SDL's PipeWire camera backend needs libpipewire 0.3.44, so the Linux build has only the V4L2
  camera backend. Audio is off in SDL anyway.
- The image takes about 3 minutes to build and 3 GB of disk. CI builds it in every Linux job.
- Moving the baseline means a new Dockerfile base and a new ADR superseding this one.
