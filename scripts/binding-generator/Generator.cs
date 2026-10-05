using Jade.BindingGenerator.Clang;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Dawn;
using Jade.BindingGenerator.Emission;
using Jade.BindingGenerator.Projection;
using Jade.BindingGenerator.Reporting;
using Jade.BindingGenerator.Sources;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator;

/// <summary>
/// Entry point of the binding generator: loads the inputs of every generated interop library,
/// reports what they contain, and writes the raw layer of the libraries whose front-end builds an
/// intermediate representation.
/// </summary>
/// <remarks>
/// The <c>dawn.json</c> front-end builds one (roadmap task 7); the C header front-end only loads
/// and reports until roadmap task 8.
/// </remarks>
internal static class Generator
{
    /// <summary>The exit code of a run that completed.</summary>
    private const int SuccessExitCode = 0;

    /// <summary>The exit code of a run stopped by invalid or unreachable inputs.</summary>
    private const int FailureExitCode = 1;

    /// <summary>The exit code of a run interrupted by the user, as a shell reports SIGINT.</summary>
    private const int CancelledExitCode = 130;

    /// <summary>Runs the generator for every interop project that has a <c>bindings.json</c>.</summary>
    /// <param name="scriptDirectory">The directory of the entry-point script, <c>scripts/</c> in the repository.</param>
    /// <param name="output">Receives the report of the loaded inputs.</param>
    /// <param name="error">Receives the error messages.</param>
    /// <param name="cancellationToken">Stops the downloads between two requests and the run between two libraries.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string? scriptDirectory, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        try
        {
            var layout = RepositoryLayout.FromScriptDirectory(scriptDirectory);
            var versions = PinnedVersions.Load(layout.VersionsFile);

            using var http = new HttpClient();
            var loader = new LibraryLoader(
                versions,
                new SourceCache(layout.SourceCacheDirectory, http),
                new HeaderParser(layout, SupportedTargets.Create(versions.MinimumOs)));

            foreach (var library in layout.GetGeneratedLibraries())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var loaded = await loader.LoadAsync(library, cancellationToken).ConfigureAwait(false);

                await InputReport.WriteAsync(output, loaded, cancellationToken).ConfigureAwait(false);

                if (loaded.Dawn is { } api)
                {
                    var model = DawnModelBuilder.Build(api, loaded.Configuration.Exclude);
                    var projected = RawProjection.Project(model, library.Project, loaded.Configuration);
                    var update = GeneratedDirectory.Update(library.GeneratedDirectory, RawLayerEmitter.Emit(projected));
                    var directory = Path.GetRelativePath(layout.Root, library.GeneratedDirectory).Replace('\\', '/');

                    await OutputReport.WriteAsync(output, directory, model, projected, update, cancellationToken).ConfigureAwait(false);
                }
            }

            return SuccessExitCode;
        }
        catch (InvalidDataException exception)
        {
            await ReportErrorAsync(error, exception.Message).ConfigureAwait(false);

            return FailureExitCode;
        }
        catch (HttpRequestException exception)
        {
            await ReportErrorAsync(error, $"cannot fetch a pinned source: {exception.Message}").ConfigureAwait(false);

            return FailureExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CancelledExitCode;
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
