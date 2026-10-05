using Jade.BindingGenerator.Configuration;

namespace Jade.BindingGenerator.Targets;

/// <summary>The targets C headers are parsed for: one per runtime identifier of ADR 0012.</summary>
/// <remarks>
/// Declarations and layouts differ between platforms, so a parse for the host alone would miss the
/// API of the others (ADR 0026).
/// </remarks>
internal static class SupportedTargets
{
    /// <summary>Creates the targets, at the minimum OS versions of ADR 0024.</summary>
    /// <param name="minimumOs">The minimum OS versions pinned in <c>build/versions.json</c>.</param>
    /// <returns>The targets, in the order of ADR 0012.</returns>
    public static IReadOnlyList<Target> Create(MinimumOsVersions minimumOs)
    {
        var android = FormattableString.Invariant($"linux-android{minimumOs.AndroidApiLevel}");

        return
        [
            new("win-x64", "x86_64-pc-windows-msvc", Platforms.Windows),
            new("win-arm64", "aarch64-pc-windows-msvc", Platforms.Windows),
            new("linux-x64", "x86_64-pc-linux-gnu", Platforms.Linux),
            new("linux-arm64", "aarch64-pc-linux-gnu", Platforms.Linux),
            new("osx-arm64", $"arm64-apple-macosx{minimumOs.Macos}", Platforms.MacOS),
            new("osx-x64", $"x86_64-apple-macosx{minimumOs.Macos}", Platforms.MacOS),
            new("ios-arm64", $"arm64-apple-ios{minimumOs.Ios}", Platforms.IOS),
            new("iossimulator-arm64", $"arm64-apple-ios{minimumOs.Ios}-simulator", Platforms.IOS),
            new("iossimulator-x64", $"x86_64-apple-ios{minimumOs.Ios}-simulator", Platforms.IOS),
            new("android-arm64", $"aarch64-{android}", Platforms.Android),
            new("android-x64", $"x86_64-{android}", Platforms.Android),
            new("browser-wasm", "wasm32-unknown-emscripten", Platforms.Browser),
        ];
    }
}
