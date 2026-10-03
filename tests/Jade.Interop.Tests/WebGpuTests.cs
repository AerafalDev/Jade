using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Jade.Interop.WebGpu;
using Shouldly;
using Xunit;

namespace Jade.Interop.Tests;

// Every test creates and releases its own instance, so the tests share no WebGPU state. Callbacks use
// CallbackMode.WaitAnyOnly: they only run inside Instance.WaitAny, on the test's thread.
public sealed unsafe class WebGpuTests
{
    private const string NotStaged = "jade_native is not staged for this RID; run `dotnet scripts/build-native.cs` and rebuild the tests.";

    // WaitAny with a timeout needs InstanceFeatureName.TimedWaitAny; a bounded one keeps a broken driver from hanging CI.
    private const ulong TimeoutNanoseconds = 30_000_000_000;

    private static readonly bool s_staged = NativeLibrary.TryLoad("jade_native", typeof(Wgpu).Assembly, null, out _);

    /// <summary>Gets the backends to request: the default one needs a GPU, Dawn's Null backend answers without one.</summary>
    public static TheoryData<BackendType> Backends => [BackendType.Undefined, BackendType.Null];

    [Theory]
    [MemberData(nameof(Backends))]
    public void A_device_writes_a_buffer_through_the_span_overload(BackendType backend)
    {
        Assert.SkipUnless(s_staged, NotStaged);
        var instance = CreateInstance();
        try
        {
            var adapter = RequestAdapter(instance, backend);
            try
            {
                var info = default(AdapterInfo);
                adapter.GetInfo(ref info).ShouldBe(Status.Success);
                if (backend != BackendType.Undefined)
                {
                    info.BackendType.ShouldBe(backend);
                }

                Wgpu.AdapterInfoFreeMembers(info);
                WriteAndReadBack(instance, RequestDevice(instance, adapter));
            }
            finally
            {
                adapter.Release();
            }
        }
        finally
        {
            instance.Release();
        }
    }

    [Fact]
    public void A_chained_WGSL_module_is_valid_and_SPIR_V_ones_are_rejected()
    {
        Assert.SkipUnless(s_staged, NotStaged);
        Wgpu.HasInstanceFeature(InstanceFeatureName.ShaderSourceSPIRV).ShouldNotBe(0u);
        var instance = CreateInstance();
        try
        {
            var adapter = RequestAdapter(instance, BackendType.Null);
            var device = RequestDevice(instance, adapter);
            try
            {
                var wgsl = "@compute @workgroup_size(1) fn main() {}"u8;
                fixed (byte* code = wgsl)
                {
                    var source = new ShaderSourceWGSL { Code = new StringView { Data = code, Length = (nuint)wgsl.Length } };
                    source.Chain.SType.ShouldBe(SType.ShaderSourceWGSL);
                    CreateShaderModule(instance, device, &source.Chain).ShouldBe((ErrorType.NoError, string.Empty));
                }

                // The SPIR-V module header alone: magic number, version 1.0, generator, ID bound, schema.
                ReadOnlySpan<uint> spirv = [0x07230203, 0x00010000, 0, 1, 0];
                fixed (uint* words = spirv)
                {
                    var source = new ShaderSourceSPIRV { CodeSize = (uint)spirv.Length, Code = words };
                    var (type, message) = CreateShaderModule(instance, device, &source.Chain);
                    type.ShouldBe(ErrorType.Validation);
                    message.ShouldContain("SPIR-V is disallowed");

                    var dawnSource = new DawnShaderSourceSPIRV { CodeSize = (nuint)spirv.Length, Code = words };
                    (type, message) = CreateShaderModule(instance, device, &dawnSource.Chain);
                    type.ShouldBe(ErrorType.Validation);
                    message.ShouldContain("SPIR-V is disallowed");
                }
            }
            finally
            {
                device.Release();
                adapter.Release();
            }
        }
        finally
        {
            instance.Release();
        }
    }

    [Fact]
    public void StringView_AsSpan_reads_sized_terminated_null_and_empty_views()
    {
        var text = "jade\0tail"u8;
        fixed (byte* data = text)
        {
            new StringView { Data = data, Length = 3 }.AsSpan().SequenceEqual("jad"u8).ShouldBeTrue();
            new StringView { Data = data, Length = Wgpu.Strlen }.AsSpan().SequenceEqual("jade"u8).ShouldBeTrue();
        }

        StringView.Null.AsSpan().IsEmpty.ShouldBeTrue();
        StringView.Null.Length.ShouldBe(nuint.MaxValue);
        default(StringView).AsSpan().IsEmpty.ShouldBeTrue();
    }

    // The buffer is written by the queue and mapped back without a copy: Dawn's Null backend runs no command buffer
    // (null::Queue::SubmitImpl), but writes and maps the buffer's memory.
    private static void WriteAndReadBack(Instance instance, Device device)
    {
        var queue = device.GetQueue();
        ReadOnlySpan<uint> values = [0xDEADBEEF, 1, 2, 3];
        var size = (ulong)(values.Length * sizeof(uint));
        var buffer = device.CreateBuffer(new BufferDescriptor { Usage = BufferUsage.MapRead | BufferUsage.CopyDst, Size = size });
        try
        {
            buffer.SetLabel("jade readback"u8);
            device.PushErrorScope(ErrorFilter.Validation);
            queue.WriteBuffer(buffer, 0, values);
            PopErrorScope(instance, device).ShouldBe((ErrorType.NoError, string.Empty));

            var map = new Completion();
            var handle = GCHandle.Alloc(map);
            try
            {
                var callback = new BufferMapCallbackInfo { Mode = CallbackMode.WaitAnyOnly, Callback = &OnMapped, Userdata1 = (void*)GCHandle.ToIntPtr(handle) };
                Wait(instance, buffer.MapAsync(MapMode.Read, 0, (nuint)size, callback));
            }
            finally
            {
                handle.Free();
            }

            map.Status.ShouldBe((int)MapAsyncStatus.Success, map.Message);
            new ReadOnlySpan<uint>(buffer.GetConstMappedRange(0, (nuint)size), values.Length).ToArray().ShouldBe(values.ToArray());
            buffer.Unmap();
        }
        finally
        {
            buffer.Release();
            queue.Release();
            device.Release();
        }
    }

