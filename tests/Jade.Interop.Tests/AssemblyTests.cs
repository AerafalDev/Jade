using System.Reflection;
using System.Runtime.CompilerServices;
using Shouldly;
using Xunit;

namespace Jade.Interop.Tests;

public sealed class AssemblyTests
{
    [Fact]
    public void Interop_assembly_disables_runtime_marshalling()
    {
        var assembly = Assembly.Load("Jade.Interop");

        assembly.GetCustomAttribute<DisableRuntimeMarshallingAttribute>().ShouldNotBeNull();
    }
}
