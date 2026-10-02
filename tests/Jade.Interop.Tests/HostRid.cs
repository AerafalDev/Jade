using System.Runtime.InteropServices;

namespace Jade.Interop.Tests;

/// <summary>The ADR-0007 RID of the machine running the tests, independent of how the runtime itself was built.</summary>
internal static class HostRid
{
    /// <summary>Gets the RID, for example <c>linux-x64</c>, or <c>unknown-...</c> outside the desktop RIDs.</summary>
    public static string Current { get; } = $"{Os()}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";

    private static string Os() =>
        OperatingSystem.IsWindows() ? "win"
        : OperatingSystem.IsMacOS() ? "osx"
        : OperatingSystem.IsLinux() ? "linux"
        : "unknown";
}
