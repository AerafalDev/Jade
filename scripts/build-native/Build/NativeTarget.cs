using System.Runtime.InteropServices;

namespace Jade.NativeBuild.Build;

/// <summary>A runtime identifier of ADR 0012, how xmake names it and what its build produces.</summary>
/// <param name="RuntimeIdentifier">The .NET runtime identifier, such as <c>linux-x64</c>.</param>
/// <param name="Platform">The platform family.</param>
/// <param name="Architecture">The processor architecture of the binaries.</param>
/// <param name="XmakeArchitecture">The xmake architecture (<c>--arch</c>).</param>
/// <param name="Simulator">Whether the target is the iOS simulator (<c>--appledev=simulator</c>).</param>
internal sealed record NativeTarget(string RuntimeIdentifier, NativePlatform Platform, Architecture Architecture, string XmakeArchitecture, bool Simulator = false)
{
    /// <summary>The functions that prove a Dawn library is the native one (<c>wgpuGetProcAddress</c> is JavaScript in Emdawnwebgpu).</summary>
    private static readonly string[] _dawnExports = ["wgpuCreateInstance", "wgpuGetProcAddress"];

    /// <summary>The functions that prove an Emdawnwebgpu archive holds its C implementation.</summary>
    private static readonly string[] _emdawnwebgpuExports = ["wgpuCreateInstance", "wgpuInstanceRequestAdapter"];

    /// <summary>The JavaScript libraries and Closure externs that the final <c>emcc</c> link of Emdawnwebgpu needs.</summary>
    private static readonly string[] _emdawnwebgpuScripts =
    [
        "library_webgpu.js",
        "library_webgpu_enum_tables.js",
        "library_webgpu_generated_sig_info.js",
        "library_webgpu_generated_struct_info.js",
        "webgpu-externs.js",
    ];

    /// <summary>Gets every runtime identifier the build defines.</summary>
    public static IReadOnlyList<NativeTarget> All { get; } =
    [
        new("linux-x64", NativePlatform.Linux, Architecture.X64, "x86_64"),
        new("linux-arm64", NativePlatform.Linux, Architecture.Arm64, "arm64"),
        new("win-x64", NativePlatform.Windows, Architecture.X64, "x64"),
        new("win-arm64", NativePlatform.Windows, Architecture.Arm64, "arm64"),
        new("osx-arm64", NativePlatform.MacOS, Architecture.Arm64, "arm64"),
        new("osx-x64", NativePlatform.MacOS, Architecture.X64, "x86_64"),
        new("ios-arm64", NativePlatform.IOS, Architecture.Arm64, "arm64"),
        new("iossimulator-arm64", NativePlatform.IOS, Architecture.Arm64, "arm64", Simulator: true),
        new("iossimulator-x64", NativePlatform.IOS, Architecture.X64, "x86_64", Simulator: true),
        new("android-arm64", NativePlatform.Android, Architecture.Arm64, "arm64-v8a"),
        new("android-x64", NativePlatform.Android, Architecture.X64, "x86_64"),
        new("browser-wasm", NativePlatform.Browser, Architecture.Wasm, "wasm32"),
    ];

    /// <summary>Gets the xmake platform (<c>--plat</c>).</summary>
    public string XmakePlatform => Platform switch
    {
        NativePlatform.Linux => "linux",
        NativePlatform.Windows => "windows",
        NativePlatform.MacOS => "macosx",
        NativePlatform.IOS => "iphoneos",
        NativePlatform.Android => "android",
        NativePlatform.Browser => "wasm",
        _ => throw new InvalidOperationException($"Unknown platform {Platform}."),
    };

    /// <summary>Gets a value indicating whether the target links native code statically into the application (iOS, browser).</summary>
    public bool IsStatic => Platform is NativePlatform.IOS or NativePlatform.Browser;

