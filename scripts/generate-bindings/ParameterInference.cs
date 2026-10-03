using System.Text.RegularExpressions;

/// <summary>
/// Infers friendly-overload rules (ADR-0006) from upstream <c>\param</c> text, for libraries whose docs state how each
/// pointer is used, as SDL's do. A rule is only inferred when the text says so plainly; anything else keeps the raw
/// pointer, and <see cref="LibraryConfig.Parameters"/> overrides every inference.
/// </summary>
internal static partial class ParameterInference
{
    /// <summary>Infers the rule of one parameter.</summary>
    /// <param name="index">The parameter's position.</param>
    /// <param name="names">The C names of every parameter.</param>
    /// <param name="types">The types of every parameter.</param>
    /// <param name="documentation">The function's documentation.</param>
    /// <param name="isStruct">Whether a named type is a struct exposed by value.</param>
    /// <returns>The rule, or <see langword="null"/> to keep the raw pointer.</returns>
    public static ParameterRule? Infer(int index, IReadOnlyList<string> names, IReadOnlyList<TypeRef> types, Documentation documentation, Func<string, bool> isStruct)
    {
        var type = types[index];
        if (!type.IsPointer || type.Element!.Kind is TypeKind.Void or TypeKind.FunctionPointer
            || !documentation.Parameters.TryGetValue(names[index], out var text))
        {
            return null;
        }

        var element = type.Element;
        var isChar = element.Is(PrimitiveType.Char);
        if (type.IsConst && isChar)
        {
            // NUL-terminated strings get their UTF-8 overload elsewhere.
            return null;
        }

        // Pointers cannot be generic arguments, so an array of them has no span form.
        if (element.Kind != TypeKind.Pointer && Count(index, names, types, documentation, text) is { } count)
        {
            return ParameterRule.Span(count);
        }

        // Arrays and buffers whose length the docs do not tie to a parameter stay raw.
        if (ArrayPattern().IsMatch(text) || isChar)
        {
            return null;
        }

        if (!type.IsConst && OutPattern().IsMatch(text))
        {
            return ParameterRule.Out;
        }

        if (!type.IsConst && RefPattern().IsMatch(text))
        {
            return ParameterRule.Ref;
        }

        return type.IsConst && element.Kind == TypeKind.Named && isStruct(element.Name!) ? ParameterRule.In : null;
    }

    // The count of a pointer is the one integer parameter whose doc counts it: by naming it ("the number of values to
    // write to data"), or as the only "number of" parameter when the pointer's own doc calls it an array.
    private static string? Count(int index, IReadOnlyList<string> names, IReadOnlyList<TypeRef> types, Documentation documentation, string text)
    {
        var bytes = types[index].Element!.Kind == TypeKind.Primitive && types[index].Element!.Primitive is PrimitiveType.Char or PrimitiveType.Byte or PrimitiveType.SByte;
        var candidates = new List<string>();
        var counting = new List<string>();
        for (var i = 0; i < names.Count; i++)
        {
            // The name has to look like a count too: "the maximum number of milliseconds to wait for the next available
            // event" does not count `event`.
            if (i == index || !types[i].IsInteger() || !CountNamePattern().IsMatch(names[i]) || !documentation.Parameters.TryGetValue(names[i], out var countText))
            {
                continue;
            }

            // Only a byte buffer can take its length from a size in bytes.
            var counts = NumberPattern().IsMatch(countText) || (bytes && SizePattern().IsMatch(countText));
            if (!counts)
            {
                continue;
            }

            counting.Add(names[i]);
            if (Regex.IsMatch(countText, $@"\b{Regex.Escape(names[index])}\b", RegexOptions.CultureInvariant))
            {
                candidates.Add(names[i]);
            }
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        return candidates.Count == 0 && counting.Count == 1 && ArrayPattern().IsMatch(text) ? counting[0] : null;
    }

    // Only how the doc introduces the parameter counts: "an array of", "destination buffer for", "pointer to a list of".
    // "the number of elements in the list" is about the returned list, not about this parameter.
    [GeneratedRegex(@"^\s*(a pointer to |pointer to )?((an?|the) )?(\w+ ){0,2}(array|buffer|list)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ArrayPattern();

    [GeneratedRegex(@"\b(filled|fill|store|stores|stored|storing|receives?|to hold|to obtain|written here|supplied here|to be initialized|resulting|to output|will be set)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OutPattern();

    [GeneratedRegex(@"\b(to be modified|to be read and adjusted)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RefPattern();

    [GeneratedRegex(@"^(n|num|count|len|cb|max)|(count|len|length|size|num)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CountNamePattern();

    [GeneratedRegex(@"\bnumber of\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"\b(size|length|bytes)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SizePattern();
}
