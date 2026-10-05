using System.Globalization;
using Jade.NativeBuild.Build;
using Jade.NativeBuild.Configuration;
using Jade.NativeBuild.Tools;

namespace Jade.NativeFetch;

/// <summary>
/// Entry point of <c>fetch-native.cs</c>: downloads the attested natives of the native workflow
/// for local work, into the directories <c>build-native.cs</c> installs to (ADR 0038).
/// </summary>
internal static class NativeFetch
{
    /// <summary>The exit code of a fetch that completed.</summary>
    private const int SuccessExitCode = 0;

    /// <summary>The exit code of a fetch stopped by invalid arguments, a missing run or a failed verification.</summary>
    private const int FailureExitCode = 1;

    /// <summary>The exit code of a fetch interrupted by the user, as a shell reports SIGINT.</summary>
    private const int CancelledExitCode = 130;

    /// <summary>The repository whose workflow builds and attests the natives.</summary>
    private const string Repository = "AerafalDev/Jade";

    /// <summary>The branch whose runs are searched when no run is given.</summary>
    private const string Branch = "main";

    /// <summary>The usage line, printed with an argument error.</summary>
    private const string Usage = "usage: dotnet run scripts/fetch-native.cs [--rid <runtime identifier>]... [--run <run id>]";

    /// <summary>Runs the fetch.</summary>
    /// <param name="scriptDirectory">The directory of the entry-point script, <c>scripts/</c> in the repository.</param>
    /// <param name="arguments">
    /// The command-line arguments: <c>--rid</c>, repeatable, selects the runtime identifiers, the
    /// host's by default; <c>--run</c> takes the artifacts of a given run instead of the newest
    /// matching run of <c>main</c>.
    /// </param>
    /// <param name="output">Receives the progress and the fetched files.</param>
    /// <param name="error">Receives the error messages.</param>
    /// <param name="cancellationToken">Stops the running tool.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string? scriptDirectory, IReadOnlyList<string> arguments, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        try
        {
            var (runtimeIdentifiers, runId) = ParseArguments(arguments);
            var layout = BuildLayout.FromScriptDirectory(scriptDirectory);
            var artifacts = runtimeIdentifiers.Count == 0
                ? [FindArtifact(HostRuntimeIdentifier(HostPlatform.Current))]
                : runtimeIdentifiers.Select(FindArtifact).ToList();
            var gitHub = new GitHubCli(Repository);
            var inputs = new NativeInputs(layout.Root);
            var run = runId is { } id
                ? await gitHub.GetRunAsync(id, cancellationToken).ConfigureAwait(false)
                : await FindRunAsync(gitHub, inputs, artifacts, output, cancellationToken).ConfigureAwait(false);

            if (runId is not null && !await inputs.MatchAsync(run.HeadSha, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture, $"Run {run.Id} built {run.HeadSha}, whose native build inputs differ from the checkout's."));
            }

