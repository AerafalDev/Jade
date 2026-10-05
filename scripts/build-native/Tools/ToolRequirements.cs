using System.Text.RegularExpressions;
using Jade.NativeBuild.Configuration;

namespace Jade.NativeBuild.Tools;

/// <summary>Checks the tools the build runs before it starts, so that a missing one fails early and clearly.</summary>
internal static partial class ToolRequirements
{
    /// <summary>The tools that must be installed, at any version.</summary>
    /// <remarks>
    /// Their output does not depend on their version the way it depends on the pinned xmake and
    /// CMake: git fetches by commit hash, Ninja runs the commands CMake writes, and Python 3 runs
    /// Dawn's code generators.
    /// </remarks>
    private static readonly string[] _unpinnedTools = ["git", "ninja", "python3"];

    /// <summary>Checks that xmake and CMake are at the pinned versions and that the other tools are installed.</summary>
    /// <param name="toolchains">The tools pinned in <c>build/versions.json</c>.</param>
    /// <param name="cancellationToken">Stops the version queries.</param>
    /// <returns>A task that completes when every tool is checked.</returns>
    /// <exception cref="InvalidDataException">xmake or CMake reports another version than the pinned one.</exception>
    /// <exception cref="CommandFailedException">A tool is missing.</exception>
    public static async Task CheckAsync(PinnedToolchains toolchains, CancellationToken cancellationToken)
    {
        await CheckVersionAsync("xmake", XmakeVersion(), toolchains.Xmake.Version, cancellationToken).ConfigureAwait(false);
        await CheckVersionAsync("cmake", CmakeVersion(), toolchains.Cmake.Version, cancellationToken).ConfigureAwait(false);

        foreach (var tool in _unpinnedTools)
        {
            _ = await Command.ReadAsync(tool, ["--version"], cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Checks the version a tool reports.</summary>
    /// <param name="tool">The tool to run with <c>--version</c>.</param>
    /// <param name="pattern">The pattern that captures the version in the tool's output.</param>
    /// <param name="expected">The pinned version.</param>
    /// <param name="cancellationToken">Stops the query.</param>
    /// <returns>A task that completes when the version is checked.</returns>
    /// <exception cref="InvalidDataException">The tool reports another version, or none.</exception>
    private static async Task CheckVersionAsync(string tool, Regex pattern, string expected, CancellationToken cancellationToken)
    {
        var output = await Command.ReadAsync(tool, ["--version"], cancellationToken).ConfigureAwait(false);
        var match = pattern.Match(output);

        if (!match.Success)
        {
            throw new InvalidDataException($"'{tool} --version' reports no version.");
        }

        var actual = match.Groups["version"].Value;

        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"build/versions.json pins {tool} {expected}, but '{tool} --version' reports {actual}.");
        }
    }

    /// <summary>Matches the version in the output of <c>xmake --version</c>, such as <c>xmake v3.1.1+20260827</c>.</summary>
    /// <returns>The pattern, with the version in the <c>version</c> group.</returns>
    [GeneratedRegex(@"xmake v(?<version>\d+\.\d+\.\d+)")]
    private static partial Regex XmakeVersion();

    /// <summary>Matches the version in the output of <c>cmake --version</c>, such as <c>cmake version 4.4.4</c>.</summary>
    /// <returns>The pattern, with the version in the <c>version</c> group.</returns>
    [GeneratedRegex(@"cmake version (?<version>\d+\.\d+\.\d+)")]
    private static partial Regex CmakeVersion();
}
