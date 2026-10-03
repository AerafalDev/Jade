using System.ComponentModel;
using System.Diagnostics;

/// <summary>Runs the GitHub CLI.</summary>
internal static class Gh
{
    /// <summary>Fails unless gh is installed and signed in, with a message that says which.</summary>
    /// <param name="workingDirectory">A directory inside the repository.</param>
    /// <exception cref="InvalidOperationException">gh is missing or not authenticated.</exception>
    public static void EnsureAuthenticated(string workingDirectory)
    {
        var (exitCode, _, _) = Start(workingDirectory, ["auth", "status"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException("gh is not authenticated. Run `gh auth login`, or set GH_TOKEN.");
        }
    }

    /// <summary>Runs gh and returns its standard output.</summary>
    /// <param name="workingDirectory">A directory inside the repository, which gh takes the GitHub repository from.</param>
    /// <param name="arguments">Arguments, passed without shell quoting.</param>
    /// <returns>The standard output, trimmed.</returns>
    /// <exception cref="InvalidOperationException">gh is missing or exits with a non-zero code.</exception>
    public static string Run(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var (exitCode, output, error) = Start(workingDirectory, arguments);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"`gh {string.Join(' ', arguments)}` failed with exit code {exitCode}: {error.Trim()}");
        }

        return output.Trim();
    }

    private static (int ExitCode, string Output, string Error) Start(string workingDirectory, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("gh")
        {
            UseShellExecute = false,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
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
            throw new InvalidOperationException($"cannot start gh ({e.Message}). Install the GitHub CLI (https://cli.github.com) and put it on PATH.", e);
        }

        using (process)
        {
            if (process is null)
            {
                throw new InvalidOperationException("cannot start gh.");
            }

            // Both pipes are drained together so that neither can fill up and block gh.
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            Task.WaitAll(output, error);
            process.WaitForExit();
            return (process.ExitCode, output.Result, error.Result);
        }
    }
}
