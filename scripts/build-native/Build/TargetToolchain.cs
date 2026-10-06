using System.Globalization;
using Jade.NativeBuild.Configuration;
using Jade.NativeBuild.Tools;

namespace Jade.NativeBuild.Build;

/// <summary>What a target adds to the xmake configuration, and how its libraries are checked.</summary>
/// <param name="Options">The xmake configuration options of the target (<c>--name=value</c>).</param>
/// <param name="Environment">The environment variables the target's tools need.</param>
/// <param name="Symbols">Reads the symbols of the libraries when the build process cannot load them, or <see langword="null"/>.</param>
internal sealed record TargetToolchain(IReadOnlyList<string> Options, IReadOnlyDictionary<string, string?> Environment, SymbolReader? Symbols)
{
    /// <summary>Prepares the toolchain of a target: finds and checks its SDK, and turns the minimum OS versions into compile targets (ADR 0024).</summary>
    /// <param name="target">The target.</param>
    /// <param name="host">The host.</param>
    /// <param name="versions">The pinned versions.</param>
    /// <param name="layout">The repository layout.</param>
    /// <param name="cancellationToken">Stops the version checks.</param>
    /// <returns>The toolchain.</returns>
    /// <exception cref="InvalidDataException">The target's SDK is missing or not at the pinned version.</exception>
    public static async Task<TargetToolchain> PrepareAsync(NativeTarget target, HostPlatform host, PinnedVersions versions, BuildLayout layout, CancellationToken cancellationToken)
    {
        var minimum = versions.MinimumOs;
        var loadable = target.CanLoadOn(host);

        switch (target.Platform)
        {
            case NativePlatform.Windows:
                // Windows names its versions by _WIN32_WINNT, which only has a major and a minor
                // number: the build of the 1607 floor is a support floor, not a compile setting.
                var windows = Version.Parse(minimum.Windows);
                var winnt = string.Create(CultureInfo.InvariantCulture, $"0x{windows.Major:X2}{windows.Minor:X2}");

                return new(["--runtimes=MT", "--win32_winnt=" + winnt], new Dictionary<string, string?>(), Symbols: null);

            case NativePlatform.MacOS or NativePlatform.IOS:
                var minver = target.Platform == NativePlatform.MacOS ? minimum.Macos : minimum.Ios;
                List<string> options = ["--target_minver=" + minver];

                if (target.Simulator)
                {
                    options.Add("--appledev=simulator");
                }

                return new(options, new Dictionary<string, string?>(), loadable && !target.IsStatic ? null : new SymbolReader("xcrun", ["nm", "-g", "-U", "-j"], Environment: null, Prefixed: true));

            case NativePlatform.Android:
                var ndk = AndroidNdk.Locate(versions.Toolchains.AndroidNdk);
                var nm = Path.Combine(AndroidNdk.GetToolDirectory(ndk, host), host.Platform == NativePlatform.Windows ? "llvm-nm.exe" : "llvm-nm");

                return new(
                    [
                        "--ndk=" + ndk,
                        "--ndk_sdkver=" + minimum.AndroidApiLevel.ToString(CultureInfo.InvariantCulture),
                        // Dawn is the only C++ library and exposes a C API, so the C++ runtime is
                        // linked into it rather than shipped as libc++_shared.so beside it.
                        "--runtimes=c++_static",
                    ],
                    new Dictionary<string, string?>(),
                    new SymbolReader(nm, ["--dynamic", "--defined-only", "--extern-only", "--format=just-symbols", "--quiet"], Environment: null, Prefixed: false));

            case NativePlatform.Browser:
                var cache = Path.Combine(layout.GetObjectDirectory(target.RuntimeIdentifier), "emscripten-cache");
                var workload = await EmscriptenWorkload.LocateAsync(versions.Toolchains.Emscripten, host, cache, cancellationToken).ConfigureAwait(false);
                var environment = workload.CreateEnvironment(cache);

                workload.PrepareCache(cache);

                return new(["--emsdk=" + workload.EmscriptenDirectory], environment, new SymbolReader(workload.SymbolTool, ["--defined-only", "--extern-only", "--format=just-symbols", "--quiet"], environment, Prefixed: false));

            case NativePlatform.Linux:
            default:
                // The glibc floor is the build environment's own (CI's manylinux_2_28 container),
                // not a compiler option.
                return new([], new Dictionary<string, string?>(), Symbols: null);
        }
    }
}
