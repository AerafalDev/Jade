using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Jade.Sdl.Raw;

namespace Jade.Sdl.Tests;

/// <summary>
/// Checks that the host's library exports every function the raw layer imports for this platform:
/// a declaration that the headers declare but the library does not export would only fail when
/// first called.
/// </summary>
[TestClass]
internal sealed class ExportTests
{
    private const string LibraryName = "SDL3";

    [TestMethod]
    public void TheLibraryExportsEveryFunctionOfThisPlatform()
    {
        if (!NativeLibrary.TryLoad(LibraryName, typeof(Window).Assembly, null, out var library))
        {
            Assert.Inconclusive($"The {LibraryName} library is not built for this host: run 'dotnet run scripts/build-native.cs'.");
        }

        var entryPoints = typeof(NativeMethods)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(IsAvailableOnThisPlatform)
            .Select(static method => method.GetCustomAttribute<LibraryImportAttribute>()?.EntryPoint)
            .OfType<string>()
            .ToList();
        var missing = entryPoints.Where(entryPoint => !NativeLibrary.TryGetExport(library, entryPoint, out _)).Order(StringComparer.Ordinal).ToList();

        Assert.IsNotEmpty(entryPoints);
        Assert.IsEmpty(missing, $"Not exported: {string.Join(", ", missing)}");
    }

    private static bool IsAvailableOnThisPlatform(MethodInfo method)
    {
        var supported = method.GetCustomAttributes<SupportedOSPlatformAttribute>().ToList();

        return (supported.Count == 0 || supported.Exists(static attribute => OperatingSystem.IsOSPlatform(attribute.PlatformName)))
            && !method.GetCustomAttributes<UnsupportedOSPlatformAttribute>().Any(static attribute => OperatingSystem.IsOSPlatform(attribute.PlatformName));
    }
}
