using Jade.Sdl.Raw;

namespace Jade.Sdl.Tests;

/// <summary>Checks the managed side of the generated raw layer, which needs no native library.</summary>
[TestClass]
internal sealed unsafe class RawLayerTests
{
    [TestMethod]
    public void EventsKeepTheSizeSdlAssertsForThem()
    {
        // SDL_events.h asserts that SDL_Event is as large as its 128-byte padding member.
        Assert.AreEqual(128, sizeof(Event));
        Assert.AreEqual(sizeof(EventPadding), sizeof(Event));
    }

    [TestMethod]
    public void FixedArraysHoldTheirElements()
    {
        var guid = default(Guid);
        Span<byte> data = guid.Data;

        data[15] = 0xFF;

        Assert.AreEqual(16, sizeof(Guid));
        Assert.AreEqual(16, data.Length);
        Assert.AreEqual(0xFF, guid.Data[15]);
    }

    [TestMethod]
    public void FlagsComeFromTheirMacros()
    {
        // SDL_WindowFlags is a Uint64 whose values are the SDL_WINDOW_* macros; SDLK_A is 'a'.
        Assert.AreEqual(typeof(ulong), Enum.GetUnderlyingType(typeof(WindowFlags)));
        Assert.AreEqual("Hidden, Resizable", ((WindowFlags)0x28).ToString());
        Assert.AreEqual(nameof(Keycode.A), Enum.GetName((Keycode)'a'));
    }

    [TestMethod]
    public void StringConstantsAreTerminated()
    {
        fixed (byte* hint = NativeMethods.HintVideoDriver)
        {
            Assert.AreEqual("SDL_VIDEO_DRIVER"u8.Length, NativeMethods.HintVideoDriver.Length);
            Assert.AreEqual(0, hint[NativeMethods.HintVideoDriver.Length]);
        }
    }

    [TestMethod]
    public void HandlesCompareTheirPointers()
    {
        Assert.AreEqual(new Window(1), new Window(1));
        Assert.AreNotEqual(new Window(1), new Window(2));
        Assert.AreEqual(sizeof(nint), sizeof(Window));
    }
}
