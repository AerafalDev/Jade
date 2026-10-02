using System.Text;
using System.Text.RegularExpressions;

/// <summary>Renders <see cref="Documentation"/> as XML doc comments, wrapped at 120 columns.</summary>
internal static partial class DocWriter
{
    private const int Width = 120;

    /// <summary>Writes a complete doc comment.</summary>
    /// <param name="writer">The destination.</param>
    /// <param name="summary">The summary text (inline Markdown).</param>
    /// <param name="remarks">The remarks blocks.</param>
    /// <param name="parameters">Every parameter of the C# signature, with its text (inline Markdown).</param>
    /// <param name="returns">The returns text, or <see langword="null"/>.</param>
    public static void Write(CodeWriter writer, string summary, IReadOnlyList<DocBlock> remarks, IReadOnlyList<KeyValuePair<string, string>> parameters, string? returns)
    {
        Element(writer, "summary", string.Empty, Inline(summary));
        if (remarks.Count > 0)
        {
            writer.Line("/// <remarks>");
            foreach (var block in remarks)
            {
                switch (block.Kind)
                {
                    case DocBlockKind.Paragraph:
                        Element(writer, "para", string.Empty, Inline(block.Lines[0]));
                        break;

                    case DocBlockKind.List:
                        writer.Line("/// <list type=\"bullet\">");
                        foreach (var item in block.Lines)
                        {
                            Element(writer, "item", string.Empty, $"<description>{Inline(item)}</description>");
                        }

                        writer.Line("/// </list>");
                        break;

                    case DocBlockKind.Code:
                        writer.Line("/// <code>");
                        foreach (var line in block.Lines)
                        {
                            writer.Line(("/// " + Escape(line)).TrimEnd());
                        }

                        writer.Line("/// </code>");
                        break;
                }
            }

            writer.Line("/// </remarks>");
        }

        foreach (var (name, text) in parameters)
        {
            Element(writer, "param", $" name=\"{name.TrimStart('@')}\"", Inline(text));
        }

        if (returns is not null)
        {
            Element(writer, "returns", string.Empty, Inline(returns));
        }
    }

    /// <summary>Writes a one-element doc comment such as <c>/// &lt;summary&gt;...&lt;/summary&gt;</c>.</summary>
    /// <param name="writer">The destination.</param>
    /// <param name="tag">The element name.</param>
    /// <param name="text">The text (inline Markdown).</param>
    public static void Single(CodeWriter writer, string tag, string text) => Element(writer, tag, string.Empty, Inline(text));

    /// <summary>Converts inline Markdown to XML doc markup: escapes XML, then renders <c>`code`</c> and <c>**bold**</c>.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The XML fragment.</returns>
    public static string Inline(string text)
    {
        var escaped = Escape(text);
        escaped = CodePattern().Replace(escaped, "<c>$1</c>");
        return BoldPattern().Replace(escaped, "<b>$1</b>");
    }

    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);

    // One line when it fits, otherwise the tags on their own lines around the wrapped text.
    private static void Element(CodeWriter writer, string tag, string attributes, string xml)
    {
        var single = $"/// <{tag}{attributes}>{xml}</{tag}>";
        if (writer.Indentation + single.Length <= Width)
        {
            writer.Line(single);
            return;
        }

        writer.Line($"/// <{tag}{attributes}>");
        foreach (var line in Wrap(xml, Width - writer.Indentation - 4))
        {
            writer.Line("/// " + line);
        }

        writer.Line($"/// </{tag}>");
    }

    private static List<string> Wrap(string text, int width)
    {
        var lines = new List<string>();
        var line = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                lines.Add(line.ToString());
                line.Clear();
            }

            if (line.Length > 0)
            {
                line.Append(' ');
            }

            line.Append(word);
        }

        if (line.Length > 0)
        {
            lines.Add(line.ToString());
        }

        return lines;
    }

    [GeneratedRegex("`([^`]+)`")]
    private static partial Regex CodePattern();

    [GeneratedRegex(@"\*\*([^*]+)\*\*")]
    private static partial Regex BoldPattern();
}
