using System.Text.RegularExpressions;
using Jade.NativeBuild.Build;
using Jade.NativeBuild.Configuration;

namespace Jade.NativeBuild.Tools;

/// <summary>Finds the Android NDK of <c>build/versions.json</c>.</summary>
internal static partial class AndroidNdk
{
    /// <summary>Finds the pinned NDK among the usual locations.</summary>
    /// <param name="ndk">The pinned NDK.</param>
    /// <returns>The NDK's root directory.</returns>
    /// <exception cref="InvalidDataException">No location holds the pinned revision.</exception>
    /// <remarks>
    /// The Android SDK keeps each NDK under <c>ndk/&lt;revision&gt;</c>, and runner images point
    /// <c>ANDROID_NDK_ROOT</c> at their default NDK, which need not be the pinned one: every
    /// candidate is accepted by the revision its <c>source.properties</c> reports, never by its path.
    /// </remarks>
    public static string Locate(PinnedAndroidNdk ndk)
    {
        var candidates = new List<string>();

        foreach (var variable in (string[])["ANDROID_NDK_ROOT", "ANDROID_NDK_HOME", "ANDROID_NDK"])
        {
            if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } directory)
            {
                candidates.Add(directory);
            }
        }

        foreach (var variable in (string[])["ANDROID_HOME", "ANDROID_SDK_ROOT"])
        {
            if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } sdk)
            {
                candidates.Add(Path.Combine(sdk, "ndk", ndk.Revision));
            }
        }

        return candidates.FirstOrDefault(directory => GetRevision(directory) == ndk.Revision)
            ?? throw new InvalidDataException(
                $"No Android NDK {ndk.Version} ({ndk.Revision}) was found: install it with 'sdkmanager \"ndk;{ndk.Revision}\"' and set ANDROID_HOME, or point ANDROID_NDK_ROOT at it.");
    }

    /// <summary>Gets the directory of the NDK's LLVM tools for the host.</summary>
    /// <param name="root">The NDK's root directory.</param>
    /// <param name="host">The host.</param>
    /// <returns>The <c>bin</c> directory of the host's prebuilt toolchain.</returns>
    public static string GetToolDirectory(string root, HostPlatform host)
    {
        var tag = host.Platform switch
        {
            NativePlatform.Linux => "linux-x86_64",
            NativePlatform.MacOS => "darwin-x86_64",
            NativePlatform.Windows => "windows-x86_64",
            NativePlatform.IOS or NativePlatform.Android or NativePlatform.Browser or _ => throw new InvalidOperationException($"{host.Platform} is not a host platform."),
        };

        return Path.Combine(root, "toolchains", "llvm", "prebuilt", tag, "bin");
    }

    /// <summary>Reads the revision of an NDK.</summary>
    /// <param name="directory">The candidate directory.</param>
    /// <returns>The revision, or <see langword="null"/> when the directory is not an NDK.</returns>
    private static string? GetRevision(string directory)
    {
        var properties = Path.Combine(directory, "source.properties");

        if (!File.Exists(properties))
        {
            return null;
        }

        var match = Revision().Match(File.ReadAllText(properties));

        return match.Success ? match.Groups["revision"].Value : null;
    }

    /// <summary>Matches the revision line of an NDK's <c>source.properties</c>, such as <c>Pkg.Revision = 28.2.13676358</c>.</summary>
    /// <returns>The pattern, with the revision in the <c>revision</c> group.</returns>
    [GeneratedRegex(@"^Pkg\.Revision\s*=\s*(?<revision>[\w.-]+)\s*$", RegexOptions.Multiline)]
    private static partial Regex Revision();
}
