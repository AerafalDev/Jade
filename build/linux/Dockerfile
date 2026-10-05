# The glibc 2.28 environment of the shipped Linux libraries (docs/adr/0024, docs/adr/0038): the
# manylinux_2_28 image (AlmaLinux 8, GCC 14 from gcc-toolset-14), the development packages of the
# SDL3 features that build/sdl/xmake.lua requires, and libdecor and liburing at versions EL8 lacks.
# The image is pinned by digest for each architecture, tag 2026.10.03-1, the newest on 2026-10-05;
# BuildKit sets TARGETARCH to the architecture of the build.
#
# The .NET SDK is not installed here: the build mounts the runner's, whose portable Linux build
# runs on glibc 2.27 and later. xmake and CMake come from build-native.cs --install-tools.

FROM quay.io/pypa/manylinux_2_28_x86_64@sha256:39df0042d5cc900b085aa25a0659368b42a0006c54c474299b785b44c1b4ff82 AS base-amd64
FROM quay.io/pypa/manylinux_2_28_aarch64@sha256:f5f03dedf61b47d69d0742d36047db78adeee3bdb7a9910ff52304750ecf2514 AS base-arm64

ARG TARGETARCH
FROM base-${TARGETARCH}

RUN dnf install -y \
        libX11-devel libXext-devel libXcursor-devel libXfixes-devel libXi-devel libXrandr-devel \
        libXScrnSaver-devel libXtst-devel libxcb-devel \
        wayland-devel wayland-protocols-devel libxkbcommon-devel \
        libdrm-devel mesa-libgbm-devel mesa-libEGL-devel mesa-libGL-devel \
        dbus-devel ibus-devel systemd-devel libusbx-devel fribidi-devel libthai-devel \
        ninja-build meson cairo-devel pango-devel libicu \
    && dnf clean all

# SDL3 loads libdecor and liburing-ffi at runtime, so its build only needs their headers, their
# pkg-config files and their SONAME; nothing of them is shipped. EL8 has no libdecor, and its
# liburing 1.0.7 has no liburing-ffi. Both are fetched at the commit of their release tag.
RUN git init -q /tmp/libdecor \
    && git -C /tmp/libdecor fetch -q --depth 1 https://gitlab.freedesktop.org/libdecor/libdecor.git 149c6f0b05663aaa69fdf7f94be2483776d1a311 \
    && git -C /tmp/libdecor checkout -q --detach FETCH_HEAD \
    && test "$(git -C /tmp/libdecor rev-parse HEAD)" = 149c6f0b05663aaa69fdf7f94be2483776d1a311 \
    && meson setup /tmp/libdecor/build /tmp/libdecor --prefix=/usr --libdir=lib64 --buildtype=release -Ddemo=false -Dgtk=disabled \
    && ninja -C /tmp/libdecor/build install \
    && rm -rf /tmp/libdecor

RUN git init -q /tmp/liburing \
    && git -C /tmp/liburing fetch -q --depth 1 https://github.com/axboe/liburing.git d41bf9220ec39277ff235379e9089d9e0fd6c2a5 \
    && git -C /tmp/liburing checkout -q --detach FETCH_HEAD \
    && test "$(git -C /tmp/liburing rev-parse HEAD)" = d41bf9220ec39277ff235379e9089d9e0fd6c2a5 \
    && cd /tmp/liburing \
    && ./configure --prefix=/usr --libdir=/usr/lib64 --libdevdir=/usr/lib64 \
    && make -C src -j"$(nproc)" \
    && make -C src install \
    && rm -rf /tmp/liburing
