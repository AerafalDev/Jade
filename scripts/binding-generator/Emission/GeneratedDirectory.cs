using System.Text;

namespace Jade.BindingGenerator.Emission;

/// <summary>Brings a <c>Generated/</c> directory in line with the files a run produces.</summary>
internal static class GeneratedDirectory
{
    /// <summary>The pattern of the files the generator owns, in the directory and its <c>Raw/</c> subdirectory; they hold nothing else (ADR 0023).</summary>
    private const string GeneratedFilePattern = "*.g.cs";

    /// <summary>The encoding of the generated files: UTF-8 without a byte order mark.</summary>
    private static readonly UTF8Encoding _encoding = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Writes the files that are new or differ and deletes the generated files that are no longer produced.</summary>
    /// <param name="directory">The directory.</param>
    /// <param name="files">The generated files.</param>
    /// <returns>What changed.</returns>
    /// <exception cref="InvalidDataException">Two files would only differ in case, which some file systems cannot hold.</exception>
    /// <remarks>
    /// Unchanged files are left untouched so that their timestamps do not trigger rebuilds. Stale
    /// files are deleted first, so that a file whose name only changes in case is recreated on a
    /// case-insensitive file system.
    /// </remarks>
    public static GeneratedDirectoryUpdate Update(string directory, IReadOnlyList<GeneratedFile> files)
    {
        var names = files.Select(static file => file.Name).ToHashSet(StringComparer.Ordinal);

        if (files.GroupBy(static file => file.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(static group => group.Count() > 1) is { } collision)
        {
            throw new InvalidDataException($"Several generated files are named '{collision.Key}' when case is ignored.");
        }

        _ = Directory.CreateDirectory(directory);

        var deleted = 0;

        foreach (var path in Directory.EnumerateFiles(directory, GeneratedFilePattern, SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList())
        {
            if (!names.Contains(Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/')))
            {
                File.Delete(path);
                deleted++;
            }
        }

        var written = 0;

        foreach (var file in files)
        {
            var path = Path.Combine(directory, file.Name);
            var content = _encoding.GetBytes(file.Content);

            _ = Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
            {
                File.WriteAllBytes(path, content);
                written++;
            }
        }

        return new GeneratedDirectoryUpdate(files.Count, written, deleted);
    }
}
