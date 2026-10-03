using System.Text.Json;

/// <summary>Reads and changes a repository's labels through the GitHub CLI.</summary>
internal sealed class GitHubLabels
{
    private readonly string _workingDirectory;
    private readonly string? _repository;

    /// <summary>Creates an accessor for one repository.</summary>
    /// <param name="workingDirectory">A directory inside the clone, which gh takes the repository from by default.</param>
    /// <param name="repository">The repository as <c>owner/name</c>, or <see langword="null"/> for the one gh resolves from the clone.</param>
    public GitHubLabels(string workingDirectory, string? repository)
    {
        _workingDirectory = workingDirectory;
        _repository = repository;
    }

    /// <summary>Asks gh which repository it works on.</summary>
    /// <returns>The repository as <c>owner/name</c>.</returns>
    /// <exception cref="InvalidOperationException">gh fails, for example on an unknown repository.</exception>
    public string ResolveRepository() =>
        Gh.Run(_workingDirectory, _repository is null
            ? ["repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner"]
            : ["repo", "view", _repository, "--json", "nameWithOwner", "--jq", ".nameWithOwner"]);

    /// <summary>Lists the repository's labels.</summary>
    /// <returns>The labels, without aliases.</returns>
    /// <exception cref="InvalidOperationException">gh fails.</exception>
    public IReadOnlyList<Label> List()
    {
        using var document = JsonDocument.Parse(Run(["label", "list", "--limit", "1000", "--json", "name,color,description"]));
        var labels = new List<Label>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            labels.Add(new Label(
                element.GetProperty("name").GetString()!,
                element.GetProperty("color").GetString() ?? "",
                element.GetProperty("description").GetString() ?? "",
                []));
        }

        return labels;
    }

    /// <summary>Creates a label.</summary>
    /// <param name="label">The label to create.</param>
    /// <exception cref="InvalidOperationException">gh fails.</exception>
    public void Create(Label label) =>
        Run(["label", "create", label.Name, "--color", label.Color, "--description", label.Description]);

    /// <summary>Gives an existing label the name, color and description of a declaration.</summary>
    /// <param name="currentName">The label's name on GitHub.</param>
    /// <param name="label">The declaration.</param>
    /// <exception cref="InvalidOperationException">gh fails.</exception>
    public void Update(string currentName, Label label) =>
        Run(["label", "edit", currentName, "--name", label.Name, "--color", label.Color, "--description", label.Description]);

    /// <summary>Deletes a label, which also removes it from its issues and pull requests.</summary>
    /// <param name="name">The label's name.</param>
    /// <exception cref="InvalidOperationException">gh fails.</exception>
    public void Delete(string name) =>
        Run(["label", "delete", name, "--yes"]);

    private string Run(IReadOnlyList<string> arguments) =>
        Gh.Run(_workingDirectory, _repository is null ? arguments : [.. arguments, "--repo", _repository]);
}
