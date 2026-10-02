using System.Text;

/// <summary>Writes generated files into a folder that only holds generated files.</summary>
internal static class GeneratedFiles
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Makes <paramref name="directory"/> hold exactly <paramref name="files"/> among its <c>*.g.cs</c> files.</summary>
    /// <param name="directory">The output folder, created if missing.</param>
    /// <param name="files">File contents by file name, with LF line endings.</param>
    /// <returns>The number of files created, changed or deleted.</returns>
    public static int Write(string directory, IReadOnlyDictionary<string, string> files)
    {
        Directory.CreateDirectory(directory);
        var changes = 0;
        foreach (var stale in Directory.EnumerateFiles(directory, "*.g.cs").Where(f => !files.ContainsKey(Path.GetFileName(f))).Order(StringComparer.Ordinal))
        {
            File.Delete(stale);
            changes++;
        }

        foreach (var (name, content) in files)
        {
            // Unchanged files keep their timestamp, so incremental builds stay incremental.
            var path = Path.Combine(directory, name);
            if (!File.Exists(path) || File.ReadAllText(path, Utf8) != content)
            {
                File.WriteAllText(path, content, Utf8);
                changes++;
            }
        }

        return changes;
    }
}
