using System.Runtime.InteropServices;

namespace Jade.Wgpu.Tests;

/// <summary>Checks the managed side of the generated idiomatic layer, which needs no native library.</summary>
[TestClass]
internal sealed unsafe class IdiomaticLayerTests
{
    [TestMethod]
    public void MirrorsApplyTheirInitializerMacro()
    {
        // WGPU_TEXTURE_DESCRIPTOR_INIT, WGPU_SAMPLER_DESCRIPTOR_INIT, WGPU_TEXTURE_VIEW_DESCRIPTOR_INIT
        // and WGPU_COLOR_TARGET_STATE_INIT in webgpu.h.
        var texture = new TextureDescriptor();
        var sampler = new SamplerDescriptor();
        var view = new TextureViewDescriptor();
        var target = new ColorTargetState();

        // An enum with an undefined value defaults to it, which lets the implementation pick 2D.
        Assert.AreEqual(TextureDimension.Undefined, texture.Dimension);
        Assert.AreEqual(1u, texture.MipLevelCount);
        Assert.AreEqual(1u, texture.SampleCount);
        Assert.AreEqual(1u, texture.Size.Height);
        Assert.IsTrue(texture.Label.IsNull);
        Assert.AreEqual(32f, sampler.LodMaxClamp);
        Assert.AreEqual(1, sampler.MaxAnisotropy);
        Assert.AreEqual(TextureViewDescriptor.MipLevelCountUndefined, view.MipLevelCount);
        Assert.AreEqual(TextureViewDescriptor.ArrayLayerCountUndefined, view.ArrayLayerCount);
        Assert.AreEqual(ColorWriteMask.All, target.WriteMask);
    }

    [TestMethod]
    public void PlacesConstantsOnTheirTypes()
    {
        var setVertexBuffer = typeof(RenderPassEncoder).GetMethod(nameof(RenderPassEncoder.SetVertexBuffer))!;

        Assert.AreEqual(nuint.MaxValue, GpuBuffer.WholeMapSize);
        Assert.AreEqual(GpuBuffer.WholeSize, setVertexBuffer.GetParameters()[^1].DefaultValue);
        Assert.AreEqual(Limits.LimitU32Undefined, new Limits().MaxBindGroups);
        Assert.AreEqual(Limits.LimitU64Undefined, new Limits().MaxBufferSize);
        Assert.AreEqual(RenderPassColorAttachment.DepthSliceUndefined, new RenderPassColorAttachment().DepthSlice);
        Assert.AreEqual(TexelCopyBufferLayout.CopyStrideUndefined, new TexelCopyBufferLayout().BytesPerRow);
        Assert.AreEqual(PassTimestampWrites.QuerySetIndexUndefined, new PassTimestampWrites().BeginningOfPassWriteIndex);
        Assert.IsTrue(float.IsNaN(new RenderPassDepthStencilAttachment().DepthClearValue));
    }

