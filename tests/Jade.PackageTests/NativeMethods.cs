using System.Runtime.InteropServices;

namespace Jade.PackageTests;

/// <summary>Hand-written imports of the <c>jade_*</c> shims, which have no generated bindings yet (task 205).</summary>
internal static partial class NativeMethods
{
    /// <summary>The <c>JADE_NATIVE_ABI_VERSION</c> of <c>native/shims/jade_native.h</c> that these imports were written against.</summary>
    public const uint ExpectedAbiVersion = 1;

    /// <summary><c>jade_native_abi_version</c>: the ABI version compiled into the loaded library.</summary>
    /// <returns><c>JADE_NATIVE_ABI_VERSION</c> as the library was built with it.</returns>
    [LibraryImport("jade_native", EntryPoint = "jade_native_abi_version")]
    public static partial uint AbiVersion();
}
