/// <summary>A C type the generator cannot express; the reader turns it into an error naming the declaration.</summary>
internal sealed class MappingException : Exception
{
    /// <summary>Creates the exception.</summary>
    public MappingException()
    {
    }

    /// <summary>Creates the exception.</summary>
    /// <param name="message">Why the type cannot be bound.</param>
    public MappingException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception.</summary>
    /// <param name="message">Why the type cannot be bound.</param>
    /// <param name="innerException">The cause.</param>
    public MappingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
