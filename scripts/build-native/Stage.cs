using System.Text.Json;

/// <summary>Fills artifacts/native/&lt;rid&gt;/ from a build manifest.</summary>
internal static class Stage
{
    /// <summary>Recreates <paramref name="stageDirectory"/> from <paramref name="manifest"/>.</summary>
    /// <param name="manifest">The manifest xmake wrote after the build.</param>
    /// <param name="rid">The RID that was built, recorded in versions.json.</param>
    /// <param name="config">The configuration that was built, recorded in versions.json.</param>
    /// <param name="stageDirectory">artifacts/native/&lt;rid&gt;/, deleted first.</param>
    /// <returns>The path of the staged library.</returns>
    /// <exception cref="InvalidOperationException">A stage fragment is missing, or two of them stage the same file.</exception>
    public static string Run(Manifest manifest, string rid, string config, string stageDirectory)
    {
        if (Directory.Exists(stageDirectory))
        {
            Directory.Delete(stageDirectory, recursive: true);
        }

        var libraryDirectory = Directory.CreateDirectory(Path.Combine(stageDirectory, "lib")).FullName;
        var includeDirectory = Directory.CreateDirectory(Path.Combine(stageDirectory, "include")).FullName;
        var metadataDirectory = Directory.CreateDirectory(Path.Combine(stageDirectory, "metadata")).FullName;
        var licensesDirectory = Directory.CreateDirectory(Path.Combine(metadataDirectory, "licenses")).FullName;

        var library = Path.Combine(libraryDirectory, Path.GetFileName(manifest.Library));
        File.Copy(manifest.Library, library);

        foreach (var header in manifest.Headers)
        {
            CopyFile(header.Source, Path.Combine(includeDirectory, header.Destination));
        }

        var upstreams = new List<Upstream>();
        foreach (var package in manifest.Packages)
        {
            // Stage fragment written by native/modules/stage.lua when the package was installed.
            var fragment = Path.Combine(package.InstallDirectory, "jade");
            CopyTree(Path.Combine(fragment, "include"), includeDirectory);
            CopyTree(Path.Combine(fragment, "licenses"), licensesDirectory);
            upstreams.Add(Json.Read(Path.Combine(fragment, "upstream.json"), JsonContext.Default.Upstream));
        }

        upstreams.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        var versions = new Versions { Rid = rid, Config = config, Upstreams = upstreams };
        File.WriteAllText(Path.Combine(metadataDirectory, "versions.json"), JsonSerializer.Serialize(versions, JsonContext.Default.Versions) + "\n");
        return library;
    }

    private static void CopyTree(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            throw new InvalidOperationException($"missing stage directory {source}.");
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            CopyFile(file, Path.Combine(destination, Path.GetRelativePath(source, file)));
        }
    }

    // Never overwrites: two libraries staging the same path is a layout bug, not something to settle by order.
    private static void CopyFile(string source, string destination)
    {
        if (File.Exists(destination))
        {
            throw new InvalidOperationException($"{destination} is staged twice.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source, destination);
    }
}
