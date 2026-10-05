using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]

// The native libraries ship next to the application (runtimes/<rid>/native, or beside a NativeAOT
// executable), and on Linux and macOS only AssemblyDirectory probes there; SafeDirectories keeps the
// current directory and PATH out of the Windows search. CA5393 counts AssemblyDirectory as unsafe.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
[assembly: SuppressMessage("Security", "CA5393:Do not use unsafe DllImportSearchPath value", Justification = "The native libraries are deployed next to the assembly.", Scope = "type", Target = "~T:Jade.Sdl.Raw.NativeMethods")]

[assembly: InternalsVisibleTo("Jade.Sdl.Tests")]
