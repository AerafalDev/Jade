using System.Runtime.InteropServices;

namespace Jade.NativeBuild.Build;

/// <summary>A runtime identifier the native build supports, and how xmake names it.</summary>
/// <param name="RuntimeIdentifier">The .NET runtime identifier, such as <c>linux-x64</c>.</param>
/// <param name="XmakePlatform">The xmake platform (<c>--plat</c>).</param>
/// <param name="XmakeArchitecture">The xmake architecture (<c>--arch</c>).</param>
/// <param name="Libraries">The libraries the build produces for this target, which the packages ship.</param>
/// <param name="TestLibraries">The libraries that only the tests load: the layout libraries (ADR 0036).</param>
internal sealed record NativeTarget(string RuntimeIdentifier, string XmakePlatform, string XmakeArchitecture, IReadOnlyList<ExpectedLibrary> Libraries, IReadOnlyList<ExpectedLibrary> TestLibraries)
{
    /// <summary>Gets the target of the machine the build runs on.</summary>
    /// <returns>The host's target, or <see langword="null"/> when the build has no definition for the host yet.</returns>
    /// <remarks>Only <c>linux-x64</c> is defined; the other runtime identifiers of ADR 0012 come with roadmap task 10.</remarks>
    public static NativeTarget? ForHost()
    {
        return OperatingSystem.IsLinux() && RuntimeInformation.OSArchitecture == Architecture.X64
            ? new NativeTarget(
                "linux-x64",
                "linux",
                "x86_64",
                [
                    new ExpectedLibrary("libwebgpu_dawn.so", ["wgpuCreateInstance", "wgpuGetProcAddress"]),
                    new ExpectedLibrary("libSDL3.so", ["SDL_Init", "SDL_CreateWindow"]),
                    new ExpectedLibrary("libminiaudio.so", ["ma_context_init", "ma_engine_init", "jade_ma_device_alloc"]),
                ],
                [
                    new ExpectedLibrary("libjade_miniaudio_layout.so", ["jade_miniaudio_layout"]),
                    new ExpectedLibrary("libjade_sdl_layout.so", ["jade_sdl_layout"]),
                    new ExpectedLibrary("libjade_wgpu_layout.so", ["jade_wgpu_layout"]),
                ])
            : null;
    }
}
