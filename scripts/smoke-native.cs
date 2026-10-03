#!/usr/bin/env dotnet
// Smoke check of a staged jade_native: loads artifacts/native/<rid>/lib/, calls into each bundled
// library and compares what it reports with the staged metadata and headers where it can; Dawn has
// no version function, so it creates an instance and requests adapters instead. Runs on the host
// RID only, since it has to load the library.
//
// Usage: dotnet scripts/smoke-native.cs [--rid <rid>]

#:include smoke-native/NativeMethods.cs
#:include smoke-native/AdapterRequest.cs
#:include smoke-native/WGPUAdapterInfo.cs
#:include smoke-native/WGPURequestAdapterCallbackInfo.cs
#:include smoke-native/WGPURequestAdapterOptions.cs
#:include smoke-native/WGPUStringView.cs

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

[assembly: DisableRuntimeMarshalling]

var hostRid = $"{(OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux")}-{RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant()}";
var rid = args is ["--rid", var value] ? value : args.Length == 0 ? hostRid : null;
if (rid is null)
{
    Console.Error.WriteLine("Usage: dotnet scripts/smoke-native.cs [--rid <rid>]");
    return 2;
}

if (rid != hostRid)
{
    Console.Error.WriteLine($"error: {rid} cannot be loaded on this host ({hostRid}).");
    return 2;
}

var repositoryRoot = Path.GetFullPath(Path.Combine((string)AppContext.GetData("EntryPointFileDirectoryPath")!, ".."));
var stageDirectory = Path.Combine(repositoryRoot, "artifacts", "native", rid);
var libraryName = OperatingSystem.IsWindows() ? "jade_native.dll" : OperatingSystem.IsMacOS() ? "libjade_native.dylib" : "libjade_native.so";
var libraryPath = Path.Combine(stageDirectory, "lib", libraryName);
if (!File.Exists(libraryPath))
{
    Console.Error.WriteLine($"error: {libraryPath} not found. Run `dotnet scripts/build-native.cs --rid {rid}` first.");
    return 1;
}

// Same library name as the generated bindings, resolved to the staged file instead of the probing paths.
var handle = NativeLibrary.Load(libraryPath);
NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, (name, _, _) => name == "jade_native" ? handle : 0);
Console.WriteLine($"Loaded {Path.GetRelativePath(repositoryRoot, libraryPath)}");

using var versions = JsonDocument.Parse(File.ReadAllText(Path.Combine(stageDirectory, "metadata", "versions.json")));
string ExpectedVersion(string upstream) =>
    versions.RootElement.GetProperty("upstreams").EnumerateArray().Single(u => u.GetProperty("name").GetString() == upstream).GetProperty("version").GetString()!;

var header = File.ReadAllText(Path.Combine(stageDirectory, "include", "jade", "jade_native.h"));
var expectedAbi = Regex.Match(header, @"#define JADE_NATIVE_ABI_VERSION (\d+)").Groups[1].Value;

var failures = 0;
void Check(string function, string actual, string expected, string? detail = null)
{
    var ok = actual == expected;
    failures += ok ? 0 : 1;
    Console.WriteLine($"{function}: {actual}{(detail is null ? "" : $" ({detail})")} {(ok ? "ok" : $"FAILED, expected {expected}")}");
}

unsafe
{
    var sdl = NativeMethods.SDL_GetVersion();
    Check("SDL_GetVersion", $"{sdl / 1000000}.{sdl / 1000 % 1000}.{sdl % 1000}", ExpectedVersion("sdl3"), Marshal.PtrToStringUTF8((nint)NativeMethods.SDL_GetRevision()));
    Check("ma_version_string", Marshal.PtrToStringUTF8((nint)NativeMethods.ma_version_string())!, ExpectedVersion("miniaudio"));
    Check("jade_native_abi_version", NativeMethods.jade_native_abi_version().ToString(CultureInfo.InvariantCulture), expectedAbi);

    // SDL silently leaves out a video backend whose headers were missing at build time, so the list is
    // compared with SDL's bootstrap order for the platform (src/video/SDL_video.c).
    var drivers = Enumerable.Range(0, NativeMethods.SDL_GetNumVideoDrivers()).Select(i => Marshal.PtrToStringUTF8((nint)NativeMethods.SDL_GetVideoDriver(i)));
    var expectedDrivers = OperatingSystem.IsWindows() ? "windows, offscreen, dummy"
        : OperatingSystem.IsMacOS() ? "cocoa, offscreen, dummy"
        : "wayland, x11, kmsdrm, offscreen, dummy, evdev";
    Check("SDL video drivers", string.Join(", ", drivers), expectedDrivers);

    // Dawn has no version function. The Null backend answers without a GPU, so that request also
    // passes on headless CI. The default request finds a real adapter only where a GPU and its
    // driver exist: elsewhere it completes with Unavailable, which is not a failure here.
    var instance = NativeMethods.wgpuCreateInstance(null);
    Check("wgpuCreateInstance", instance == 0 ? "null" : "non-null", "non-null");
    if (instance != 0)
    {
        const uint BackendUndefined = 0;
        const uint BackendNull = 1;
        var timeout = TimeSpan.FromSeconds(30);

        var nullRequest = AdapterRequest.Run(instance, BackendNull, timeout);
        Check("wgpuInstanceRequestAdapter (Null backend)", nullRequest.Completed ? StatusName(nullRequest.Status) : "no callback", "Success", Describe(nullRequest));

        var defaultRequest = AdapterRequest.Run(instance, BackendUndefined, timeout);
        Check("wgpuInstanceRequestAdapter (default backend)", defaultRequest.Completed ? "completed" : "no callback", "completed",
            $"{StatusName(defaultRequest.Status)}: {Describe(defaultRequest)}");

        NativeMethods.wgpuInstanceRelease(instance);
    }
}

return failures == 0 ? 0 : 1;

// Describes and releases the adapter of a successful request, or returns the request's message.
static unsafe string Describe(AdapterRequest request)
{
    if (!request.Succeeded)
    {
        return request.Message;
    }

    string[] backends = ["Undefined", "Null", "WebGPU", "D3D11", "D3D12", "Metal", "Vulkan", "OpenGL", "OpenGLES"];
    string[] adapterTypes = ["?", "DiscreteGPU", "IntegratedGPU", "CPU", "Unknown"];
    WGPUAdapterInfo info = default;
    var description = "no adapter info";
    if (NativeMethods.wgpuAdapterGetInfo(request.Adapter, &info) == 1)
    {
        description = $"{info.Device} ({backends.ElementAtOrDefault((int)info.BackendType) ?? "?"}, {adapterTypes.ElementAtOrDefault((int)info.AdapterType) ?? "?"})";
        NativeMethods.wgpuAdapterInfoFreeMembers(info);
    }

    NativeMethods.wgpuAdapterRelease(request.Adapter);
    return description;
}

static string StatusName(uint status) => status switch
{
    1 => "Success",
    2 => "CallbackCancelled",
    3 => "Unavailable",
    4 => "Error",
    _ => $"status {status}",
};