            await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"Fetching from run {run.Id} of {Repository} ({run.HeadSha})").AsMemory(), cancellationToken).ConfigureAwait(false);

            foreach (var artifact in artifacts)
            {
                await FetchAsync(gitHub, run, artifact.LibrariesArtifact, layout.GetOutputDirectory(artifact.RuntimeIdentifier), output, cancellationToken).ConfigureAwait(false);
                await FetchAsync(gitHub, run, artifact.TestArtifact, layout.GetTestOutputDirectory(artifact.RuntimeIdentifier), output, cancellationToken).ConfigureAwait(false);
            }

            return SuccessExitCode;
        }
        catch (InvalidDataException exception)
        {
            await ReportErrorAsync(error, exception.Message).ConfigureAwait(false);

            return FailureExitCode;
        }
        catch (CommandFailedException exception)
        {
            await ReportErrorAsync(error, exception.Message).ConfigureAwait(false);

            return FailureExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CancelledExitCode;
        }
    }

    /// <summary>Finds the newest successful run of <c>main</c> whose inputs equal the checkout's and that still has the artifacts.</summary>
    /// <param name="gitHub">The GitHub CLI.</param>
    /// <param name="inputs">The native build inputs of the checkout.</param>
    /// <param name="artifacts">The artifacts to fetch.</param>
    /// <param name="output">Receives a line for each run passed over.</param>
    /// <param name="cancellationToken">Stops the tools.</param>
    /// <returns>The run.</returns>
    /// <exception cref="InvalidDataException">No such run exists.</exception>
    /// <remarks>
    /// A run whose builds were skipped, because its inputs had not changed, has no artifacts: an
    /// older run with the same inputs has them.
    /// </remarks>
    private static async Task<WorkflowRun> FindRunAsync(GitHubCli gitHub, NativeInputs inputs, IReadOnlyList<NativeArtifact> artifacts, TextWriter output, CancellationToken cancellationToken)
    {
        foreach (var run in await gitHub.ListSuccessfulRunsAsync(Branch, cancellationToken).ConfigureAwait(false))
        {
            if (!await inputs.MatchAsync(run.HeadSha, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            var available = await gitHub.ListArtifactsAsync(run, cancellationToken).ConfigureAwait(false);

            if (artifacts.All(artifact => available.Contains(artifact.LibrariesArtifact) && available.Contains(artifact.TestArtifact)))
            {
                return run;
            }

            await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"Run {run.Id} has the checkout's inputs but not the artifacts.").AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidDataException(
            $"No successful run of {GitHubCli.WorkflowPath} on {Branch} has the native build inputs of the checkout and its artifacts: build them with 'dotnet run scripts/build-native.cs', or pass the run of a branch with --run.");
    }

    /// <summary>Downloads an artifact, verifies the attestation of every file, and installs it.</summary>
    /// <param name="gitHub">The GitHub CLI.</param>
    /// <param name="run">The run.</param>
    /// <param name="name">The artifact's name.</param>
    /// <param name="directory">The directory the files replace the content of.</param>
    /// <param name="output">Receives a line for each verified file.</param>
    /// <param name="cancellationToken">Stops the tools.</param>
    /// <returns>A task that completes when the files are installed.</returns>
    /// <remarks>The files are verified before anything is replaced, so a failed fetch leaves the previous libraries in place.</remarks>
    private static async Task FetchAsync(GitHubCli gitHub, WorkflowRun run, string name, string directory, TextWriter output, CancellationToken cancellationToken)
    {
        var staging = directory + ".partial";

        DeleteDirectory(staging);
        await gitHub.DownloadAsync(run, name, staging, cancellationToken).ConfigureAwait(false);

        foreach (var file in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            await gitHub.VerifyAsync(run, file, cancellationToken).ConfigureAwait(false);
            await output.WriteLineAsync($"{name}: {Path.GetRelativePath(staging, file)} verified".AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        DeleteDirectory(directory);
        Directory.Move(staging, directory);
    }

    /// <summary>Parses the command line.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The requested runtime identifiers and run, if any.</returns>
    /// <exception cref="InvalidDataException">An argument is unknown or lacks its value.</exception>
    private static (IReadOnlyList<string> RuntimeIdentifiers, long? RunId) ParseArguments(IReadOnlyList<string> arguments)
    {
        var runtimeIdentifiers = new List<string>();
        long? runId = null;

        for (var index = 0; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case "--rid" when index + 1 < arguments.Count:
                    runtimeIdentifiers.Add(arguments[++index]);
                    break;
                case "--run" when index + 1 < arguments.Count && runId is null && long.TryParse(arguments[index + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var id):
                    runId = id;
                    index++;
                    break;
                default:
                    throw new InvalidDataException($"Unexpected argument '{arguments[index]}'.\n{Usage}");
            }
        }

        return (runtimeIdentifiers, runId);
    }

    /// <summary>Gets the artifacts of a runtime identifier.</summary>
    /// <param name="runtimeIdentifier">The runtime identifier.</param>
    /// <returns>Its artifacts.</returns>
    /// <exception cref="InvalidDataException">The workflow does not build it.</exception>
    private static NativeArtifact FindArtifact(string runtimeIdentifier)
    {
        return NativeArtifact.Find(runtimeIdentifier)
            ?? throw new InvalidDataException($"The native workflow does not build '{runtimeIdentifier}': the runtime identifiers are {string.Join(", ", NativeArtifact.RuntimeIdentifiers)}.");
    }

    /// <summary>Gets the runtime identifier of the host, whose natives the tests load.</summary>
    /// <param name="host">The host.</param>
    /// <returns>The runtime identifier, such as <c>linux-x64</c>.</returns>
    private static string HostRuntimeIdentifier(HostPlatform host)
    {
        var platform = host.Platform switch
        {
            NativePlatform.Windows => "win",
            NativePlatform.MacOS => "osx",
            NativePlatform.Linux or NativePlatform.IOS or NativePlatform.Android or NativePlatform.Browser or _ => "linux",
        };

        return $"{platform}-{host.ArchitectureKey}";
    }

    /// <summary>Deletes a directory and its content if it exists.</summary>
    /// <param name="path">The directory.</param>
    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    /// <summary>Writes an error message.</summary>
    /// <param name="error">The writer that receives the error messages.</param>
    /// <param name="message">The message.</param>
    /// <returns>A task that completes when the message is written.</returns>
    /// <remarks>The write is not cancellable: a failed run must say why, even while it is being cancelled.</remarks>
    private static async Task ReportErrorAsync(TextWriter error, string message)
    {
        await error.WriteLineAsync($"error: {message}".AsMemory(), CancellationToken.None).ConfigureAwait(false);
    }
}
