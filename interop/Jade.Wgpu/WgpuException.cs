namespace Jade.Wgpu;

/// <summary>The exception thrown when the native library reports that a call failed (ADR 0040).</summary>
/// <remarks>
/// A function that returns an error status throws <see cref="WgpuException{TStatus}"/> with that
/// status; an asynchronous operation whose callback reports a failure faults its task with it.
/// Validation errors are not reported this way: the device reports them through error scopes and
/// its uncaptured error callback.
/// </remarks>
public class WgpuException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="WgpuException"/> class.</summary>
    public WgpuException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WgpuException"/> class with a message.</summary>
    /// <param name="message">The message.</param>
    public WgpuException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WgpuException"/> class with a message and the exception that caused it.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public WgpuException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Throws when a function returns an error status.</summary>
    /// <param name="status">The status.</param>
    /// <param name="function">The C name of the function.</param>
    /// <exception cref="WgpuException{TStatus}">The status is not <see cref="Status.Success"/>.</exception>
    internal static void ThrowIfFailed(Status status, string function)
    {
        if (status != Status.Success)
        {
            throw new WgpuException<Status>(status, $"{function} failed with {status}.");
        }
    }

    /// <summary>Creates the exception that faults the task of an asynchronous operation.</summary>
    /// <typeparam name="TStatus">The type of the status the callback reports.</typeparam>
    /// <param name="status">The status.</param>
    /// <param name="function">The C name of the function that started the operation.</param>
    /// <param name="message">The message the callback reports.</param>
    /// <returns>The exception.</returns>
    internal static WgpuException<TStatus> FromCallback<TStatus>(TStatus status, string function, Raw.StringView message)
        where TStatus : struct, Enum
    {
        var text = Utf8Text.ToManagedString(message);

        return new WgpuException<TStatus>(status, string.IsNullOrEmpty(text) ? $"{function} failed with {status}." : $"{function} failed with {status}: {text}");
    }

    /// <summary>Creates the exception that faults the task of an asynchronous operation whose callback reports no message.</summary>
    /// <typeparam name="TStatus">The type of the status the callback reports.</typeparam>
    /// <param name="status">The status.</param>
    /// <param name="function">The C name of the function that started the operation.</param>
    /// <returns>The exception.</returns>
    internal static WgpuException<TStatus> FromCallback<TStatus>(TStatus status, string function)
        where TStatus : struct, Enum
    {
        return new WgpuException<TStatus>(status, $"{function} failed with {status}.");
    }
}
