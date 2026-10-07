using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jade.Wgpu;

/// <content>The lowering of the callbacks a device keeps, which <c>bindings.json</c> leaves to this file (ADR 0040).</content>
public unsafe ref partial struct DeviceDescriptor
{
    /// <summary>Fills in the callback infos of the device-lost and uncaptured error callbacks.</summary>
    /// <param name="target">The raw descriptor.</param>
    /// <param name="arena">The memory of the call, which the callbacks do not need: they outlive it.</param>
    private readonly partial void LowerHandWritten(Raw.DeviceDescriptor* target, scoped ref Arena arena)
    {
        target->DeviceLostCallbackInfo = default;
        target->UncapturedErrorCallbackInfo = default;

        if (DeviceLost is null && UncapturedError is null)
        {
            return;
        }

        // One handle serves both callbacks, freed by the device-lost callback: Dawn calls it exactly
        // once for every device (DeviceLostEvent in src/dawn/native/Device.cpp, failed creations
        // included) and clears the uncaptured error callback before it.
        var callbacks = new GCHandle<DeviceCallbacks>(new DeviceCallbacks(DeviceLost, UncapturedError));
        var userdata = (void*)GCHandle<DeviceCallbacks>.ToIntPtr(callbacks);

        target->DeviceLostCallbackInfo = new Raw.DeviceLostCallbackInfo
        {
            Mode = CallbackMode.AllowSpontaneous,
            Callback = &OnDeviceLost,
            Userdata1 = userdata,
        };

        if (UncapturedError is not null)
        {
            target->UncapturedErrorCallbackInfo = new Raw.UncapturedErrorCallbackInfo
            {
                Callback = &OnUncapturedError,
                Userdata1 = userdata,
            };
        }
    }

    /// <summary>Receives the loss of the device, calls the delegate and frees the callbacks' handle.</summary>
    /// <param name="device">A pointer to the device, a handle without object when none remains.</param>
    /// <param name="reason">Why the device was lost.</param>
    /// <param name="message">The message of the library.</param>
    /// <param name="userdata1">The handle of the callbacks.</param>
    /// <param name="userdata2">Unused.</param>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDeviceLost(Device* device, DeviceLostReason reason, Raw.StringView message, void* userdata1, void* userdata2)
    {
        var handle = GCHandle<DeviceCallbacks>.FromIntPtr((nint)userdata1);
        var callbacks = handle.Target;

        handle.Dispose();
        callbacks.DeviceLost?.Invoke(*device, reason, Utf8Text.ToManagedString(message));
    }

    /// <summary>Receives an uncaptured error and calls the delegate.</summary>
    /// <param name="device">A pointer to the device.</param>
    /// <param name="type">The type of the error.</param>
    /// <param name="message">The message of the library.</param>
    /// <param name="userdata1">The handle of the callbacks.</param>
    /// <param name="userdata2">Unused.</param>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnUncapturedError(Device* device, ErrorType type, Raw.StringView message, void* userdata1, void* userdata2)
    {
        var callbacks = GCHandle<DeviceCallbacks>.FromIntPtr((nint)userdata1).Target;

        callbacks.UncapturedError?.Invoke(*device, new GpuError(type, Utf8Text.ToManagedString(message)));
    }

    /// <summary>The delegates of a device's callbacks, which the library's userdata points to.</summary>
    /// <param name="deviceLost">The device-lost callback.</param>
    /// <param name="uncapturedError">The uncaptured error callback.</param>
    private sealed class DeviceCallbacks(DeviceLostCallback? deviceLost, UncapturedErrorCallback? uncapturedError)
    {
        /// <summary>Gets the device-lost callback.</summary>
        public DeviceLostCallback? DeviceLost { get; } = deviceLost;

        /// <summary>Gets the uncaptured error callback.</summary>
        public UncapturedErrorCallback? UncapturedError { get; } = uncapturedError;
    }
}
