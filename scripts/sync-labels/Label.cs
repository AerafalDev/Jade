/// <summary>A label, as .github/labels.yml declares it or as it exists on GitHub.</summary>
internal sealed class Label
{
    /// <summary>Creates a label.</summary>
    /// <param name="name">The name.</param>
    /// <param name="color">The color, six hexadecimal digits without <c>#</c>.</param>
    /// <param name="description">The description.</param>
    /// <param name="aliases">Names of existing labels to rename into this one; empty for a label read from GitHub.</param>
    public Label(string name, string color, string description, IReadOnlyList<string> aliases)
    {
        Name = name;
        Color = color;
        Description = description;
        Aliases = aliases;
    }

    /// <summary>Gets the name.</summary>
    public string Name { get; }

    /// <summary>Gets the color, six hexadecimal digits without <c>#</c>.</summary>
    public string Color { get; }

    /// <summary>Gets the description.</summary>
    public string Description { get; }

    /// <summary>Gets the names of existing labels to rename into this one, so that their issues and pull requests keep it.</summary>
    public IReadOnlyList<string> Aliases { get; }
}
