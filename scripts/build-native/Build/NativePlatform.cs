namespace Jade.NativeBuild.Build;

/// <summary>The platform families of the runtime identifiers of ADR 0012.</summary>
internal enum NativePlatform
{
    /// <summary>Linux with glibc: <c>linux-x64</c>, <c>linux-arm64</c>.</summary>
    Linux,

    /// <summary>Windows: <c>win-x64</c>, <c>win-arm64</c>.</summary>
    Windows,

    /// <summary>macOS: <c>osx-arm64</c>, <c>osx-x64</c>.</summary>
    MacOS,

    /// <summary>iOS devices and simulators: <c>ios-arm64</c>, <c>iossimulator-arm64</c>, <c>iossimulator-x64</c>.</summary>
    IOS,

    /// <summary>Android: <c>android-arm64</c>, <c>android-x64</c>.</summary>
    Android,

    /// <summary>The browser: <c>browser-wasm</c>.</summary>
    Browser,
}
