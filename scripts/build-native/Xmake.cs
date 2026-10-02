using System.ComponentModel;
using System.Diagnostics;

/// <summary>Runs the xmake CLI.</summary>
internal static class Xmake
{
    /// <summary>Runs <c>xmake</c> with <paramref name="arguments"/> in the project directory, its output going to the console.</summary>
    /// <param name="projectDirectory">The directory holding xmake.lua.</param>
    /// <param name="arguments">Arguments, passed without shell quoting.</param>
    /// <exception cref="InvalidOperationException">xmake cannot start or exits with a non-zero code.</exception>
    public static void Run(string projectDirectory, IReadOnlyList<string> arguments)
    {
        // Not `-P`: with it, xmake keeps its configuration under the current directory and resolves the
        // build directory against it, so outputs land outside native/ when run from the repository root.
        var startInfo = new ProcessStartInfo("xmake") { UseShellExecute = false, WorkingDirectory = projectDirectory };
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
            throw new InvalidOperationException($"cannot start xmake ({e.Message}). Install xmake and put it on PATH.", e);
        }

        using (process)
        {
            if (process is null)
            {
                throw new InvalidOperationException("cannot start xmake.");
            }

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"`xmake {string.Join(' ', arguments)}` failed with exit code {process.ExitCode}.");
            }
        }
    }
}
