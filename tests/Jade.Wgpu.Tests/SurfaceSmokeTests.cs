namespace Jade.Wgpu.Tests;

/// <summary>Clears the surface of a window through the idiomatic layer only; SDL3 only provides the window.</summary>
/// <remarks>SDL3 has one video subsystem per process, so the test never runs at the same time as another.</remarks>
[TestClass]
[DoNotParallelize]
internal sealed class SurfaceSmokeTests
{
    private const int Frames = 3;

    [TestMethod]
    public void ClearsASurface()
    {
        // The device destroys the swap chain of the surface after the queue's work, at the latest
        // when it is released, and the driver then uses the window: the window goes last.
        using var window = TestWindow.Create(320, 240);
        using var gpu = GpuContext.Create();
        using var surface = CreateSurface(gpu.Instance, window.Source);
        var capabilities = surface.GetCapabilities(gpu.Adapter);

        Assert.IsFalse(capabilities.Formats.IsEmpty);
        Assert.IsTrue(capabilities.Usages.HasFlag(TextureUsage.RenderAttachment));

        // FIFO is always supported, but waits for a frame of a visible window: prefer a mode that does not.
        var presentMode = capabilities.PresentModes.Contains(PresentMode.Mailbox) ? PresentMode.Mailbox
            : capabilities.PresentModes.Contains(PresentMode.Immediate) ? PresentMode.Immediate
            : PresentMode.Fifo;

        surface.Configure(new SurfaceConfiguration
        {
            Device = gpu.Device,
            Format = capabilities.Formats[0],
            Width = window.Width,
            Height = window.Height,
            PresentMode = presentMode,
        });

        gpu.Device.PushErrorScope(ErrorFilter.Validation);

        for (var frame = 0; frame < Frames; frame++)
        {
            window.PumpEvents();

            var surfaceTexture = surface.GetCurrentTexture();

            Assert.IsTrue(surfaceTexture.Status is SurfaceGetCurrentTextureStatus.SuccessOptimal or SurfaceGetCurrentTextureStatus.SuccessSuboptimal, $"{surfaceTexture.Status}");

            using var texture = surfaceTexture.Texture;
            using var view = texture.CreateView();
            using var encoder = gpu.Device.CreateCommandEncoder();

            using (var pass = encoder.BeginRenderPass(new RenderPassDescriptor
            {
                Label = "clear"u8,
                ColorAttachments = [new RenderPassColorAttachment { View = view, LoadOp = LoadOp.Clear, StoreOp = StoreOp.Store, ClearValue = new Color { R = 0.1, G = 0.6, B = 0.3, A = 1 } }],
            }))
            {
                pass.End();
            }

            using var commands = encoder.Finish();

            gpu.Queue.Submit(commands);
            surface.Present();
        }

        var error = gpu.Wait(gpu.Device.PopErrorScopeAsync());

        Assert.AreEqual(ErrorType.NoError, error.Type, error.Message);
        surface.Unconfigure();
        gpu.AssertNoUncapturedError();
    }

    private static Surface CreateSurface(Instance instance, TestWindow.SurfaceSource source)
    {
        var descriptor = new SurfaceDescriptor { Label = "window"u8 };

        return source.Kind switch
        {
            TestWindow.SurfaceSourceKind.Wayland => instance.CreateSurface(descriptor, new SurfaceSourceWaylandSurface { Display = source.First, Surface = source.Second }),
            TestWindow.SurfaceSourceKind.Xlib => instance.CreateSurface(descriptor, new SurfaceSourceXlibWindow { Display = source.First, Window = source.Window }),
            TestWindow.SurfaceSourceKind.Windows => instance.CreateSurface(descriptor, new SurfaceSourceWindowsHwnd { Hinstance = source.First, Hwnd = source.Second }),
            TestWindow.SurfaceSourceKind.Metal => instance.CreateSurface(descriptor, new SurfaceSourceMetalLayer { Layer = source.First }),
            _ => throw new ArgumentOutOfRangeException(nameof(source), source.Kind, "Unknown surface source."),
        };
    }
}
