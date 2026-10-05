using System.Runtime.InteropServices;

namespace Jade.NativeBuild.Build;

/// <summary>The machine the build runs on.</summary>
/// <param name="Platform">The host's platform family: Linux, Windows or macOS.</param>
/// <param name="Architecture">The architecture of the build process, which decides what it can load.</param>
internal sealed record HostPlatform(NativePlatform Platform, Architecture Architecture)
{
    /// <summary>Gets the machine the build runs on.</summary>
    /// <exception cref="PlatformNotSupportedException">The host is neither Linux, Windows nor macOS.</exception>
    public static HostPlatform Current { get; } = new(
        OperatingSystem.IsLinux() ? NativePlatform.Linux
            : OperatingSystem.IsWindows() ? NativePlatform.Windows
            : OperatingSystem.IsMacOS() ? NativePlatform.MacOS
            : throw new PlatformNotSupportedException($"The native build does not run on {RuntimeInformation.OSDescription}."),
        RuntimeInformation.ProcessArchitecture);

    /// <summary>Gets the operating system part of the host keys of <c>build/versions.json</c>: <c>linux</c>, <c>windows</c> or <c>macos</c>.</summary>
    public string OperatingSystemKey => Platform switch
    {
        NativePlatform.Linux => "linux",
        NativePlatform.Windows => "windows",
        NativePlatform.MacOS => "macos",
        NativePlatform.IOS or NativePlatform.Android or NativePlatform.Browser or _ => throw new InvalidOperationException($"{Platform} is not a host platform."),
    };

    /// <summary>Gets the host key of <c>build/versions.json</c>, such as <c>linux-arm64</c>.</summary>
    public string Key => $"{OperatingSystemKey}-{ArchitectureKey}";

    /// <summary>Gets the architecture part of the host key: <c>x64</c> or <c>arm64</c>.</summary>
    /// <exception cref="PlatformNotSupportedException">The architecture is neither x64 nor arm64.</exception>
    public string ArchitectureKey => Architecture switch
    {
        Architecture.X64 => "x64",
        Architecture.Arm64 => "arm64",
        Architecture.X86 or Architecture.Arm or Architecture.Wasm or Architecture.S390x or Architecture.LoongArch64 or Architecture.Armv6 or Architecture.Ppc64le or Architecture.RiscV64 or _
            => throw new PlatformNotSupportedException($"The native build does not run on {Architecture}."),
    };
}
