using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Jade.Wgpu.Tests;

/// <summary>
/// Calls the native library through the generated raw layer on the host: the library built by
/// <c>scripts/build-native.cs</c> in <c>artifacts/native/bin/&lt;rid&gt;/</c> is copied next to the tests.
/// </summary>
[TestClass]
internal sealed unsafe class InstanceSmokeTests
{
    private const string LibraryName = "webgpu_dawn";

    [TestMethod]
    public void CreatesAnInstance()
    {
        RequireNativeLibrary();

        var descriptor = new WGPUInstanceDescriptor();
        var instance = NativeMethods.wgpuCreateInstance(&descriptor);

        Assert.AreNotEqual(default, instance);

        NativeMethods.wgpuInstanceRelease(instance);
    }

    [TestMethod]
    public void ListsTheInstanceFeatures()
    {
        RequireNativeLibrary();

        var features = new WGPUSupportedInstanceFeatures();

        NativeMethods.wgpuGetInstanceFeatures(&features);

        try
        {
            var names = new ReadOnlySpan<InstanceFeatureName>(features.features, checked((int)features.featureCount));

            Assert.Contains(InstanceFeatureName.TimedWaitAny, names.ToArray());
            Assert.IsTrue(NativeMethods.wgpuHasInstanceFeature(InstanceFeatureName.TimedWaitAny));
        }
        finally
        {
            NativeMethods.wgpuSupportedInstanceFeaturesFreeMembers(features);
        }
    }

    [TestMethod]
    public void RequestsAnAdapter()
    {
        RequireNativeLibrary();

        // Waiting with a timeout needs the timed wait feature.
        var requiredFeatures = stackalloc InstanceFeatureName[] { InstanceFeatureName.TimedWaitAny };
        var descriptor = new WGPUInstanceDescriptor { requiredFeatureCount = 1, requiredFeatures = requiredFeatures };
        var instance = NativeMethods.wgpuCreateInstance(&descriptor);
        var request = new AdapterRequest();
        var requestHandle = GCHandle.Alloc(request);

        Assert.AreNotEqual(default, instance);

        try
        {
            var options = new RequestAdapterOptions();
            var callbackInfo = new WGPURequestAdapterCallbackInfo
            {
                mode = CallbackMode.WaitAnyOnly,
                callback = &OnAdapterRequested,
                userdata1 = (void*)GCHandle.ToIntPtr(requestHandle),
            };
            var wait = new FutureWaitInfo { Future = NativeMethods.wgpuInstanceRequestAdapter(instance, &options, callbackInfo) };

            Assert.AreEqual(WaitStatus.Success, NativeMethods.wgpuInstanceWaitAny(instance, 1, &wait, ulong.MaxValue));
            Assert.IsTrue(wait.Completed);
            Assert.AreEqual(1, request.Calls);

            // A host without a usable GPU driver has no adapter, which is not a binding failure.
            Assert.IsTrue(request.Status is RequestAdapterStatus.Success or RequestAdapterStatus.Unavailable, $"{request.Status}: {request.Message}");

            if (request.Status == RequestAdapterStatus.Success)
            {
                CheckAdapterInfo(request.Adapter);
            }
        }
        finally
        {
            if (request.Adapter != default)
            {
                NativeMethods.wgpuAdapterRelease(request.Adapter);
            }

            requestHandle.Free();
            NativeMethods.wgpuInstanceRelease(instance);
        }
    }

    private static void CheckAdapterInfo(Adapter adapter)
    {
        var info = new WGPUAdapterInfo();

        Assert.AreEqual(Status.Success, NativeMethods.wgpuAdapterGetInfo(adapter, &info));

        try
        {
            Assert.AreNotEqual(BackendType.Undefined, info.backendType);
            Assert.IsFalse(string.IsNullOrEmpty(ToString(info.device)));
        }
        finally
        {
            NativeMethods.wgpuAdapterInfoFreeMembers(info);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnAdapterRequested(RequestAdapterStatus status, Adapter adapter, WGPUStringView message, void* userdata1, void* userdata2)
    {
        var request = (AdapterRequest)GCHandle.FromIntPtr((nint)userdata1).Target!;

        request.Calls++;
        request.Status = status;
        request.Adapter = adapter;
        request.Message = ToString(message);
    }

    private static string? ToString(WGPUStringView view)
    {
        return view.data is null
            ? null
            : view.length == NativeMethods.WGPU_STRLEN
                ? Marshal.PtrToStringUTF8((nint)view.data)
                : Encoding.UTF8.GetString(view.data, checked((int)view.length));
    }

    private static void RequireNativeLibrary()
    {
        // Probed like the generated imports. The library stays loaded: unloading Dawn is not what
        // these tests exercise.
        if (!NativeLibrary.TryLoad(LibraryName, typeof(Instance).Assembly, null, out _))
        {
            Assert.Inconclusive($"The {LibraryName} library is not built for this host: run 'dotnet run scripts/build-native.cs'.");
        }
    }

    private sealed class AdapterRequest
    {
        public int Calls { get; set; }

        public RequestAdapterStatus Status { get; set; }

        public Adapter Adapter { get; set; }

        public string? Message { get; set; }
    }
}