    private static Instance CreateInstance()
    {
        var feature = InstanceFeatureName.TimedWaitAny;
        var instance = Wgpu.CreateInstance(new InstanceDescriptor { RequiredFeatureCount = 1, RequiredFeatures = &feature });
        instance.IsNull.ShouldBeFalse();
        return instance;
    }

    private static Adapter RequestAdapter(Instance instance, BackendType backend)
    {
        var request = new Completion();
        var handle = GCHandle.Alloc(request);
        try
        {
            var callback = new RequestAdapterCallbackInfo { Mode = CallbackMode.WaitAnyOnly, Callback = &OnAdapter, Userdata1 = (void*)GCHandle.ToIntPtr(handle) };
            Wait(instance, instance.RequestAdapter(new RequestAdapterOptions { BackendType = backend }, callback));
        }
        finally
        {
            handle.Free();
        }

        // Without a GPU, a default request still gets Dawn's Null adapter (InstanceBase::EnumeratePhysicalDevices tries
        // every backend), so this only skips where jade_native has no adapter at all for the request.
        if (request.Status != (int)RequestAdapterStatus.Success)
        {
            var name = backend == BackendType.Undefined ? "default" : backend.ToString();
            Assert.Skip($"No {name} adapter is available here (no GPU, driver or backend for it): {(RequestAdapterStatus)request.Status}, {request.Message}");
        }

        return new Adapter(request.Handle);
    }

    private static Device RequestDevice(Instance instance, Adapter adapter)
    {
        var request = new Completion();
        var handle = GCHandle.Alloc(request);
        try
        {
            var callback = new RequestDeviceCallbackInfo { Mode = CallbackMode.WaitAnyOnly, Callback = &OnDevice, Userdata1 = (void*)GCHandle.ToIntPtr(handle) };
            Wait(instance, adapter.RequestDevice(new DeviceDescriptor(), callback));
        }
        finally
        {
            handle.Free();
        }

        request.Status.ShouldBe((int)RequestDeviceStatus.Success, request.Message);
        return new Device(request.Handle);
    }

    private static (ErrorType Type, string Message) CreateShaderModule(Instance instance, Device device, ChainedStruct* source)
    {
        device.PushErrorScope(ErrorFilter.Validation);
        var module = device.CreateShaderModule(new ShaderModuleDescriptor { NextInChain = source });
        var error = PopErrorScope(instance, device);
        module.Release();
        return error;
    }

    private static (ErrorType Type, string Message) PopErrorScope(Instance instance, Device device)
    {
        var scope = new Completion();
        var handle = GCHandle.Alloc(scope);
        try
        {
            var callback = new PopErrorScopeCallbackInfo { Mode = CallbackMode.WaitAnyOnly, Callback = &OnErrorScope, Userdata1 = (void*)GCHandle.ToIntPtr(handle) };
            Wait(instance, device.PopErrorScope(callback));
        }
        finally
        {
            handle.Free();
        }

        scope.Status.ShouldBe((int)PopErrorScopeStatus.Success, scope.Message);
        return ((ErrorType)scope.Handle, scope.Message);
    }

    private static void Wait(Instance instance, Future future)
    {
        var wait = new FutureWaitInfo { Future = future };
        instance.WaitAny(new Span<FutureWaitInfo>(ref wait), TimeoutNanoseconds).ShouldBe(WaitStatus.Success);
        wait.Completed.ShouldNotBe(0u);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnAdapter(RequestAdapterStatus status, Adapter adapter, StringView message, void* userdata1, void* userdata2) =>
        Completion.From(userdata1).Complete((int)status, adapter.Handle, message);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDevice(RequestDeviceStatus status, Device device, StringView message, void* userdata1, void* userdata2) =>
        Completion.From(userdata1).Complete((int)status, device.Handle, message);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnMapped(MapAsyncStatus status, StringView message, void* userdata1, void* userdata2) =>
        Completion.From(userdata1).Complete((int)status, 0, message);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnErrorScope(PopErrorScopeStatus status, ErrorType type, StringView message, void* userdata1, void* userdata2) =>
        Completion.From(userdata1).Complete((int)status, (nint)type, message);

    // What a callback reported, reached through its userdata1 as a GCHandle. The message is copied: the view is only
    // valid during the callback.
    private sealed class Completion
    {
        public int Status { get; private set; } = -1;

        public nint Handle { get; private set; }

        public string Message { get; private set; } = string.Empty;

        public static Completion From(void* userdata) => (Completion)GCHandle.FromIntPtr((nint)userdata).Target!;

        public void Complete(int status, nint handle, StringView message)
        {
            Status = status;
            Handle = handle;
            Message = Encoding.UTF8.GetString(message.AsSpan());
        }
    }
}
