using System.Collections.Concurrent;

namespace Jade.Wgpu.Tests;

/// <summary>
/// Drives the host's GPU through the idiomatic layer only: adapter and device requests, buffer
/// transfers, rendering, error scopes and device loss.
/// </summary>
[TestClass]
internal sealed class DeviceSmokeTests
{
    // A triangle over the lower-left half of the target, in red.
    private const string TriangleShader = """
        @vertex
        fn vs(@builtin(vertex_index) index: u32) -> @builtin(position) vec4f {
            var positions = array<vec2f, 3>(vec2f(-1.0, -1.0), vec2f(1.0, -1.0), vec2f(-1.0, 1.0));
            return vec4f(positions[index], 0.0, 1.0);
        }

        @fragment
        fn fs() -> @location(0) vec4f {
            return vec4f(1.0, 0.0, 0.0, 1.0);
        }
        """;

    private const uint TargetSize = 4;

    // Rows of a texture copied to a buffer are aligned to 256 bytes.
    private const uint BytesPerRow = 256;

    [TestMethod]
    public void RequestsAnAdapterAndADevice()
    {
        using var gpu = GpuContext.Create();

        var info = gpu.Adapter.GetInfo(out DawnAdapterPropertiesPowerPreference powerPreference);
        var limits = gpu.Adapter.GetLimits(out CompatibilityModeLimits compatibilityLimits);
        var features = gpu.Adapter.GetFeatures();

        Assert.AreNotEqual(BackendType.Undefined, info.BackendType);
        Assert.IsFalse(string.IsNullOrEmpty(info.Device));
        Assert.IsTrue(Enum.IsDefined(powerPreference.PowerPreference), $"{powerPreference.PowerPreference}");
        Assert.IsGreaterThanOrEqualTo(2048u, limits.MaxTextureDimension2D);
        Assert.AreNotEqual(Limits.LimitU32Undefined, compatibilityLimits.MaxStorageBuffersInVertexStage);
        Assert.IsTrue(features.Features.All(gpu.Adapter.HasFeature));
        Assert.AreEqual(info.Device, gpu.Device.GetAdapterInfo().Device);
        Assert.AreNotEqual(default, gpu.Queue);
        gpu.AssertNoUncapturedError();
    }

    [TestMethod]
    public void WritesAndReadsBackABuffer()
    {
        using var gpu = GpuContext.Create();

        ReadOnlySpan<uint> data = [0x0102_0304, 0x0506_0708, 0x090A_0B0C, 0x0D0E_0F10];
        using var source = gpu.Device.CreateBuffer(new BufferDescriptor
        {
            Label = "source",
            Usage = BufferUsage.CopySrc | BufferUsage.CopyDst,
            Size = 16,
        });
        using var readback = gpu.Device.CreateBuffer(new BufferDescriptor
        {
            Label = "readback"u8,
            Usage = BufferUsage.MapRead | BufferUsage.CopyDst,
            Size = 16,
        });

        gpu.Queue.WriteBuffer(source, 0, data);

        using (var encoder = gpu.Device.CreateCommandEncoder())
        {
            encoder.CopyBufferToBuffer(source, 0, readback, 0, 16);

            using var commands = encoder.Finish();

            gpu.Queue.Submit(commands);
        }

        gpu.Wait(readback.MapAsync(MapMode.Read, 0, 16));

        Assert.AreEqual(BufferMapState.Mapped, readback.MapState);
        CollectionAssert.AreEqual(data.ToArray(), System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(readback.GetConstMappedRange()).ToArray());
        readback.Unmap();
        gpu.AssertNoUncapturedError();
    }

    [TestMethod]
    public void WritesABufferMappedAtCreation()
    {
        using var gpu = GpuContext.Create();
        using var buffer = gpu.Device.CreateBuffer(new BufferDescriptor
        {
            Usage = BufferUsage.CopySrc,
            Size = 8,
            MappedAtCreation = true,
        });
        var range = buffer.GetMappedRange();

        Assert.AreEqual(8, range.Length);
        range.Fill(0xAB);
        buffer.Unmap();
        Assert.AreEqual(8ul, buffer.Size);
        gpu.AssertNoUncapturedError();
    }

    [TestMethod]
    public void RendersATriangleIntoATexture()
    {
        using var gpu = GpuContext.Create();
        using var shader = gpu.Device.CreateShaderModule(new ShaderModuleDescriptor { Label = "triangle"u8 }, new ShaderSourceWgsl { Code = TriangleShader });
        using var pipeline = gpu.Wait(gpu.Device.CreateRenderPipelineAsync(new RenderPipelineDescriptor
        {
            Label = "triangle"u8,
            Vertex = new VertexState { Module = shader, EntryPoint = "vs"u8 },
            Fragment = new FragmentState { Module = shader, EntryPoint = "fs", Targets = [new ColorTargetState { Format = TextureFormat.Rgba8Unorm }] },
        }));
        using var target = gpu.Device.CreateTexture(new TextureDescriptor
        {
            Usage = TextureUsage.RenderAttachment | TextureUsage.CopySrc,
            Size = new Extent3D { Width = TargetSize, Height = TargetSize },
            Format = TextureFormat.Rgba8Unorm,
        });
        using var view = target.CreateView();
        using var readback = gpu.Device.CreateBuffer(new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.CopyDst, Size = BytesPerRow * TargetSize });

