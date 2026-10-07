namespace Jade.Wgpu;

/// <summary>
/// Receives an error that no error scope captured (<see cref="DeviceDescriptor.UncapturedError"/>).
/// The library calls it on any thread, and never after the device-lost callback. An exception it
/// throws ends the process, since it runs inside a native call.
/// </summary>
/// <param name="device">The device, which the callback must not release.</param>
/// <param name="error">The error.</param>
public delegate void UncapturedErrorCallback(Device device, GpuError error);
