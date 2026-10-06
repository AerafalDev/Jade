using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Jade.Tests;

[TestClass]
internal sealed partial class NativeVersionsTests
{
    private static readonly JsonElement _versions = LoadVersions();

    [TestMethod]
    public void EveryEntryCitesItsSource()
    {
        var entries = _versions.GetProperty("dependencies").EnumerateObject()
            .Concat(_versions.GetProperty("toolchains").EnumerateObject())
            .Select(static property => (property.Name, property.Value))
            .Append((Name: "minimumOs", Value: _versions.GetProperty("minimumOs")));

        var withoutSource = entries
            .Where(static entry => !entry.Value.TryGetProperty("source", out var source) || string.IsNullOrWhiteSpace(source.GetString()))
            .Select(static entry => entry.Name)
            .ToList();

        Assert.IsEmpty(withoutSource, $"Entries without a source: {string.Join(", ", withoutSource)}");
    }

    [TestMethod]
    public void DependenciesArePinnedToFullCommitHashes()
    {
        var notPinned = _versions.GetProperty("dependencies").EnumerateObject()
            .Where(static dependency => !dependency.Value.TryGetProperty("commit", out var commit) || !CommitHash().IsMatch(commit.GetString() ?? string.Empty))
            .Select(static dependency => dependency.Name)
            .ToList();

        Assert.IsEmpty(notPinned, $"Dependencies without a full commit hash: {string.Join(", ", notPinned)}");
    }

    [TestMethod]
    [DataRow("Jade.Native.Wgpu")]
    [DataRow("Jade.Native.Sdl")]
    [DataRow("Jade.Native.MiniAudio")]
    public void NativePackageRequiresTheMinimumOsVersions(string package)
    {
        var minimumOs = _versions.GetProperty("minimumOs");
        var prefix = "_" + package.Replace(".", string.Empty, StringComparison.Ordinal);
        var properties = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "native", package + ".targets"))
            .Descendants("PropertyGroup")
            .Elements()
            .ToDictionary(static property => property.Name.LocalName, static property => property.Value, StringComparer.Ordinal);

        Assert.AreEqual(minimumOs.GetProperty("ios").GetString(), properties[prefix + "MinimumIOSVersion"]);
        Assert.AreEqual(string.Create(CultureInfo.InvariantCulture, $"{minimumOs.GetProperty("androidApiLevel").GetInt32()}.0"), properties[prefix + "MinimumAndroidVersion"]);
    }

    private static JsonElement LoadVersions()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "versions.json")));

        return document.RootElement.Clone();
    }

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex CommitHash();
}