    [TestMethod]
    public void TextKeepsTheNullAndEmptyStringsDistinct()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);

        try
        {
            var defaultText = default(Utf8Text).Lower(null, ref arena);
            var nullString = Utf8Text.FromString(null).Lower(null, ref arena);
            var emptyString = Utf8Text.FromString(string.Empty).Lower(null, ref arena);
            var emptyUtf8 = Utf8Text.FromReadOnlySpan(""u8).Lower(null, ref arena);
            var text = Utf8Text.FromString("héllo").Lower(null, ref arena);

            Assert.IsTrue(defaultText.Data is null);
            Assert.AreEqual(Raw.NativeMethods.Strlen, defaultText.Length);
            Assert.AreEqual(Raw.NativeMethods.Strlen, nullString.Length);
            Assert.AreEqual(0u, (uint)emptyString.Length);
            Assert.AreEqual(0u, (uint)emptyUtf8.Length);
            Assert.AreEqual(6u, (uint)text.Length);
            Assert.IsNull(Utf8Text.ToManagedString(defaultText));
            Assert.AreEqual(string.Empty, Utf8Text.ToManagedString(emptyString));
            Assert.AreEqual("héllo", Utf8Text.ToManagedString(text));
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void TextPassesPinnedUtf8WithoutACopy()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);
        ReadOnlySpan<byte> bytes = "label"u8;

        try
        {
            fixed (byte* pointer = bytes)
            {
                var view = Utf8Text.FromReadOnlySpan(bytes).Lower(pointer, ref arena);

                Assert.IsTrue(view.Data == pointer);
                Assert.AreEqual(5u, (uint)view.Length);
            }
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void TheArenaAlignsAndGrowsIntoNativeMemory()
    {
        var arena = new Arena(stackalloc byte[16]);

        try
        {
            _ = arena.Allocate<byte>();

            var aligned = arena.Allocate<long>();
            var large = arena.Allocate<byte>(10_000);
            var strings = arena.CopyStrings(["a", "bc"]);

            large[9_999] = 1;

            Assert.AreEqual(0, (nint)aligned % sizeof(long));
            Assert.AreEqual("bc", Marshal.PtrToStringUTF8((nint)strings[1]));
            Assert.IsTrue(arena.Allocate<int>(0) is null);
            Assert.IsTrue(arena.Copy(ReadOnlySpan<int>.Empty) is null);
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void LowersChainedExtensions()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);

        try
        {
            var source = LowerExtension<ShaderSourceWgsl, ShaderModuleDescriptor>(new ShaderSourceWgsl { Code = "fn main() {}" }, ref arena);
            var dimension = LowerExtension<TextureBindingViewDimension, TextureDescriptor>(new TextureBindingViewDimension { ViewDimension = TextureViewDimension.Cube }, ref arena);

            Assert.AreEqual(SType.ShaderSourceWgsl, source->SType);
            Assert.IsTrue(source->Next is null);
            Assert.AreEqual("fn main() {}", Utf8Text.ToManagedString(((Raw.ShaderSourceWgsl*)source)->Code));
            Assert.AreEqual(SType.TextureBindingViewDimension, dimension->SType);
            Assert.AreEqual(TextureViewDimension.Cube, ((TextureBindingViewDimension*)dimension)->ViewDimension);
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void AllocatesAndReadsOutputExtensions()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);

        try
        {
            var chain = AllocateOutput<CompatibilityModeLimits, Limits>(ref arena);

            Assert.AreEqual(SType.CompatibilityModeLimits, chain->SType);
            Assert.AreEqual(Limits.LimitU32Undefined, ((CompatibilityModeLimits*)chain)->MaxStorageBuffersInVertexStage);

            ((CompatibilityModeLimits*)chain)->MaxStorageBuffersInVertexStage = 7;
            chain->Next = chain;

            var raised = RaiseOutput<CompatibilityModeLimits, Limits>(chain);

            Assert.AreEqual(7u, raised.MaxStorageBuffersInVertexStage);
            Assert.IsTrue(raised.Chain.Next is null);
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void LowersTheExtensionsOfNestedRoots()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);
        var texture = new ExternalTexture(2);
        var descriptor = new BindGroupDescriptor
        {
            Layout = new BindGroupLayout(1),
            Entries = [new BindGroupEntry { Binding = 0 }, new BindGroupEntry { Binding = 1 }],
            BindGroupEntryExtensions = [default, new BindGroupEntryExtensions { ExternalTextureBindingEntry = new ExternalTextureBindingEntry { ExternalTexture = texture } }],
        };

        try
        {
            Raw.BindGroupDescriptor raw;

            descriptor.Lower(&raw, ref arena);

            var chain = raw.Entries[1].NextInChain;

            Assert.AreEqual(2u, (uint)raw.EntryCount);
            Assert.AreEqual(GpuBuffer.WholeSize, raw.Entries[0].Size);
            Assert.IsTrue(raw.Entries[0].NextInChain is null);
            Assert.AreEqual(SType.ExternalTextureBindingEntry, chain->SType);
            Assert.IsTrue(chain->Next is null);
            Assert.AreEqual(texture, ((ExternalTextureBindingEntry*)chain)->ExternalTexture);
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void RejectsExtensionsThatDoNotMatchTheirElements()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);
        var descriptor = new BindGroupDescriptor
        {
            Entries = [new BindGroupEntry()],
            BindGroupEntryExtensions = [default, default],
        };
        var options = new CopyTextureForBrowserOptions { ConversionMatrix = [1f, 2f] };

        try
        {
            Raw.BindGroupDescriptor rawDescriptor;
            Raw.CopyTextureForBrowserOptions rawOptions;

            Assert.AreEqual(nameof(BindGroupDescriptor.BindGroupEntryExtensions), LowerAndCatch(descriptor, &rawDescriptor, ref arena));
            Assert.AreEqual(nameof(CopyTextureForBrowserOptions.ConversionMatrix), LowerAndCatch(options, &rawOptions, ref arena));
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void PassesADefaultNestedMirrorAsANullPointer()
    {
        var arena = new Arena(stackalloc byte[Arena.StackSize]);
        var descriptor = new RenderPipelineDescriptor { Fragment = new FragmentState { Targets = [new ColorTargetState { Format = TextureFormat.Bgra8Unorm }] } };

        try
        {
            Raw.RenderPipelineDescriptor withoutFragment;
            Raw.RenderPipelineDescriptor withFragment;

            new RenderPipelineDescriptor().Lower(&withoutFragment, ref arena);
            descriptor.Lower(&withFragment, ref arena);

            Assert.IsTrue(withoutFragment.Fragment is null);
            Assert.IsTrue(withFragment.Fragment is not null);
            Assert.AreEqual(1u, (uint)withFragment.Fragment->TargetCount);
            Assert.AreEqual(ColorWriteMask.All, withFragment.Fragment->Targets[0].WriteMask);
            Assert.AreEqual(Raw.NativeMethods.Strlen, withFragment.Fragment->EntryPoint.Length);
        }
        finally
        {
            arena.Dispose();
        }
    }

    [TestMethod]
    public void CopiesOutputStructures()
    {
        var features = stackalloc FeatureName[] { FeatureName.DepthClipControl, FeatureName.Float32Filterable };
        var vendor = "vendor"u8;

        fixed (byte* vendorPointer = vendor)
        {
            var rawFeatures = new Raw.SupportedFeatures { FeatureCount = 2, Features = features };
            var rawInfo = new Raw.AdapterInfo { Vendor = new Raw.StringView { Data = vendorPointer, Length = (nuint)vendor.Length }, VendorID = 42 };
            var copy = new SupportedFeatures(in rawFeatures);
            var info = new AdapterInfo(in rawInfo);

            CollectionAssert.AreEqual(new[] { FeatureName.DepthClipControl, FeatureName.Float32Filterable }, copy.Features.ToArray());
            Assert.AreEqual("vendor", info.Vendor);
            Assert.IsNull(info.Device);
            Assert.AreEqual(42u, info.VendorID);
        }
    }

    [TestMethod]
    public void HandlesWithoutObjectAreNotReleased()
    {
        default(Device).Dispose();
        default(Device).AddRef();
        default(GpuBuffer).Release();
    }

    [TestMethod]
    public void ErrorStatusesThrowWithTheirStatus()
    {
        WgpuException.ThrowIfFailed(Status.Success, "wgpuTestFunction");

        var exception = Assert.ThrowsExactly<WgpuException<Status>>(() => WgpuException.ThrowIfFailed(Status.Error, "wgpuTestFunction"));

        Assert.AreEqual(Status.Error, exception.Status);
        StringAssert.Contains(exception.Message, "wgpuTestFunction", StringComparison.Ordinal);
    }

    private static Raw.ChainedStruct* LowerExtension<TExtension, TRoot>(scoped in TExtension extension, scoped ref Arena arena)
        where TExtension : IChainedExtension<TExtension, TRoot>, allows ref struct
        where TRoot : allows ref struct
    {
        return TExtension.Lower(in extension, ref arena);
    }

    private static Raw.ChainedStruct* AllocateOutput<TExtension, TRoot>(scoped ref Arena arena)
        where TExtension : IChainedOutputExtension<TExtension, TRoot>
    {
        return TExtension.Allocate(ref arena);
    }

    private static TExtension RaiseOutput<TExtension, TRoot>(Raw.ChainedStruct* chain)
        where TExtension : IChainedOutputExtension<TExtension, TRoot>
    {
        return TExtension.Raise(chain);
    }

    // A ref struct cannot be captured by the lambda of Assert.Throws.
    private static string? LowerAndCatch(scoped in BindGroupDescriptor descriptor, Raw.BindGroupDescriptor* target, scoped ref Arena arena)
    {
        try
        {
            descriptor.Lower(target, ref arena);

            return null;
        }
        catch (ArgumentException exception)
        {
            return exception.ParamName;
        }
    }

    private static string? LowerAndCatch(scoped in CopyTextureForBrowserOptions options, Raw.CopyTextureForBrowserOptions* target, scoped ref Arena arena)
    {
        try
        {
            options.Lower(target, ref arena);

            return null;
        }
        catch (ArgumentException exception)
        {
            return exception.ParamName;
        }
    }
}
