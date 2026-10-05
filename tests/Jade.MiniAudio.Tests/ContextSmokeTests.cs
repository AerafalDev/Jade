using System.Numerics;
using System.Runtime.InteropServices;
using Jade.MiniAudio.Raw;

namespace Jade.MiniAudio.Tests;

/// <summary>
/// Calls miniaudio through the generated raw layer on the host: the library built by
/// <c>scripts/build-native.cs</c> in <c>artifacts/native/bin/&lt;rid&gt;/</c> is copied next to the tests.
/// </summary>
[TestClass]
internal sealed unsafe class ContextSmokeTests
{
    private const string LibraryName = "miniaudio";

    [TestMethod]
    public void ReportsTheVersionOfItsHeader()
    {
        RequireNativeLibrary();

        Assert.AreEqual(Marshal.PtrToStringUTF8((nint)NativeMethods.GetVersionString()), System.Text.Encoding.UTF8.GetString(NativeMethods.VersionString));
    }

    [TestMethod]
    public void InitializesAContext()
    {
        RequireNativeLibrary();

        // ma_context is opaque: its layout depends on the platform, so the shim allocates it.
        var context = NativeMethods.ContextAlloc();

        Assert.AreNotEqual(default, context);

        try
        {
            // Without a list of backends, miniaudio tries its defaults, down to the null backend.
            Assert.AreEqual(Result.Success, NativeMethods.ContextInit(null, 0, null, context));
            Assert.AreEqual(Result.Success, NativeMethods.ContextUninit(context));
        }
        finally
        {
            NativeMethods.ContextFree(context);
        }
    }

    [TestMethod]
    public void ReturnsVectorsByValue()
    {
        RequireNativeLibrary();

        // ma_vec3f maps to Vector3: the position comes back in registers, as the C calling convention returns three floats.
        var config = NativeMethods.SpatializerListenerConfigInit(2);
        var listener = default(SpatializerListener);

        Assert.AreEqual(Result.Success, NativeMethods.SpatializerListenerInit(&config, null, &listener));

        try
        {
            NativeMethods.SpatializerListenerSetPosition(&listener, 1.5f, -2.25f, 3.125f);

            Assert.AreEqual(new Vector3(1.5f, -2.25f, 3.125f), NativeMethods.SpatializerListenerGetPosition(&listener));
        }
        finally
        {
            NativeMethods.SpatializerListenerUninit(&listener, null);
        }
    }

    private static void RequireNativeLibrary()
    {
        // Probed like the generated imports. The library stays loaded: unloading miniaudio is not
        // what these tests exercise.
        if (!NativeLibrary.TryLoad(LibraryName, typeof(Context).Assembly, null, out _))
        {
            Assert.Inconclusive($"The {LibraryName} library is not built for this host: run 'dotnet run scripts/build-native.cs'.");
        }
    }
}
