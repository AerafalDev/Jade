using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jade.Wgpu.Raw;

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

        var descriptor = new InstanceDescriptor();
        var instance = NativeMethods.CreateInstance(&descriptor);

        Assert.AreNotEqual(default, instance);

        NativeMethods.InstanceRelease(instance);
    }

    [TestMethod]
    public void ListsTheInstanceFeatures()
    {
        RequireNativeLibrary();

        var features = new SupportedInstanceFeatures();

        NativeMethods.GetInstanceFeatures(&features);

        try
        {
            var names = new ReadOnlySpan<InstanceFeatureName>(features.Features, checked((int)features.FeatureCount));

            Assert.Contains(InstanceFeatureName.TimedWaitAny, names.ToArray());
            Assert.IsTrue(NativeMethods.HasInstanceFeature(InstanceFeatureName.TimedWaitAny));
        }
        finally
        {
            NativeMethods.SupportedInstanceFeaturesFreeMembers(features);
        }
    }

    [TestMethod]
    public void RequestsAnAdapter()
    {
        RequireNativeLibrary();

        // Waiting with a timeout needs the timed wait feature.
        var requiredFeatures = stackalloc InstanceFeatureName[] { InstanceFeatureName.TimedWaitAny };
        var descriptor = new InstanceDescriptor { RequiredFeatureCount = 1, RequiredFeatures = requiredFeatures };
        var instance = NativeMethods.CreateInstance(&descriptor);
        var request = new AdapterRequest();
        var requestHandle = GCHandle.Alloc(request);

        Assert.AreNotEqual(default, instance);

        try
        {
            var options = new RequestAdapterOptions();
            var callbackInfo = new RequestAdapterCallbackInfo
            {
                Mode = CallbackMode.WaitAnyOnly,
                Callback = &OnAdapterRequested,
                Userdata1 = (void*)GCHandle.ToIntPtr(requestHandle),
            };
            var wait = new FutureWaitInfo { Future = NativeMethods.InstanceRequestAdapter(instance, &options, callbackInfo) };

            Assert.AreEqual(WaitStatus.Success, NativeMethods.InstanceWaitAny(instance, 1, &wait, ulong.MaxValue));
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
                NativeMethods.AdapterRelease(request.Adapter);
            }

            requestHandle.Free();
            NativeMethods.InstanceRelease(instance);
        }
    }

    private static void CheckAdapterInfo(Adapter adapter)
    {
        var info = new AdapterInfo();

        Assert.AreEqual(Status.Success, NativeMethods.AdapterGetInfo(adapter, &info));

        try
        {
            Assert.AreNotEqual(BackendType.Undefined, info.BackendType);
            Assert.IsFalse(string.IsNullOrEmpty(ToString(info.Device)));
        }
        finally
        {
            NativeMethods.AdapterInfoFreeMembers(info);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnAdapterRequested(RequestAdapterStatus status, Adapter adapter, StringView message, void* userdata1, void* userdata2)
    {
        var request = (AdapterRequest)GCHandle.FromIntPtr((nint)userdata1).Target!;

        request.Calls++;
        request.Status = status;
        request.Adapter = adapter;
        request.Message = ToString(message);
    }

    private static string? ToString(StringView view)
    {
        return view.Data is null
            ? null
            : view.Length == NativeMethods.Strlen
                ? Marshal.PtrToStringUTF8((nint)view.Data)
                : Encoding.UTF8.GetString(view.Data, checked((int)view.Length));
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