        Assert.AreEqual(TargetSize, target.Width);
        Assert.AreEqual(TextureDimension.Dimension2D, target.Dimension);

        using (var encoder = gpu.Device.CreateCommandEncoder(new CommandEncoderDescriptor { Label = "frame" }))
        {
            using (var pass = encoder.BeginRenderPass(new RenderPassDescriptor
            {
                ColorAttachments = [new RenderPassColorAttachment { View = view, LoadOp = LoadOp.Clear, StoreOp = StoreOp.Store, ClearValue = new Color { B = 1, A = 1 } }],
            }))
            {
                pass.SetPipeline(pipeline);
                pass.Draw(3);
                pass.End();
            }

            encoder.CopyTextureToBuffer(
                new TexelCopyTextureInfo { Texture = target },
                new TexelCopyBufferInfo { Buffer = readback, Layout = new TexelCopyBufferLayout { BytesPerRow = BytesPerRow } },
                new Extent3D { Width = TargetSize, Height = TargetSize });

            using var commands = encoder.Finish();

            gpu.Queue.Submit(commands);
        }

        gpu.Wait(gpu.Queue.OnSubmittedWorkDoneAsync());
        gpu.Wait(readback.MapAsync(MapMode.Read, 0, BytesPerRow * TargetSize));

        var pixels = readback.GetConstMappedRange();

        // The bottom-left pixel is inside the triangle, the top-right one only cleared.
        CollectionAssert.AreEqual(new byte[] { 255, 0, 0, 255 }, pixels.Slice((int)(BytesPerRow * (TargetSize - 1)), 4).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0, 0, 255, 255 }, pixels.Slice((int)((TargetSize - 1) * 4), 4).ToArray());
        readback.Unmap();
        gpu.AssertNoUncapturedError();
    }

    [TestMethod]
    public void CapturesValidationErrorsInScopes()
    {
        using var gpu = GpuContext.Create();

        gpu.Device.PushErrorScope(ErrorFilter.Validation);

        // Mapping for reading and for writing at once is invalid.
        using (gpu.Device.CreateBuffer(new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.MapWrite, Size = 4 }))
        {
            var error = gpu.Wait(gpu.Device.PopErrorScopeAsync());

            Assert.AreEqual(ErrorType.Validation, error.Type);
            Assert.IsFalse(string.IsNullOrEmpty(error.Message));
        }

        gpu.Device.PushErrorScope(ErrorFilter.Validation);

        using (gpu.Device.CreateBuffer(new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.CopyDst, Size = 4 }))
        {
            Assert.AreEqual(ErrorType.NoError, gpu.Wait(gpu.Device.PopErrorScopeAsync()).Type);
        }

        gpu.AssertNoUncapturedError();
    }

    [TestMethod]
    public void ReportsUncapturedErrors()
    {
        using var gpu = GpuContext.Create();
        using var buffer = gpu.Device.CreateBuffer(new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.MapWrite, Size = 4 });

        gpu.Instance.ProcessEvents();

        Assert.IsTrue(gpu.UncapturedErrors.TryDequeue(out var error));
        Assert.AreEqual(ErrorType.Validation, error.Type);
    }

    [TestMethod]
    public void ReportsTheLossOfADestroyedDevice()
    {
        GpuContext.RequireNativeLibrary();

        using var instance = Instance.Create();
        Adapter adapter;

        try
        {
            adapter = GpuContext.Wait(instance, instance.RequestAdapterAsync());
        }
        catch (WgpuException<RequestAdapterStatus> exception) when (exception.Status == RequestAdapterStatus.Unavailable)
        {
            Assert.Inconclusive($"The host has no adapter: {exception.Message}");
            throw;
        }

        var losses = new ConcurrentQueue<DeviceLostReason>();

        try
        {
            var device = GpuContext.Wait(instance, adapter.RequestDeviceAsync(new DeviceDescriptor { DeviceLost = (_, reason, _) => losses.Enqueue(reason) }));

            device.Destroy();
            device.Release();
            instance.ProcessEvents();
        }
        finally
        {
            adapter.Release();
        }

        Assert.AreEqual(1, losses.Count);
        Assert.IsTrue(losses.TryPeek(out var lossReason));
        Assert.AreEqual(DeviceLostReason.Destroyed, lossReason);
    }

    [TestMethod]
    public void FailsATaskWhenTheCallbackReportsAFailure()
    {
        using var gpu = GpuContext.Create();
        using var buffer = gpu.Device.CreateBuffer(new BufferDescriptor { Usage = BufferUsage.CopyDst, Size = 4 });

        // A buffer without a map usage cannot be mapped.
        gpu.Device.PushErrorScope(ErrorFilter.Validation);

        var mapping = buffer.MapAsync(MapMode.Read, 0, 4);

        _ = gpu.Wait(gpu.Device.PopErrorScopeAsync());

        var exception = Assert.ThrowsExactly<WgpuException<MapAsyncStatus>>(() => gpu.Wait(mapping));

        Assert.AreEqual(MapAsyncStatus.Error, exception.Status);
        StringAssert.Contains(exception.Message, "wgpuBufferMapAsync", StringComparison.Ordinal);
    }
}
