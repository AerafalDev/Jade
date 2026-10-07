namespace Jade.Wgpu;

/// <summary>The exception thrown when the native library reports that a call failed, with the status it reported (ADR 0040).</summary>
/// <typeparam name="TStatus">The type of the status, such as <see cref="Status"/> or <see cref="RequestAdapterStatus"/>.</typeparam>
public sealed class WgpuException<TStatus> : WgpuException
    where TStatus : struct, Enum
{
    /// <summary>Initializes a new instance of the <see cref="WgpuException{TStatus}"/> class.</summary>
    public WgpuException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WgpuException{TStatus}"/> class with a message.</summary>
    /// <param name="message">The message.</param>
    public WgpuException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WgpuException{TStatus}"/> class with a message and the exception that caused it.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The exception that caused this one.</param>
    public WgpuException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="WgpuException{TStatus}"/> class with the status the library reported.</summary>
    /// <param name="status">The status.</param>
    /// <param name="message">The message.</param>
    public WgpuException(TStatus status, string message)
        : base(message)
    {
        Status = status;
    }

    /// <summary>Gets the status the library reported.</summary>
    public TStatus Status { get; }
}
