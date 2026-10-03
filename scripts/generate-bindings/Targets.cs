/// <summary>The target triples of the ADR-0007 RIDs.</summary>
internal static class Targets
{
    // Every triple parses here without its SDK: the generator parses with -nostdinc against the stub headers
    // in sysroot/, and clang's target info alone fixes type sizes, alignment and predefined platform macros.
    // macOS carries a deployment version only because SDL_platform_defines.h rejects anything below 10.7;
    // 11.0 is the first arm64 macOS. Real deployment targets belong to 103 and 104 and do not change declarations.

    /// <summary>Gets every RID built now (ADR-0007), in the order reports list them.</summary>
    public static IReadOnlyList<Target> All { get; } =
    [
        new("win-x64", "x86_64-pc-windows-msvc", "windows"),
        new("win-arm64", "aarch64-pc-windows-msvc", "windows"),
        new("linux-x64", "x86_64-unknown-linux-gnu", "linux"),
        new("linux-arm64", "aarch64-unknown-linux-gnu", "linux"),
        new("osx-x64", "x86_64-apple-macosx11.0", "macos"),
        new("osx-arm64", "arm64-apple-macosx11.0", "macos"),
        new("android-arm64", "aarch64-linux-android", "android"),
        new("android-x64", "x86_64-linux-android", "android"),
        new("ios-arm64", "arm64-apple-ios", "ios"),
        new("iossimulator-arm64", "arm64-apple-ios-simulator", "ios"),
        new("iossimulator-x64", "x86_64-apple-ios-simulator", "ios"),
        new("browser-wasm", "wasm32-unknown-emscripten", "browser"),
    ];
}
