using System.ComponentModel;
using System.Diagnostics;

namespace Jade.NativeBuild.Tools;

/// <summary>Runs the external tools of the build: git, xmake and, through xmake, CMake and the compilers.</summary>
internal static class Command
{
    /// <summary>Runs a tool that writes to the console, and fails when it fails.</summary>
    /// <param name="program">The program to run, found through <c>PATH</c>.</param>
    /// <param name="arguments">The arguments, passed as they are without shell quoting.</param>
    /// <param name="environment">
    /// The environment variables to change, or <see langword="null"/>; a <see langword="null"/> value
    /// removes the variable.
    /// </param>
    /// <param name="cancellationToken">Stops the tool and its child processes.</param>
    /// <returns>A task that completes when the tool exits successfully.</returns>
    /// <exception cref="CommandFailedException">The tool cannot be started or exits with a non-zero code.</exception>
    public static async Task RunAsync(string program, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string?>? environment, CancellationToken cancellationToken)
    {
        using var process = Start(program, arguments, environment, redirectOutput: false);

        await WaitAsync(process, program, arguments, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a tool and returns what it writes to its standard output.</summary>
    /// <param name="program">The program to run, found through <c>PATH</c>.</param>
    /// <param name="arguments">The arguments, passed as they are without shell quoting.</param>
    /// <param name="cancellationToken">Stops the tool and its child processes.</param>
    /// <returns>The standard output of the tool.</returns>
    /// <exception cref="CommandFailedException">The tool cannot be started or exits with a non-zero code.</exception>
    public static async Task<string> ReadAsync(string program, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        using var process = Start(program, arguments, environment: null, redirectOutput: true);

        // The output is read before waiting for the exit: a tool that fills the pipe would
        // otherwise never exit.
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        await WaitAsync(process, program, arguments, cancellationToken).ConfigureAwait(false);

        return output;
    }

    /// <summary>Starts a tool.</summary>
    /// <param name="program">The program to run.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="environment">The environment variables to change, or <see langword="null"/>.</param>
    /// <param name="redirectOutput">Whether the standard output is captured instead of written to the console.</param>
    /// <returns>The started process.</returns>
    /// <exception cref="CommandFailedException">The program cannot be started.</exception>
    private static Process Start(string program, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string?>? environment, bool redirectOutput)
    {
        var startInfo = new ProcessStartInfo(program)
        {
            UseShellExecute = false,
            RedirectStandardOutput = redirectOutput,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in environment ?? new Dictionary<string, string?>())
        {
            if (value is null)
            {
                _ = startInfo.Environment.Remove(name);
            }
            else
            {
                startInfo.Environment[name] = value;
            }
        }

        try
        {
            return Process.Start(startInfo) ?? throw new CommandFailedException($"'{program}' did not start.");
        }
        catch (Win32Exception exception)
        {
            throw new CommandFailedException($"'{program}' cannot be started ({exception.Message}); it must be installed and on PATH (see CONTRIBUTING.md).", exception);
        }
    }

    /// <summary>Waits for a tool to exit.</summary>
    /// <param name="process">The running tool.</param>
    /// <param name="program">The program, for the error message.</param>
    /// <param name="arguments">The arguments, for the error message.</param>
    /// <param name="cancellationToken">Stops the tool and its child processes.</param>
    /// <returns>A task that completes when the tool exits successfully.</returns>
    /// <exception cref="CommandFailedException">The tool exits with a non-zero code.</exception>
    private static async Task WaitAsync(Process process, string program, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);

            throw;
        }

        if (process.ExitCode != 0)
        {
            throw new CommandFailedException($"'{program} {string.Join(' ', arguments)}' exited with code {process.ExitCode}.");
        }
    }
}
