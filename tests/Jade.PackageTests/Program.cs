using Jade.Interop.Sdl3;
using Jade.PackageTests;

// Loads jade_native from the Jade.Native package twice: through a local import, and through the bindings that the Jade
// package ships in Jade.Interop.dll. Any mismatch fails the run with exit code 1.

// The .NET host hands the runtime the native directories listed in the .deps.json; a NativeAOT binary runs without a
// host. RuntimeFeature cannot tell them apart: PublishAot turns IsDynamicCodeSupported off under the JIT as well.
var nativeSearchDirectories = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string;
Console.WriteLine(nativeSearchDirectories is null ? "Runtime: NativeAOT" : $"Runtime: JIT, native search directories {nativeSearchDirectories}");

var failed = false;

var abiVersion = NativeMethods.AbiVersion();
Console.WriteLine($"jade_native_abi_version: {abiVersion}");
if (abiVersion != NativeMethods.ExpectedAbiVersion)
{
    Console.Error.WriteLine($"error: expected ABI version {NativeMethods.ExpectedAbiVersion}.");
    failed = true;
}

var sdlVersion = Sdl.GetVersion();
Console.WriteLine($"SDL_GetVersion: {Sdl.VersionNumMajor(sdlVersion)}.{Sdl.VersionNumMinor(sdlVersion)}.{Sdl.VersionNumMicro(sdlVersion)}");
if (sdlVersion != Sdl.Version)
{
    Console.Error.WriteLine($"error: Jade.Interop was generated from SDL {Sdl.MajorVersion}.{Sdl.MinorVersion}.{Sdl.MicroVersion}.");
    failed = true;
}

return failed ? 1 : 0;
