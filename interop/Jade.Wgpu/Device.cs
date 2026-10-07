using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jade.Wgpu;

/// <content>The members of a device that <c>bindings.json</c> leaves to this file (ADR 0040).</content>
public readonly unsafe partial struct Device
{
    /// <summary>
    /// Calls <c>wgpuDevicePopErrorScope</c>. The task completes when <c>ProcessEvents</c> of the
    /// instance delivers the callback.
    /// </summary>
    /// <returns>A task that completes with the error the scope captured, of type <see cref="ErrorType.NoError"/> when it captured none.</returns>
    public Task<GpuError> PopErrorScopeAsync()
    {
        var completion = new TaskCompletionSource<GpuError>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionHandle = new GCHandle<TaskCompletionSource<GpuError>>(completion);
        var callbackInfo = new Raw.PopErrorScopeCallbackInfo
        {
            Mode = CallbackMode.AllowProcessEvents,
            Callback = &OnPopErrorScopeCompleted,
            Userdata1 = (void*)GCHandle<TaskCompletionSource<GpuError>>.ToIntPtr(completionHandle),
        };

        _ = Raw.NativeMethods.DevicePopErrorScope(this, callbackInfo);

        return completion.Task;
    }

    /// <summary>Completes the task of <c>wgpuDevicePopErrorScope</c> when the library calls back; it never throws, since it runs inside a native call.</summary>
    /// <param name="status">Whether the scope could be popped.</param>
    /// <param name="type">The type of the error the scope captured.</param>
    /// <param name="message">The message of the error, or of the failure.</param>
    /// <param name="userdata1">The handle of the task's completion.</param>
    /// <param name="userdata2">Unused.</param>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnPopErrorScopeCompleted(PopErrorScopeStatus status, ErrorType type, Raw.StringView message, void* userdata1, void* userdata2)
    {
        var completionHandle = GCHandle<TaskCompletionSource<GpuError>>.FromIntPtr((nint)userdata1);
        var completion = completionHandle.Target;

        completionHandle.Dispose();

        _ = status == PopErrorScopeStatus.Success
            ? completion.TrySetResult(new GpuError(type, Utf8Text.ToManagedString(message)))
            : completion.TrySetException(WgpuException.FromCallback(status, "wgpuDevicePopErrorScope", message));
    }
}
