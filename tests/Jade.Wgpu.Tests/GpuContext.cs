using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Jade.Wgpu.Tests;

/// <summary>
/// An instance, an adapter and a device created through the idiomatic layer, for the smoke tests;
/// the tests are reported as skipped when the host has no library or no adapter.
/// </summary>
internal sealed class GpuContext : IDisposable
{
    private const string LibraryName = "webgpu_dawn";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    private GpuContext(Instance instance, Adapter adapter, Device device, ConcurrentQueue<GpuError> errors)
    {
        Instance = instance;
        Adapter = adapter;
        Device = device;
        Queue = device.GetQueue();
        UncapturedErrors = errors;
    }

    public Instance Instance { get; }

    public Adapter Adapter { get; }

    public Device Device { get; }

    public Queue Queue { get; }

    public ConcurrentQueue<GpuError> UncapturedErrors { get; }

    public static GpuContext Create()
    {
        RequireNativeLibrary();

        var instance = Instance.Create();
        Adapter adapter;

        try
        {
            adapter = Wait(instance, instance.RequestAdapterAsync());
        }
        catch (WgpuException<RequestAdapterStatus> exception) when (exception.Status == RequestAdapterStatus.Unavailable)
        {
            // A host without a usable GPU driver has no adapter, which is not a binding failure.
            instance.Dispose();
            Assert.Inconclusive($"The host has no adapter: {exception.Message}");
            throw;
        }

        var errors = new ConcurrentQueue<GpuError>();
        var device = Wait(instance, adapter.RequestDeviceAsync(new DeviceDescriptor
        {
            Label = "Jade.Wgpu.Tests"u8,
            UncapturedError = (_, error) => errors.Enqueue(error),
        }));

        return new GpuContext(instance, adapter, device, errors);
    }

    public static void RequireNativeLibrary()
    {
        // Probed like the generated imports. The library stays loaded: unloading Dawn is not what
        // these tests exercise.
        if (!NativeLibrary.TryLoad(LibraryName, typeof(Instance).Assembly, null, out _))
        {
            Assert.Inconclusive($"The {LibraryName} library is not built for this host: run 'dotnet run scripts/build-native.cs'.");
        }
    }

    // The idiomatic tasks complete when the instance processes events, which a test does itself.
    public static T Wait<T>(Instance instance, Task<T> task)
    {
        Wait(instance, (Task)task);

        return task.GetAwaiter().GetResult();
    }

    public static void Wait(Instance instance, Task task)
    {
        var watch = Stopwatch.StartNew();

        while (!task.IsCompleted)
        {
            if (watch.Elapsed > _timeout)
            {
                Assert.Fail($"The operation did not complete within {_timeout}.");
            }

            instance.ProcessEvents();
            Thread.Sleep(1);
        }

        task.GetAwaiter().GetResult();
    }

    public T Wait<T>(Task<T> task)
    {
        return Wait(Instance, task);
    }

    public void Wait(Task task)
    {
        Wait(Instance, task);
    }

    public void AssertNoUncapturedError()
    {
        Instance.ProcessEvents();
        Assert.IsTrue(UncapturedErrors.IsEmpty, string.Join(Environment.NewLine, UncapturedErrors));
    }

    public void Dispose()
    {
        Queue.Dispose();
        Device.Dispose();
        Adapter.Dispose();
        Instance.Dispose();
    }
}
