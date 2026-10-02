using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>One <c>wgpuInstanceRequestAdapter</c> call, completed through <c>wgpuInstanceProcessEvents</c>.</summary>
internal sealed unsafe class AdapterRequest
{
    private const uint FeatureLevelCore = 2;
    private const uint PowerPreferenceUndefined = 0;
    private const uint CallbackModeAllowProcessEvents = 2;
    private const uint StatusSuccess = 1;

    private AdapterRequest()
    {
    }

    /// <summary>Gets a value indicating whether the callback ran.</summary>
    public bool Completed { get; private set; }

    /// <summary>Gets the <c>WGPURequestAdapterStatus</c> passed to the callback.</summary>
    public uint Status { get; private set; }

    /// <summary>Gets the adapter, owned by the caller once <see cref="Succeeded"/>.</summary>
    public nint Adapter { get; private set; }

    /// <summary>Gets the message passed to the callback.</summary>
    public string Message { get; private set; } = string.Empty;

    /// <summary>Gets a value indicating whether the callback ran with <c>WGPURequestAdapterStatus_Success</c>.</summary>
    public bool Succeeded => Completed && Status == StatusSuccess;

    /// <summary>Requests a core adapter and processes events until the callback runs or <paramref name="timeout"/> elapses.</summary>
    /// <param name="instance">A <c>WGPUInstance</c>.</param>
    /// <param name="backendType">The <c>WGPUBackendType</c> to request, <c>Undefined</c> (0) for the default.</param>
    /// <param name="timeout">How long to wait for the callback.</param>
    /// <returns>The request, <see cref="Completed"/> or not.</returns>
    public static AdapterRequest Run(nint instance, uint backendType, TimeSpan timeout)
    {
        var request = new AdapterRequest();
        var handle = GCHandle.Alloc(request);
        var options = new WGPURequestAdapterOptions
        {
            NextInChain = null,
            FeatureLevel = FeatureLevelCore,
            PowerPreference = PowerPreferenceUndefined,
            ForceFallbackAdapter = 0,
            BackendType = backendType,
            CompatibleSurface = 0,
        };
        var callbackInfo = new WGPURequestAdapterCallbackInfo
        {
            NextInChain = null,
            Mode = CallbackModeAllowProcessEvents,
            Callback = &OnRequestAdapter,
            Userdata1 = (void*)GCHandle.ToIntPtr(handle),
            Userdata2 = null,
        };
        _ = NativeMethods.wgpuInstanceRequestAdapter(instance, &options, callbackInfo);

        var stopwatch = Stopwatch.StartNew();
        while (!request.Completed && stopwatch.Elapsed < timeout)
        {
            NativeMethods.wgpuInstanceProcessEvents(instance);
            if (!request.Completed)
            {
                Thread.Sleep(1);
            }
        }

        // A pending request still calls back, with CallbackCancelled, when the instance is released:
        // its handle has to stay valid until then, so it leaks for the rest of this short process.
        if (request.Completed)
        {
            handle.Free();
        }

        return request;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnRequestAdapter(uint status, nint adapter, WGPUStringView message, void* userdata1, void* userdata2)
    {
        var request = (AdapterRequest)GCHandle.FromIntPtr((nint)userdata1).Target!;
        request.Status = status;
        request.Adapter = adapter;
        request.Message = message.ToString();
        request.Completed = true;
    }
}
