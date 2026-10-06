using Jade.NativeBuild.Build;
using Jade.NativeBuild.Configuration;
using Jade.NativeBuild.Sources;
using Jade.NativeBuild.Tools;

namespace Jade.NativeBuild;

/// <summary>
/// Entry point of the native build: builds Dawn, SDL3 and miniaudio for one runtime identifier from
/// <c>build/versions.json</c>, and the layout libraries of the tests (ADR 0036).
/// </summary>
internal static class NativeBuild
{
    /// <summary>The exit code of a build that completed.</summary>
    private const int SuccessExitCode = 0;

    /// <summary>The exit code of a build stopped by invalid inputs, a missing tool or a failed step.</summary>
    private const int FailureExitCode = 1;

    /// <summary>The exit code of a build interrupted by the user, as a shell reports SIGINT.</summary>
    private const int CancelledExitCode = 130;

    /// <summary>The usage line, printed with an argument error.</summary>
    private const string Usage = "usage: dotnet run scripts/build-native.cs [--rid <runtime identifier>] [--install-tools]";

    /// <summary>The dependencies built by the xmake project, by their key in <c>build/versions.json</c>.</summary>
    /// <remarks>Each one has its definition in <c>build/&lt;key&gt;/</c> and an <c>&lt;key&gt;_source</c> option.</remarks>
    private static readonly string[] _dependencies = ["dawn", "sdl", "miniaudio"];

    /// <summary>The dependencies built as xmake packages, which need a build key (see <see cref="BuildKey"/>).</summary>
    private static readonly string[] _packages = ["dawn", "sdl"];

    /// <summary>Runs the build.</summary>
    /// <param name="scriptDirectory">The directory of the entry-point script, <c>scripts/</c> in the repository.</param>
    /// <param name="arguments">
    /// The command-line arguments: <c>--rid</c> selects the runtime identifier, the host's by default,
    /// and <c>--install-tools</c> installs the pinned xmake and CMake first.
    /// </param>
    /// <param name="output">Receives the progress and the report of the built libraries.</param>
    /// <param name="error">Receives the error messages.</param>
    /// <param name="cancellationToken">Stops the running tool.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string? scriptDirectory, IReadOnlyList<string> arguments, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        try
        {
            var (runtimeIdentifier, installTools) = ParseArguments(arguments);
            var layout = BuildLayout.FromScriptDirectory(scriptDirectory);
            var host = HostPlatform.Current;
            var target = runtimeIdentifier is null
                ? NativeTarget.ForHost(host)
                : NativeTarget.Find(runtimeIdentifier) ?? throw new InvalidDataException(
                    $"No native build is defined for '{runtimeIdentifier}': the runtime identifiers are {string.Join(", ", NativeTarget.All.Select(static target => target.RuntimeIdentifier))}.");

            if (!target.CanBuildOn(host))
            {
                throw new InvalidDataException($"{target.RuntimeIdentifier} cannot be built on {host.Key}: Linux and Windows build on their own platform and architecture, and the Apple targets on macOS.");
            }

            var versions = PinnedVersions.Load(layout.VersionsFile);

            if (installTools)
            {
                await new ToolInstaller(layout.ToolsDirectory, host, output).InstallAsync(versions.Toolchains, cancellationToken).ConfigureAwait(false);
            }

            await ToolRequirements.CheckAsync(versions.Toolchains, cancellationToken).ConfigureAwait(false);

            var toolchain = await TargetToolchain.PrepareAsync(target, host, versions, layout, cancellationToken).ConfigureAwait(false);
            var options = await FetchSourcesAsync(layout, versions, target, output, cancellationToken).ConfigureAwait(false);

            foreach (var package in _packages)
            {
                var key = BuildKey.Compute(versions.GetDependency(package).Commit, layout.XmakeProjectFile, layout.GetDefinitionDirectory(package));

                options.Add($"--{package}_key={key}");
            }

            var (outputDirectory, testOutputDirectory) = await new XmakeProject(layout, target, toolchain).BuildAsync(options, cancellationToken).ConfigureAwait(false);

            await output.WriteLineAsync($"Built {target.RuntimeIdentifier} into {outputDirectory}:".AsMemory(), cancellationToken).ConfigureAwait(false);
            await OutputCheck.VerifyAsync(outputDirectory, target.Libraries, toolchain.Symbols, output, cancellationToken).ConfigureAwait(false);
            await output.WriteLineAsync($"Built the test libraries of {target.RuntimeIdentifier} into {testOutputDirectory}:".AsMemory(), cancellationToken).ConfigureAwait(false);
            await OutputCheck.VerifyAsync(testOutputDirectory, target.TestLibraries, toolchain.Symbols, output, cancellationToken).ConfigureAwait(false);
            await SourceReport.WriteAsync(Path.Combine(layout.GetObjectDirectory(target.RuntimeIdentifier), "build"), output, cancellationToken).ConfigureAwait(false);

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
        catch (HttpRequestException exception)
        {
            await ReportErrorAsync(error, exception.Message).ConfigureAwait(false);

            return FailureExitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CancelledExitCode;
        }
    }

    /// <summary>Parses the command line.</summary>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The requested runtime identifier, if any, and whether to install the tools.</returns>
    /// <exception cref="InvalidDataException">An argument is unknown or <c>--rid</c> has no value.</exception>
    private static (string? RuntimeIdentifier, bool InstallTools) ParseArguments(IReadOnlyList<string> arguments)
    {
        string? runtimeIdentifier = null;
        var installTools = false;

        for (var index = 0; index < arguments.Count; index++)
        {
            switch (arguments[index])
            {
                case "--rid" when index + 1 < arguments.Count && runtimeIdentifier is null:
                    runtimeIdentifier = arguments[++index];
                    break;
                case "--install-tools":
                    installTools = true;
                    break;
                default:
                    throw new InvalidDataException($"Unexpected argument '{arguments[index]}'.\n{Usage}");
            }
        }

        return (runtimeIdentifier, installTools);
    }

    /// <summary>Fetches the sources of every dependency.</summary>
    /// <param name="layout">The repository layout.</param>
    /// <param name="versions">The pinned versions.</param>
    /// <param name="target">The target, which decides the <c>DEPS</c> entries of Dawn to fetch.</param>
    /// <param name="output">Receives a line for each fetch.</param>
    /// <param name="cancellationToken">Stops git.</param>
    /// <returns>The <c>&lt;key&gt;_source</c> options of the xmake project.</returns>
    private static async Task<List<string>> FetchSourcesAsync(BuildLayout layout, PinnedVersions versions, NativeTarget target, TextWriter output, CancellationToken cancellationToken)
    {
        var cache = new SourceCache(layout.SourceCacheDirectory, output);
        var dawnThirdParty = DawnThirdParty.Load(layout.DawnThirdPartyFile);
        var options = new List<string>();

        foreach (var name in _dependencies)
        {
            var depsPaths = name == "dawn" ? dawnThirdParty.GetPaths(target.XmakePlatform) : [];
            var directory = await cache.GetAsync(name, versions.GetDependency(name), depsPaths, cancellationToken).ConfigureAwait(false);

            options.Add($"--{name}_source={directory}");
        }

        return options;
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
