using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Jade.Tests;

[TestClass]
internal sealed class BuildConventionTests
{
    [TestMethod]
    [DataRow("Jade")]
    [DataRow("Jade.Wgpu")]
    [DataRow("Jade.Sdl")]
    [DataRow("Jade.MiniAudio")]
    [DataRow("Jade.Emscripten")]
    public void LibraryIsAotCompatible(string assemblyName)
    {
        var metadata = Assembly.Load(assemblyName).GetCustomAttributes<AssemblyMetadataAttribute>();

        Assert.IsTrue(metadata.Any(static attribute => attribute is { Key: "IsAotCompatible", Value: "True" }));
    }

    [TestMethod]
    [DataRow("Jade.Wgpu")]
    [DataRow("Jade.Sdl")]
    [DataRow("Jade.MiniAudio")]
    public void InteropLibraryDisablesRuntimeMarshalling(string assemblyName)
    {
        Assert.IsNotNull(Assembly.Load(assemblyName).GetCustomAttribute<DisableRuntimeMarshallingAttribute>());
    }

    [TestMethod]
    public void BrowserInteropIsBrowserOnly()
    {
        var platform = Assembly.Load("Jade.Emscripten").GetCustomAttribute<SupportedOSPlatformAttribute>();

        Assert.AreEqual("browser", platform?.PlatformName);
    }
}
