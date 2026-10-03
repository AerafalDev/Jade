# Build environment of jade_native for linux-x64 and linux-arm64 (ADR-0013). AlmaLinux 8 fixes the
# glibc baseline at 2.28, and the system headers below set the oldest X11, Wayland and xkbcommon
# that SDL's runtime-loaded backends accept. `dotnet scripts/build-native.cs --container` builds
# this image and runs itself inside it; nothing else should.
FROM almalinux:8.10@sha256:8b469a3a78515e8a18ea8fc727e6a3679e1d0c5ba6f58d5b30be3d0d9b86cffe

# Toolchain: clang from llvm-toolset. Its packaged configuration (/etc/clang/*.cfg) points it at
# gcc-toolset-15, which provides libstdc++ (C++20 for Dawn, with a full libstdc++.a to link
# statically), libgcc and binutils; the system GCC 8 only builds xmake and libdecor below.
# Build tools: CMake, Ninja (xmake builds CMake packages with it), Python 3.12 for Dawn's code
# generator (EL8's default python3 is 3.6), git for Dawn's dependency fetch.
# The packages that compile or link jade_native are pinned to a version (AlmaLinux keeps the older
# ones in its repositories), so this file names the toolchain and CI's package cache key, which
# hashes it, changes with it. The rest only follows EL 8.10's fixes.
# Headers: the Fedora list of SDL's docs/README-linux.md at the pinned release, minus what EL8 does
# not ship (mesa-libGLES-devel, vulkan-devel, pipewire-jack-audio-connection-kit-devel and
# libdecor-devel; libusb1-devel is libusbx-devel here). SDL bundles the Khronos headers and audio
# is off, so only libdecor matters: it is built below. Dawn's X11 surfaces need libX11-devel,
# which ships X11/Xlib-xcb.h here.
RUN dnf -y install dnf-plugins-core \
 && dnf config-manager --set-enabled powertools \
 && dnf -y install \
      clang-21.1.8 llvm-21.1.8 gcc-toolset-15-gcc-c++-15.2.1 gcc-toolset-15-binutils-2.44 \
      cmake-3.26.5 ninja-build-1.8.2 gcc make python3.12 git-core pkgconf-pkg-config \
      alsa-lib-devel fribidi-devel pulseaudio-libs-devel pipewire-devel \
      libX11-devel libXext-devel libXrandr-devel libXcursor-devel libXfixes-devel \
      libXi-devel libXScrnSaver-devel libXtst-devel dbus-devel ibus-devel \
      systemd-devel mesa-libGL-devel libxkbcommon-devel \
      mesa-libEGL-devel vulkan-headers wayland-devel wayland-protocols-devel \
      libdrm-devel mesa-libgbm-devel libusbx-devel libthai-devel liburing-devel \
      meson cairo-devel pango-devel readline-devel libicu xz \
 && dnf clean all \
 && alternatives --set python3 /usr/bin/python3.12

# libdecor draws window decorations on compositors without server-side ones (GNOME). SDL loads it
# at runtime and only needs its headers and library name here; the optional entry points of newer
# versions are looked up at runtime, so building against the latest release costs no
# compatibility.
ARG LIBDECOR_VERSION=0.2.5
ARG LIBDECOR_SHA256=7fd50f780a4fee90a03f7b2c09055033e488654cbaff4a0c4bbae616bac9cd1c
RUN curl -fsSLo /tmp/libdecor.tar.xz https://gitlab.freedesktop.org/-/project/18349/uploads/eaf9a38b227a6e4997b5cb8fc3b8c830/libdecor-${LIBDECOR_VERSION}.tar.xz \
 && echo "${LIBDECOR_SHA256}  /tmp/libdecor.tar.xz" | sha256sum -c - \
 && tar -xJf /tmp/libdecor.tar.xz -C /tmp \
 && cd /tmp/libdecor-${LIBDECOR_VERSION} \
 && meson setup build --prefix=/usr --libdir=lib64 --buildtype=release -Ddemo=false -Dgtk=disabled \
 && meson install -C build \
 && rm -rf /tmp/libdecor*

# xmake has no prebuilt Linux arm64 binary, so it is built from its release archive on both
# architectures, as xmake-io/github-action-setup-xmake does on Linux runners.
ARG XMAKE_VERSION=3.1.1
ARG XMAKE_SHA256=e67e6692bdf8fcb10cdb2cc78c0e8f0ae6c86ad3e3fc532880feca49eefb74b8
RUN curl -fsSLo /tmp/xmake.tar.gz https://github.com/xmake-io/xmake/releases/download/v${XMAKE_VERSION}/xmake-v${XMAKE_VERSION}.tar.gz \
 && echo "${XMAKE_SHA256}  /tmp/xmake.tar.gz" | sha256sum -c - \
 && mkdir /tmp/xmake \
 && tar -xzf /tmp/xmake.tar.gz -C /tmp/xmake --strip-components=1 \
 && cd /tmp/xmake \
 && ./configure \
 && make -j"$(nproc)" \
 && make install PREFIX=/usr/local \
 && rm -rf /tmp/xmake*

# The SDK global.json asks for; scripts/build-native.cs passes its version.
ARG DOTNET_SDK_VERSION
RUN test -n "${DOTNET_SDK_VERSION}" \
 && curl -fsSLo /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh \
 && bash /tmp/dotnet-install.sh --version "${DOTNET_SDK_VERSION}" --install-dir /usr/local/dotnet \
 && ln -s /usr/local/dotnet/dotnet /usr/local/bin/dotnet \
 && rm /tmp/dotnet-install.sh

ENV DOTNET_ROOT=/usr/local/dotnet \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Labeler check.
