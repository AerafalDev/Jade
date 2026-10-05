using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jade.MiniAudio.Tests;

/// <summary>Checks the managed side of the generated raw layer, which needs no native library.</summary>
[TestClass]
internal sealed unsafe class RawLayerTests
{
    [TestMethod]
    public void BooleansKeepTheSizeOfTheirIntegers()
    {
        var two = 2u;

        Assert.AreEqual(sizeof(uint), sizeof(Bool32));
        Assert.AreEqual(sizeof(byte), sizeof(Bool8));
        Assert.IsTrue(Unsafe.As<uint, Bool32>(ref two));
        Assert.IsFalse(default(Bool8));
    }

    [TestMethod]
    public void SignedEnumsKeepTheirNegativeValues()
    {
        // MA_SUCCESS is 0, MA_ERROR -1 and MA_CANCELLED -51 in miniaudio.h.
        Assert.AreEqual(nameof(Result.Success), Enum.GetName(default(Result)));
        Assert.AreEqual(nameof(Result.Error), Enum.GetName((Result)(-1)));
        Assert.AreEqual(nameof(Result.Cancelled), Enum.GetName((Result)(-51)));
    }

    [TestMethod]
    public void FlagEnumsAreMarkedAsFlags()
    {
        // MA_SOUND_FLAG_STREAM and MA_SOUND_FLAG_DECODE are 0x1 and 0x2.
        Assert.AreEqual("Stream, Decode", ((SoundFlags)0x3).ToString());
    }

    [TestMethod]
    public void VectorsMapToSystemNumerics()
    {
        // ma_atomic_vec3f holds an ma_vec3f, which the configuration maps to Vector3.
        Assert.AreEqual(sizeof(float) * 3, sizeof(Vector3));
        Assert.AreEqual(typeof(Vector3), typeof(AtomicVec3f).GetField(nameof(AtomicVec3f.V))!.FieldType);
    }
}
