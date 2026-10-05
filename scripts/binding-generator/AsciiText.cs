using System.Text;

namespace Jade.BindingGenerator;

/// <summary>Changes the case of identifiers, which are ASCII in every input of the generator.</summary>
/// <remarks>
/// The conversions only touch ASCII letters, so that names never depend on a culture's casing
/// rules; they produce identifiers, never normalized text for comparisons.
/// </remarks>
internal static class AsciiText
{
    /// <summary>Converts the ASCII letters of a text to lower case.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The converted text.</returns>
    /// <exception cref="InvalidDataException">The text is not ASCII.</exception>
    public static string ToLower(ReadOnlySpan<char> text)
    {
        var buffer = new char[text.Length];

        return Ascii.ToLower(text, buffer, out _) == System.Buffers.OperationStatus.Done
            ? new string(buffer)
            : throw new InvalidDataException($"'{text}' is not an ASCII identifier.");
    }
}
