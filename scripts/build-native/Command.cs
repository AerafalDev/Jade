using System.ComponentModel;
using System.Diagnostics;

/// <summary>Runs the external tools of the native build: xmake, and docker for container builds.</summary>
internal static class Command
{
    /// <summary>Runs <paramref name="program"/> with its output going to the console.</summary>
    /// <param name="program">The executable, looked up on PATH.</param>
    /// <param name="workingDirectory">The directory to run it in.</param>
    /// <param name="arguments">Arguments, passed without shell quoting.</param>
    /// <param name="installHint">What to tell the user when the program cannot start.</param>
    /// <exception cref="InvalidOperationException">The program cannot start or exits with a non-zero code.</exception>
    public static void Run(string program, string workingDirectory, IReadOnlyList<string> arguments, string installHint)
    {
        var exitCode = Start(program, workingDirectory, arguments, installHint, quiet: false);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"`{program} {string.Join(' ', arguments)}` failed with exit code {exitCode}.");
        }
    }

    /// <summary>Runs <paramref name="program"/> without showing its output.</summary>
    /// <param name="program">The executable, looked up on PATH.</param>
    /// <param name="workingDirectory">The directory to run it in.</param>
    /// <param name="arguments">Arguments, passed without shell quoting.</param>
    /// <param name="installHint">What to tell the user when the program cannot start.</param>
    /// <returns>Whether it exited with code 0.</returns>
    /// <exception cref="InvalidOperationException">The program cannot start.</exception>
    public static bool Succeeds(string program, string workingDirectory, IReadOnlyList<string> arguments, string installHint) =>
        Start(program, workingDirectory, arguments, installHint, quiet: true) == 0;

    private static int Start(string program, string workingDirectory, IReadOnlyList<string> arguments, string installHint, bool quiet)
    {
        var startInfo = new ProcessStartInfo(program)
        {
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = quiet,
            RedirectStandardError = quiet,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Win32Exception e)
        {
            throw new InvalidOperationException($"cannot start {program} ({e.Message}). {installHint}", e);
        }

        using (process)
        {
            if (process is null)
            {
                throw new InvalidOperationException($"cannot start {program}.");
            }

            if (quiet)
            {
                // Both pipes are drained so that a chatty program cannot block on a full buffer.
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                Task.WaitAll(output, error);
            }

            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
