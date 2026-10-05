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

    /// <summary>The number of runs read from the newest, enough to cover the 90-day artifact retention of a monthly schedule.</summary>
    private const int RunCount = 50;

    /// <summary>Lists the successful runs of the native workflow on a branch, newest first.</summary>
    /// <param name="branch">The branch.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>The runs.</returns>
    public async Task<IReadOnlyList<WorkflowRun>> ListSuccessfulRunsAsync(string branch, CancellationToken cancellationToken)
    {
        var json = await ApiAsync($"repos/{repository}/actions/workflows/{Path.GetFileName(WorkflowPath)}/runs?branch={Uri.EscapeDataString(branch)}&status=success&per_page={RunCount}", cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(json);

        return [.. document.RootElement.GetProperty("workflow_runs").EnumerateArray().Select(ReadRun)];
    }

    /// <summary>Gets a run of the native workflow.</summary>
    /// <param name="id">The run's identifier.</param>
    /// <param name="cancellationToken">Stops <c>gh</c>.</param>
    /// <returns>The run.</returns>
    /// <exception cref="InvalidDataException">The run belongs to another workflow or did not succeed.</exception>
    public async Task<WorkflowRun> GetRunAsync(long id, CancellationToken cancellationToken)
    {
        var json = await ApiAsync(string.Create(CultureInfo.InvariantCulture, $"repos/{repository}/actions/runs/{id}"), cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        return root.GetProperty("path").GetString() != WorkflowPath
            ? throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"Run {id} is not a run of {WorkflowPath}."))
            : root.GetProperty("conclusion").GetString() != "success"
            ? throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"Run {id} did not succeed."))
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
    /// <param name="run">The run's JSON object.</param>
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
