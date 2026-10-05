using System.Security.Cryptography;
using System.Text;

namespace Jade.NativeBuild.Build;

/// <summary>Computes the key that makes xmake rebuild a package when its inputs change (see <c>build/xmake.lua</c>).</summary>
/// <remarks>
/// xmake reuses an installed package while its configuration is unchanged; it does not see a new
/// commit in the same source path or an edit of the package's definition, so both enter the key.
/// </remarks>
internal static class BuildKey
{
    /// <summary>The number of hexadecimal characters kept from the hash; enough to tell builds apart.</summary>
    private const int KeyLength = 16;

    /// <summary>Computes the key of a package.</summary>
    /// <param name="commit">The pinned commit of the package's sources.</param>
    /// <param name="projectFile">The root <c>xmake.lua</c>, whose policies every package build depends on.</param>
    /// <param name="definitionDirectory">The directory of the package's definition, such as <c>build/dawn/</c>.</param>
    /// <returns>A hexadecimal key.</returns>
    public static string Compute(string commit, string projectFile, string definitionDirectory)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        Append(hash, Encoding.UTF8.GetBytes(commit));
        Append(hash, File.ReadAllBytes(projectFile));

        var files = Directory.EnumerateFiles(definitionDirectory, "*", SearchOption.AllDirectories)
            .Select(file => (Name: Path.GetRelativePath(definitionDirectory, file).Replace('\\', '/'), Path: file))
            .OrderBy(static file => file.Name, StringComparer.Ordinal);

        foreach (var file in files)
        {
            Append(hash, Encoding.UTF8.GetBytes(file.Name));
            Append(hash, File.ReadAllBytes(file.Path));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset())[..KeyLength];
    }

    /// <summary>Adds a length-prefixed value to a hash, so that two different sequences of values never hash alike.</summary>
    /// <param name="hash">The hash being computed.</param>
    /// <param name="value">The value.</param>
    private static void Append(IncrementalHash hash, byte[] value)
    {
        hash.AppendData(BitConverter.GetBytes((long)value.Length));
        hash.AppendData(value);
    }
}
