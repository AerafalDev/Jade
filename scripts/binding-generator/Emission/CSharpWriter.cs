using System.Text;

namespace Jade.BindingGenerator.Emission;

/// <summary>Builds C# or C source text with four-space indentation and LF line endings, whatever the host.</summary>
internal sealed class CSharpWriter
{
    /// <summary>One level of indentation.</summary>
    private const string Indentation = "    ";

    /// <summary>The text written so far.</summary>
    private readonly StringBuilder _builder = new();

    /// <summary>The current indentation level.</summary>
    private int _depth;

    /// <summary>Writes a line at the current indentation; an empty line gets no indentation.</summary>
    /// <param name="text">The text of the line.</param>
    public void Line(string text = "")
    {
        if (text.Length > 0)
        {
            for (var i = 0; i < _depth; i++)
            {
                _ = _builder.Append(Indentation);
            }

            _ = _builder.Append(text);
        }

        // An explicit LF, not Environment.NewLine: the output must be the same on every host.
        _ = _builder.Append('\n');
    }

    /// <summary>Writes an opening brace and indents the following lines.</summary>
    public void OpenBlock()
    {
        Line("{");
        _depth++;
    }

    /// <summary>Unindents and writes a closing brace.</summary>
    public void CloseBlock()
    {
        CloseBlock(string.Empty);
    }

    /// <summary>Unindents and writes a closing brace followed by text, such as the semicolon of an object initializer.</summary>
    /// <param name="suffix">The text after the brace.</param>
    public void CloseBlock(string suffix)
    {
        _depth--;
        Line("}" + suffix);
    }

    /// <summary>Gets the text written so far.</summary>
    /// <returns>The source text.</returns>
    public override string ToString()
    {
        return _builder.ToString();
    }
}
