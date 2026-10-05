using System.Runtime.InteropServices;
using Jade.Sdl.Raw;

namespace Jade.Sdl.Tests;

/// <summary>
/// Initializes SDL3's video subsystem through the generated raw layer on the host: the library built
/// by <c>scripts/build-native.cs</c> in <c>artifacts/native/bin/&lt;rid&gt;/</c> is copied next to the tests.
/// </summary>
/// <remarks>SDL3 has one video subsystem per process, so these tests never run at the same time.</remarks>
[TestClass]
[DoNotParallelize]
internal sealed unsafe class VideoSmokeTests
{
    private const string LibraryName = "SDL3";

    [TestMethod]
    public void ReportsTheVersionOfItsHeaders()
    {
        RequireNativeLibrary();

        Assert.AreEqual(NativeMethods.Version, NativeMethods.GetVersion());
    }

    [TestMethod]
    public void InitializesVideoWithTheDummyDriver()
    {
        RequireNativeLibrary();

        // A UTF-8 literal is NUL-terminated in memory, as the constants of the hints are.
        fixed (byte* hint = NativeMethods.HintVideoDriver)
        fixed (byte* driver = "dummy"u8)
        {
            Assert.IsTrue(NativeMethods.SetHint(hint, driver), GetError());

            try
            {
                Assert.IsTrue(NativeMethods.Init(InitFlags.Video), GetError());
                Assert.AreEqual("dummy", ToString(NativeMethods.GetCurrentVideoDriver()));
                CreateAndDestroyWindow();
            }
            finally
            {
                NativeMethods.Quit();
                _ = NativeMethods.ResetHint(hint);
            }
        }
    }

    [TestMethod]
    public void InitializesVideoOnTheHostDisplay()
    {
        RequireNativeLibrary();

        // Jade.Sdl declares an Environment type of its own (SDL_Environment).
        if (OperatingSystem.IsLinux() && System.Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is null && System.Environment.GetEnvironmentVariable("DISPLAY") is null)
        {
            Assert.Inconclusive("The host has no display server.");
        }

        try
        {
            Assert.IsTrue(NativeMethods.Init(InitFlags.Video), GetError());

            var driver = ToString(NativeMethods.GetCurrentVideoDriver());

            Assert.IsFalse(string.IsNullOrEmpty(driver));

            if (OperatingSystem.IsLinux())
            {
                Assert.IsTrue(driver is "wayland" or "x11", driver);
            }

            CreateAndDestroyWindow();
        }
        finally
        {
            NativeMethods.Quit();
        }
    }

    private static void CreateAndDestroyWindow()
    {
        fixed (byte* title = "Jade"u8)
        {
            var window = NativeMethods.CreateWindow(title, 64, 64, WindowFlags.Hidden);

            Assert.AreNotEqual(default, window, GetError());

            try
            {
                Assert.AreNotEqual(0u, NativeMethods.GetWindowId(window));
            }
            finally
            {
                NativeMethods.DestroyWindow(window);
            }
        }
    }

    private static string? GetError()
    {
        return ToString(NativeMethods.GetError());
    }

    private static string? ToString(byte* text)
    {
        return Marshal.PtrToStringUTF8((nint)text);
    }

    private static void RequireNativeLibrary()
    {
        // Probed like the generated imports. The library stays loaded: unloading SDL3 is not what
        // these tests exercise.
        if (!NativeLibrary.TryLoad(LibraryName, typeof(Window).Assembly, null, out _))
        {
            Assert.Inconclusive($"The {LibraryName} library is not built for this host: run 'dotnet run scripts/build-native.cs'.");
        }
    }
}