    /// <summary>Gets the libraries the build produces for this target, which the packages ship.</summary>
    public IReadOnlyList<ExpectedLibrary> Libraries
    {
        get
        {
            var libraries = new List<ExpectedLibrary>
            {
                new(LibraryFileName("webgpu_dawn"), Platform == NativePlatform.Browser ? _emdawnwebgpuExports : _dawnExports),
            };

            // Dawn loads DXC at runtime by name, from its own directory first (ADR 0030).
            if (Platform == NativePlatform.Windows)
            {
                libraries.Add(new("dxcompiler.dll", ["DxcCreateInstance"]));
            }

            libraries.Add(new(LibraryFileName("SDL3"), ["SDL_Init", "SDL_CreateWindow"]));
            libraries.Add(new(LibraryFileName("miniaudio"), ["ma_context_init", "ma_engine_init", "jade_ma_device_alloc"]));

            if (Platform == NativePlatform.Browser)
            {
                libraries.AddRange(_emdawnwebgpuScripts.Select(static script => new ExpectedLibrary(script, [])));
            }

            return libraries;
        }
    }

    /// <summary>Gets the libraries that only the tests load: the layout libraries (ADR 0036).</summary>
    public IReadOnlyList<ExpectedLibrary> TestLibraries =>
    [
        new(LibraryFileName("jade_miniaudio_layout"), ["jade_miniaudio_layout"]),
        new(LibraryFileName("jade_sdl_layout"), ["jade_sdl_layout"]),
        new(LibraryFileName("jade_wgpu_layout"), ["jade_wgpu_layout"]),
    ];

    /// <summary>Finds a target by its runtime identifier.</summary>
    /// <param name="runtimeIdentifier">The runtime identifier.</param>
    /// <returns>The target, or <see langword="null"/> when the build does not define it.</returns>
    public static NativeTarget? Find(string runtimeIdentifier)
    {
        return All.FirstOrDefault(target => string.Equals(target.RuntimeIdentifier, runtimeIdentifier, StringComparison.Ordinal));
    }

    /// <summary>Gets the target of the machine the build runs on, built when no runtime identifier is given.</summary>
    /// <param name="host">The host.</param>
    /// <returns>The host's own target.</returns>
    public static NativeTarget ForHost(HostPlatform host)
    {
        return All.First(target => target.Platform == host.Platform && target.Architecture == host.Architecture);
    }

    /// <summary>Tells whether a host can build this target.</summary>
    /// <param name="host">The host.</param>
    /// <returns>
    /// <see langword="true"/> for Linux and Windows on a host of the same platform and architecture,
    /// the Apple targets on macOS, and Android and the browser anywhere their toolchains run.
    /// </returns>
    /// <remarks>
    /// Linux and Windows are built natively: cross-building them would need a glibc 2.28 sysroot
    /// with SDL3's development packages, or the host tools of DXC's LLVM.
    /// </remarks>
    public bool CanBuildOn(HostPlatform host)
    {
        return Platform switch
        {
            NativePlatform.Linux or NativePlatform.Windows => host.Platform == Platform && host.Architecture == Architecture,
            NativePlatform.MacOS or NativePlatform.IOS => host.Platform == NativePlatform.MacOS,
            NativePlatform.Android or NativePlatform.Browser => true,
            _ => false,
        };
    }

    /// <summary>Tells whether the build process can load this target's libraries, to check their exports.</summary>
    /// <param name="host">The host.</param>
    /// <returns><see langword="true"/> when the target is the host's own platform and architecture.</returns>
    public bool CanLoadOn(HostPlatform host)
    {
        return Platform == host.Platform && Architecture == host.Architecture;
    }

    /// <summary>Gets the file name of a library on this target.</summary>
    /// <param name="name">The library name, as the managed side imports it.</param>
    /// <returns>The file name, such as <c>libSDL3.so</c> or <c>SDL3.a</c> in the browser (ADR 0025).</returns>
    private string LibraryFileName(string name)
    {
        return Platform switch
        {
            NativePlatform.Linux or NativePlatform.Android => $"lib{name}.so",
            NativePlatform.Windows => $"{name}.dll",
            NativePlatform.MacOS => $"lib{name}.dylib",
            NativePlatform.IOS => $"lib{name}.a",
            NativePlatform.Browser => $"{name}.a",
            _ => throw new InvalidOperationException($"Unknown platform {Platform}."),
        };
    }
}
