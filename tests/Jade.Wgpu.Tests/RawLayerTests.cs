using System.Runtime.CompilerServices;

namespace Jade.Wgpu.Tests;

/// <summary>Checks the managed side of the generated raw layer, which needs no native library.</summary>
[TestClass]
internal sealed unsafe class RawLayerTests
{
    [TestMethod]
    public void ValueStructuresApplyTheirInitializerMacro()
    {
        // WGPU_EXTENT_3D_INIT and WGPU_TEXEL_COPY_BUFFER_LAYOUT_INIT in webgpu.h.
        var extent = new Extent3D();
        var layout = new TexelCopyBufferLayout();

        Assert.AreEqual(0u, extent.Width);
        Assert.AreEqual(1u, extent.Height);
        Assert.AreEqual(1u, extent.DepthOrArrayLayers);
        Assert.AreEqual(0ul, layout.Offset);
        Assert.AreEqual(NativeMethods.WGPU_COPY_STRIDE_UNDEFINED, layout.BytesPerRow);
        Assert.AreEqual(NativeMethods.WGPU_COPY_STRIDE_UNDEFINED, layout.RowsPerImage);
    }

    [TestMethod]
    public void ExtensionsIdentifyTheirStructureType()
    {
        var extension = new TextureBindingViewDimension();

        Assert.AreEqual(SType.TextureBindingViewDimension, extension.Chain.sType);
        Assert.IsTrue(extension.Chain.next is null);
    }

    [TestMethod]
    public void StringViewsDefaultToTheNullString()
    {
        var view = new WGPUStringView();

        Assert.IsTrue(view.data is null);
        Assert.AreEqual(nuint.MaxValue, view.length);
    }

    [TestMethod]
    public void BooleansTreatAnyNonZeroIntegerAsTrue()
    {
        var two = 2u;
        var nonCanonical = Unsafe.As<uint, Bool32>(ref two);

        Assert.AreEqual(sizeof(uint), sizeof(Bool32));
        Assert.IsTrue(nonCanonical);
        Assert.AreEqual(new Bool32(true), nonCanonical);
        Assert.IsFalse(default(Bool32));
    }

    [TestMethod]
    public void HandlesCompareTheirPointers()
    {
        Assert.AreEqual(new Device(1), new Device(1));
        Assert.AreNotEqual(new Device(1), new Device(2));
        Assert.AreEqual(0, default(Device).Handle);
        Assert.AreEqual(sizeof(nint), sizeof(Device));
    }
}
