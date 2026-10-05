namespace Jade.NativeBuild.Tools;

/// <summary>The exception thrown when an external tool of the build cannot run or fails.</summary>
internal sealed class CommandFailedException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="CommandFailedException"/> class.</summary>
    public CommandFailedException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="CommandFailedException"/> class.</summary>
    /// <param name="message">The message that describes the failure.</param>
    public CommandFailedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="CommandFailedException"/> class.</summary>
    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public CommandFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
