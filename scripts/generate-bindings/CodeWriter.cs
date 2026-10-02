using System.Text;

/// <summary>Builds C# source with four-space indentation and LF line endings, whatever the host OS.</summary>
internal sealed class CodeWriter
{
    private readonly StringBuilder builder = new();
    private int depth;

    /// <summary>Gets the current indentation width in columns.</summary>
    public int Indentation => depth * 4;

    /// <summary>Writes one line at the current indentation; an empty line gets no trailing spaces.</summary>
    /// <param name="text">The line.</param>
    public void Line(string text = "")
    {
        if (text.Length > 0)
        {
            builder.Append(' ', depth * 4).Append(text);
        }

        builder.Append('\n');
    }

    /// <summary>Writes a line followed by <c>{</c> and indents.</summary>
    /// <param name="text">The statement or declaration that opens the block.</param>
    public void Open(string text)
    {
        Line(text);
        Line("{");
        depth++;
    }

    /// <summary>Unindents and writes <c>}</c>.</summary>
    public void Close()
    {
        depth--;
        Line("}");
    }

    /// <summary>Indents without writing a brace, for stacked <c>fixed</c> statements.</summary>
    public void Indent() => depth++;

    /// <summary>Unindents without writing a brace.</summary>
    public void Unindent() => depth--;

    /// <summary>Returns the source written so far.</summary>
    /// <returns>The source.</returns>
    public override string ToString() => builder.ToString();
}
