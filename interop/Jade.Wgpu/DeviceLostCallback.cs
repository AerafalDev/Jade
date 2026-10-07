namespace Jade.Wgpu;

/// <summary>
/// Receives the loss of a device (<see cref="DeviceDescriptor.DeviceLost"/>). The library calls it
/// once, on any thread: when the device is lost or destroyed, when its creation fails, or when the
/// instance goes away. An exception it throws ends the process, since it runs inside a native call.
/// </summary>
/// <param name="device">
/// The device, which the callback must not release, or a handle without object when the device was
/// never created or no reference to it remains.
/// </param>
/// <param name="reason">Why the device was lost.</param>
/// <param name="message">The message of the library, or <see langword="null"/>.</param>
public delegate void DeviceLostCallback(Device device, DeviceLostReason reason, string? message);
