using System.Globalization;
using ClangSharp.Interop;

namespace Jade.BindingGenerator.Clang;

/// <summary>Guards which libclang the generator parses with.</summary>
internal static class LibClang
{
    /// <summary>Checks that the loaded libclang is the one of the pinned ClangSharp package.</summary>
    /// <exception cref="InvalidDataException">Another libclang, such as one installed on the system, was loaded.</exception>
    /// <remarks>
    /// ClangSharp falls back to a system libclang when the package's native library is missing. Its
    /// output would then depend on the machine, which the regeneration check forbids (ADR 0026).
    /// </remarks>
    public static void EnsurePackageVersion()
    {
        using var version = clang.getClangVersion();
        var expected = string.Create(CultureInfo.InvariantCulture, $"clang version {clang.MajorVersion}.{clang.MinorVersion}.");

        if (!version.ToString().StartsWith(expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Loaded '{version}', but the ClangSharp package expects libclang {clang.MajorVersion}.{clang.MinorVersion}.");
        }
    }
}
