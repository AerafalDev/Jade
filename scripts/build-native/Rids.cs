using System.Runtime.InteropServices;

/// <summary>The RIDs jade_native is built for, and how xmake builds each one.</summary>
internal static class Rids
{
    // Platform, architecture and toolchain names come from `xmake f --help` and
    // `xmake show -l toolchains` (xmake 3.1.1). Only linux-x64 has been built; tasks 103, 104 and
    // 105 verify the others and fix what they need.

    /// <summary>Every RID built now (ADR-0007), with its xmake configuration.</summary>
    public static readonly IReadOnlyDictionary<string, XmakeTarget> All = new Dictionary<string, XmakeTarget>
    {
        ["win-x64"] = new XmakeTarget("windows", "x64", "msvc", verified: false),
        ["win-arm64"] = new XmakeTarget("windows", "arm64", "msvc", verified: false),
        ["linux-x64"] = new XmakeTarget("linux", "x86_64", "clang", verified: true),
        ["linux-arm64"] = new XmakeTarget("linux", "arm64", "clang", verified: false),
        ["osx-x64"] = new XmakeTarget("macosx", "x86_64", "xcode", verified: false),
        ["osx-arm64"] = new XmakeTarget("macosx", "arm64", "xcode", verified: false),
        ["android-arm64"] = new XmakeTarget("android", "arm64-v8a", "ndk", verified: false),
        ["android-x64"] = new XmakeTarget("android", "x86_64", "ndk", verified: false),
        ["ios-arm64"] = new XmakeTarget("iphoneos", "arm64", "xcode", verified: false),
        ["iossimulator-arm64"] = new XmakeTarget("iphoneos", "arm64", "xcode", verified: false, "--appledev=simulator"),
        ["iossimulator-x64"] = new XmakeTarget("iphoneos", "x86_64", "xcode", verified: false, "--appledev=simulator"),
        ["browser-wasm"] = new XmakeTarget("wasm", "wasm32", "emcc", verified: false),
    };

    /// <summary>Returns the RID of the machine running the script.</summary>
    /// <exception cref="PlatformNotSupportedException">The host OS or architecture has no RID here.</exception>
    public static string Host()
    {
        var os = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : OperatingSystem.IsLinux() ? "linux"
            : throw new PlatformNotSupportedException("Unsupported host OS: pass --rid.");

        // The machine's architecture, not the process's: an emulated x64 SDK on arm64 still builds arm64.
        var architecture = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            var other => throw new PlatformNotSupportedException($"Unsupported host architecture {other}: pass --rid."),
        };
        return $"{os}-{architecture}";
    }
}
