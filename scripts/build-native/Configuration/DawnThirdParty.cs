using System.Text.Json.Serialization;

namespace Jade.NativeBuild.Configuration;

/// <summary>The entries of Dawn's <c>DEPS</c> file that its build needs, read from <c>build/dawn/deps.json</c>.</summary>
/// <remarks>
/// Dawn's own fetch script clones every dependency of its tests and tools as well. The list follows
/// the CMake options of <c>build/dawn/xmake.lua</c>: a missing entry fails Dawn's configuration, an
/// extra one is fetched for nothing.
/// </remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record DawnThirdParty
{
    /// <summary>Gets the paths of the entries, as keyed in the <c>deps</c> of the <c>DEPS</c> file.</summary>
    public required IReadOnlyList<string> Paths { get; init; }

    /// <summary>Reads <c>build/dawn/deps.json</c>.</summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The entries to fetch.</returns>
    /// <exception cref="InvalidDataException">The file does not match the expected format.</exception>
    public static DawnThirdParty Load(string path)
    {
        return JsonFile.Read(path, BuildJsonContext.Default.DawnThirdParty);
    }
}
