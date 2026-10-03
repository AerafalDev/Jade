#!/usr/bin/env dotnet
// Smoke check of a staged jade_native: loads artifacts/native/<rid>/lib/, calls into each bundled
// library and compares what it reports with the staged metadata and headers where it can; Dawn has
// no version function, so it creates an instance and requests adapters instead. ImGui also renders
// on its null backends, and its exports are compared with the staged dear_bindings metadata.
// Runs on the RID of the process only, since it has to load the library: an x64 runtime under
// Rosetta 2 checks osx-x64 on an arm64 Mac.
//
// Usage: dotnet scripts/smoke-native.cs [--rid <rid>]

#:include smoke-native/NativeMethods.cs
#:include smoke-native/AdapterRequest.cs
#:include smoke-native/ImDrawData.cs
#:include smoke-native/ImGuiIO.cs
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

// The process's architecture, not the machine's: under Rosetta 2, OSArchitecture reports arm64.
var processRid = $"{(OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux")}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
var rid = args is ["--rid", var value] ? value : args.Length == 0 ? processRid : null;
if (rid is null)
{
    Console.Error.WriteLine("Usage: dotnet scripts/smoke-native.cs [--rid <rid>]");
    return 2;
}

if (rid != processRid)
{
    Console.Error.WriteLine($"error: {rid} cannot be loaded by this process ({processRid}).");
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
JsonElement Upstream(string name) =>
    versions.RootElement.GetProperty("upstreams").EnumerateArray().Single(u => u.GetProperty("name").GetString() == name);
string ExpectedVersion(string upstream) => Upstream(upstream).GetProperty("version").GetString()!;
string ExpectedResourceVersion(string upstream, string resource) =>
    Upstream(upstream).GetProperty("resources").EnumerateArray().Single(r => r.GetProperty("name").GetString() == resource).GetProperty("version").GetString()!;

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

    // The docking branch's tags add -docking to the version ImGui reports.
    Check("ImGui_GetVersion", Marshal.PtrToStringUTF8((nint)NativeMethods.ImGui_GetVersion())!, ExpectedVersion("imgui").Replace("-docking", "", StringComparison.Ordinal));
    Check("DearBindings_GetVersion", Marshal.PtrToStringUTF8((nint)NativeMethods.DearBindings_GetVersion())!, ExpectedResourceVersion("imgui", "dear_bindings"));

    // The null backends need neither a window nor a GPU, and their renderer marks the font texture as
    // uploaded. A new window is hidden during its first frame, while ImGui measures it, so two frames run.
    var context = NativeMethods.ImGui_CreateContext(null);
    // Otherwise ImGui saves its settings to imgui.ini in the working directory.
    NativeMethods.ImGui_GetIO()->IniFilename = null;
    var initialized = NativeMethods.cImGui_ImplNull_Init() != 0;
    var visible = false;
    ImDrawData* drawData = null;
    for (var frame = 0; frame < 2; frame++)
    {
        NativeMethods.cImGui_ImplNull_NewFrame();
        NativeMethods.ImGui_NewFrame();
        fixed (byte* title = "smoke-native"u8, text = "Hello from jade_native"u8)
        {
            visible = NativeMethods.ImGui_Begin(title, null, 0) != 0;
            NativeMethods.ImGui_TextUnformatted(text);
        }

        NativeMethods.ImGui_End();
        NativeMethods.ImGui_Render();
        drawData = NativeMethods.ImGui_GetDrawData();
        NativeMethods.cImGui_ImplNullRender_RenderDrawData(drawData);
    }

    Check("ImGui frames (null backends)", initialized && visible && drawData->Valid && drawData->TotalVtxCount > 0 ? "rendered" : "not rendered", "rendered",
        $"{drawData->CmdListsSize} draw list(s), {drawData->TotalVtxCount} vertices, {drawData->TotalIdxCount} indices");
    NativeMethods.cImGui_ImplNull_Shutdown();
    NativeMethods.ImGui_DestroyContext(context);

    // Every function of the dear_bindings metadata is exported exactly when its preprocessor conditionals
    // hold for the defines ImGui was built with; the other macros they test are undefined on desktop.
    var imguiDefines = Upstream("imgui").GetProperty("defines").EnumerateArray().Select(d => d.GetString()!).ToHashSet(StringComparer.Ordinal);
    var listed = 0;
    var expectedExports = 0;
    var wrongExports = new List<string>();
    foreach (var file in (string[])["dcimgui.json", "dcimgui_impl_sdl3.json", "dcimgui_impl_wgpu.json", "dcimgui_impl_null.json"])
    {
        using var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(stageDirectory, "metadata", file)));
        foreach (var function in metadata.RootElement.GetProperty("functions").EnumerateArray())
        {
            var name = function.GetProperty("name").GetString()!;
            var expected = !function.TryGetProperty("conditionals", out var conditionals) || conditionals.EnumerateArray().All(c => Holds(c, imguiDefines));
            listed++;
            expectedExports += expected ? 1 : 0;
            if (NativeLibrary.TryGetExport(handle, name, out _) != expected)
            {
                wrongExports.Add(name);
            }
        }
    }

    Check("dear_bindings exports", wrongExports.Count == 0 ? $"{expectedExports} of {listed}" : $"{wrongExports.Count} wrong: {string.Join(", ", wrongExports)}",
        $"{expectedExports} of {listed}", "functions exported exactly when their conditionals hold");
}

return failures == 0 ? 0 : 1;

// Evaluates one conditional of the dear_bindings metadata (its docs/MetadataFormat.md) against the build's defines.
static bool Holds(JsonElement conditional, HashSet<string> defines)
{
    var expression = conditional.GetProperty("expression").GetString()!;
    var defined = Regex.Match(expression, @"^defined\((\w+)\)$");
    return conditional.GetProperty("condition").GetString() switch
    {
        "ifdef" => defines.Contains(expression),
        "ifndef" => !defines.Contains(expression),
        "if" when defined.Success => defines.Contains(defined.Groups[1].Value),
        "ifnot" when defined.Success => !defines.Contains(defined.Groups[1].Value),
        var condition => throw new InvalidOperationException($"unknown dear_bindings conditional `{condition} {expression}`"),
    };
}

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
