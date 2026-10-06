using System.Globalization;
using System.Text.Json;
using Jade.NativeBuild.Tools;

namespace Jade.NativeFetch;

/// <summary>Reads the native workflow's runs and artifacts through the GitHub CLI, which handles authentication.</summary>
/// <param name="repository">The repository, as <c>owner/name</c>.</param>
/// <remarks>Downloading an artifact needs an authenticated token even for a public repository.</remarks>
internal sealed class GitHubCli(string repository)
{
    /// <summary>The workflow that builds and attests the natives.</summary>
    public const string WorkflowPath = ".github/workflows/native.yml";

    /// <summary>The number of artifacts of one name read, newest first.</summary>
    /// <remarks>
    /// <c>main</c> builds only when the native build inputs change and once a month, so far fewer
    /// artifacts of one name live within their 90-day retention.
    /// </remarks>
    private const int ArtifactCount = 100;

    /// <summary>Lists the runs of a branch that still have an artifact, newest first.</summary>
    /// <param name="name">The artifact's name.</param>
    /// <param name="branch">The branch.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>The runs.</returns>
    /// <remarks>
    /// Runs of the branch whose builds were skipped have no artifact, so they are not listed: the
    /// artifacts, not the runs, are searched.
    /// </remarks>
    public async Task<IReadOnlyList<WorkflowRun>> ListRunsWithArtifactAsync(string name, string branch, CancellationToken cancellationToken)
    {
        var json = await ApiAsync(string.Create(CultureInfo.InvariantCulture, $"repos/{repository}/actions/artifacts?name={Uri.EscapeDataString(name)}&per_page={ArtifactCount}"), cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(json);

        return
        [
            .. document.RootElement.GetProperty("artifacts").EnumerateArray()
                .Where(artifact => !artifact.GetProperty("expired").GetBoolean() && artifact.GetProperty("workflow_run").GetProperty("head_branch").GetString() == branch)
                .Select(static artifact => ReadRun(artifact.GetProperty("workflow_run")))
                .DistinctBy(static run => run.Id),
        ];
    }

    /// <summary>Gets a run of the native workflow.</summary>
    /// <param name="id">The run's identifier.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>The run.</returns>
    /// <exception cref="InvalidDataException">The run belongs to another workflow.</exception>
    /// <remarks>
    /// A run whose other jobs failed is accepted: each artifact is uploaded once its own build has
    /// passed, and its attestation is verified anyway.
    /// </remarks>
    public async Task<WorkflowRun> GetRunAsync(long id, CancellationToken cancellationToken)
    {
        var json = await ApiAsync(string.Create(CultureInfo.InvariantCulture, $"repos/{repository}/actions/runs/{id}"), cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        return root.GetProperty("path").GetString() != WorkflowPath
            ? throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"Run {id} is not a run of {WorkflowPath}."))
            : ReadRun(root);
    }

    /// <summary>Lists the names of the artifacts a run still has.</summary>
    /// <param name="run">The run.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>The names of the artifacts that have not expired.</returns>
    public async Task<IReadOnlySet<string>> ListArtifactsAsync(WorkflowRun run, CancellationToken cancellationToken)
    {
        var json = await ApiAsync(string.Create(CultureInfo.InvariantCulture, $"repos/{repository}/actions/runs/{run.Id}/artifacts?per_page=100"), cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(json);

        return document.RootElement.GetProperty("artifacts").EnumerateArray()
            .Where(static artifact => !artifact.GetProperty("expired").GetBoolean())
            .Select(static artifact => artifact.GetProperty("name").GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Downloads and extracts an artifact.</summary>
    /// <param name="run">The run.</param>
    /// <param name="name">The artifact's name.</param>
    /// <param name="directory">The directory to extract into.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>A task that completes when the artifact is extracted.</returns>
    public Task DownloadAsync(WorkflowRun run, string name, string directory, CancellationToken cancellationToken)
    {
        return Command.RunAsync("gh", ["run", "download", run.Id.ToString(CultureInfo.InvariantCulture), "--repo", repository, "--name", name, "--dir", directory], environment: null, workingDirectory: null, cancellationToken);
    }

    /// <summary>Verifies that a file has a provenance attestation of the native workflow, for the commit of a run.</summary>
    /// <param name="run">The run the file comes from.</param>
    /// <param name="path">The file.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>A task that completes when the file is verified.</returns>
    /// <exception cref="CommandFailedException">No such attestation exists.</exception>
    public Task VerifyAsync(WorkflowRun run, string path, CancellationToken cancellationToken)
    {
        return Command.ReadAsync(
            "gh",
            [
                "attestation", "verify", path,
                "--repo", repository,
                "--signer-workflow", $"{repository}/{WorkflowPath}",
                "--source-digest", run.HeadSha,
                "--deny-self-hosted-runners",
                "--format", "json",
            ],
            environment: null,
            cancellationToken);
    }

    /// <summary>Reads a run from its JSON.</summary>
    /// <param name="run">The run's JSON object, or the <c>workflow_run</c> of an artifact.</param>
    /// <returns>The run.</returns>
    private static WorkflowRun ReadRun(JsonElement run)
    {
        return new(run.GetProperty("id").GetInt64(), run.GetProperty("head_sha").GetString()!);
    }

    /// <summary>Calls the GitHub REST API.</summary>
    /// <param name="endpoint">The endpoint, relative to the API root.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>The response body.</returns>
    private static Task<string> ApiAsync(string endpoint, CancellationToken cancellationToken)
    {
        return Command.ReadAsync("gh", ["api", endpoint], environment: null, cancellationToken);
    }
}
