using System.Text.RegularExpressions;

/// <summary>
/// Parses Doxygen comments as SDL writes them: a summary paragraph, more paragraphs and <c>-</c> lists, fenced code,
/// and the commands <c>\param</c>, <c>\returns</c>, <c>\threadsafety</c>, <c>\since</c> and <c>\sa</c>.
/// </summary>
internal static partial class DocCommentParser
{
    /// <summary>Parses a raw comment as libclang returns it, delimiters included.</summary>
    /// <param name="raw">The comment, or an empty string.</param>
    /// <returns>The documentation, or <see cref="Documentation.None"/>.</returns>
    public static Documentation Parse(string raw)
    {
        var lines = Strip(raw);
        if (lines.All(string.IsNullOrWhiteSpace))
        {
            return Documentation.None;
        }

        var blocks = new List<DocBlock>();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        var seeAlso = new List<string>();
        string? returns = null;
        var tail = new List<DocBlock>();

        var paragraph = new List<string>();
        var items = new List<string>();
        var index = 0;

        void FlushParagraph()
        {
            if (paragraph.Count > 0)
            {
                blocks.Add(new DocBlock(DocBlockKind.Paragraph, [string.Join(' ', paragraph)]));
                paragraph.Clear();
            }
        }

        void FlushList()
        {
            if (items.Count > 0)
            {
                blocks.Add(new DocBlock(DocBlockKind.List, [.. items]));
                items.Clear();
            }
        }

        // A command's text runs until a blank line or the next command.
        string ReadCommandText(string first)
        {
            var text = new List<string> { first.Trim() };
            while (index + 1 < lines.Count && !string.IsNullOrWhiteSpace(lines[index + 1]) && !CommandPattern().IsMatch(lines[index + 1]))
            {
                text.Add(lines[++index].Trim());
            }

            return string.Join(' ', text.Where(t => t.Length > 0));
        }

        for (; index < lines.Count; index++)
        {
            var line = lines[index];
            var trimmed = line.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph();
                FlushList();
                var code = new List<string>();
                while (++index < lines.Count && !lines[index].Trim().StartsWith("```", StringComparison.Ordinal))
                {
                    code.Add(lines[index]);
                }

                blocks.Add(new DocBlock(DocBlockKind.Code, Dedent(code)));
                continue;
            }

            if (trimmed.Length == 0)
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            var command = CommandPattern().Match(line);
            if (command.Success)
            {
                FlushParagraph();
                FlushList();
                var name = command.Groups["name"].Value;
                var rest = command.Groups["rest"].Value;
                switch (name)
                {
                    case "param":
                        var parameter = ParameterPattern().Match(rest);
                        if (parameter.Success)
                        {
                            parameters[parameter.Groups["name"].Value] = ReadCommandText(parameter.Groups["text"].Value);
                        }

                        break;

                    case "returns" or "return" or "retval":
                        returns = ReadCommandText(rest);
                        break;

                    case "sa" or "see":
                        seeAlso.Add(rest.Trim());
                        break;

                    case "threadsafety":
                        tail.Add(new DocBlock(DocBlockKind.Paragraph, ["Thread safety: " + ReadCommandText(rest)]));
                        break;

                    case "since":
                        tail.Add(new DocBlock(DocBlockKind.Paragraph, [ReadCommandText(rest)]));
                        break;

                    case "brief":
                        paragraph.Add(rest.Trim());
                        break;

                    default:
                        // Unknown commands (\note, \warning, \deprecated, ...) keep their text as a labelled paragraph.
                        tail.Insert(0, new DocBlock(DocBlockKind.Paragraph, [$"{char.ToUpperInvariant(name[0])}{name[1..]}: {ReadCommandText(rest)}"]));
                        break;
                }

                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph();
                items.Add(trimmed[2..].Trim());
                continue;
            }

            if (items.Count > 0 && line.StartsWith(' '))
            {
                items[^1] += " " + trimmed;
                continue;
            }

            FlushList();
            paragraph.Add(trimmed);
        }

        FlushParagraph();
        FlushList();

        if (seeAlso.Count > 0)
        {
            tail.Add(new DocBlock(DocBlockKind.Paragraph, ["See also: " + string.Join(", ", seeAlso.Select(s => $"`{s}`")) + "."]));
        }

        string? summary = null;
        if (blocks.Count > 0 && blocks[0].Kind == DocBlockKind.Paragraph)
        {
            summary = blocks[0].Lines[0];
            blocks.RemoveAt(0);
        }

        return new Documentation { Summary = summary, Remarks = [.. blocks, .. tail], Parameters = parameters, Returns = returns };
    }

    // Removes the comment delimiters and the leading " * " of each line, keeping the indentation after it.
    private static List<string> Strip(string raw)
    {
        var text = raw.Replace("\r\n", "\n", StringComparison.Ordinal);
        var lines = new List<string>();
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            line = DelimiterPattern().Replace(line, string.Empty, 1);
            if (line.EndsWith("*/", StringComparison.Ordinal))
            {
                line = line[..^2].TrimEnd();
            }

            var star = LeadingStarPattern().Match(line);
            if (star.Success)
            {
                line = line[star.Length..];
            }

            lines.Add(line);
        }

        return lines;
    }

    private static List<string> Dedent(List<string> lines)
    {
        var indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return [.. lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart())];
    }

    [GeneratedRegex(@"^\s*(/\*\*<|/\*!<|///<|//!<|/\*\*|/\*!|///|//!)\s?")]
    private static partial Regex DelimiterPattern();

    [GeneratedRegex(@"^\s*\*(?!/) ?")]
    private static partial Regex LeadingStarPattern();

    [GeneratedRegex(@"^\s*[\\@](?<name>[a-z]+)\b(?<rest>.*)$")]
    private static partial Regex CommandPattern();

    [GeneratedRegex(@"^(\[[a-z, ]+\])?\s*(?<name>\w+)\s*(?<text>.*)$")]
    private static partial Regex ParameterPattern();
}
