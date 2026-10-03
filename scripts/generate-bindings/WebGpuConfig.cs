/// <summary>
/// The WebGPU bindings: <c>Jade.Interop.WebGpu</c>, functions and constants in <c>Wgpu</c>. They are read from the staged
/// <c>dawn.json</c> (ADR-0001, ADR-0005), and <c>webgpu/webgpu.h</c> from the same Dawn revision cross-checks them.
/// </summary>
internal static class WebGpuConfig
{
    /// <summary>Builds the config.</summary>
    /// <returns>The WebGPU config.</returns>
    public static LibraryConfig Create() => new()
    {
        Name = "WebGpu",
        Upstream = "dawn",
        IncludeDirectory = "dawn",
        ApiDescription = "dawn.json",
        CrossCheckHeader = "webgpu/webgpu.h",
        FunctionsClass = "Wgpu",
        Prefixes = ["wgpu"],
        ExportPrefixes = ["wgpu"],

        // WGPUBool is a uint32_t that C code compares with 0 (ADR-0012). uint keeps that width and that test, and passes
        // like the C type on every ABI. A 4-byte wrapper struct would read better, but its by-value passing in the
        // signatures that take or return WGPUBool would rest on how each ABI passes a one-field struct, which 104 and 105
        // have yet to check for handles; an enum would add a type and still need `!= 0`.
        TypedefMappings = new Dictionary<string, PrimitiveType>
        {
            ["WGPUBool"] = PrimitiveType.UInt32,
        },

        Exclusions = new Dictionary<string, string>
        {
            ["emscripten_webgpu_get_device"] = "declared by Dawn's api.h template outside dawn.json, and only defined in Emscripten builds",
            ["WGPUINTERNAL_HAVE_EMDAWNWEBGPU_HEADER"] = "a marker that C code tests with #ifdef to tell emdawnwebgpu's header apart; it holds no data",
        },

        Renames = new Dictionary<string, string>
        {
            // With the SDK's implicit `using System;`, `Buffer` would be ambiguous with System.Buffer in user code.
            // ADR-0006 already writes it GpuBuffer.
            ["WGPUBuffer"] = "GpuBuffer",

            // A member cannot share its struct's name (CS0542); the field holds a WGPUTextureViewDimension.
            ["WGPUTextureBindingViewDimension.textureBindingViewDimension"] = "ViewDimension",

            // C# names cannot start with a digit; the C++ API writes these e1D, e2D, ...
            ["WGPUTextureDimension.WGPUTextureDimension_1D"] = "Dimension1D",
            ["WGPUTextureDimension.WGPUTextureDimension_2D"] = "Dimension2D",
            ["WGPUTextureDimension.WGPUTextureDimension_3D"] = "Dimension3D",
            ["WGPUTextureViewDimension.WGPUTextureViewDimension_1D"] = "Dimension1D",
            ["WGPUTextureViewDimension.WGPUTextureViewDimension_2D"] = "Dimension2D",
            ["WGPUTextureViewDimension.WGPUTextureViewDimension_2DArray"] = "Dimension2DArray",
            ["WGPUTextureViewDimension.WGPUTextureViewDimension_3D"] = "Dimension3D",

            // dawn.json writes these values with underscores, which public C# names avoid (CA1707).
            ["WGPUColorSpaceTransferDawn.WGPUColorSpaceTransferDawn_BT_1886"] = "BT1886",
            ["WGPUColorSpaceTransferDawn.WGPUColorSpaceTransferDawn_SMPTE_170M"] = "SMPTE170M",
            ["WGPUVertexFormat.WGPUVertexFormat_Snorm10_10_10_2"] = "Snorm1010102",
            ["WGPUVertexFormat.WGPUVertexFormat_Unorm10_10_10_2"] = "Unorm1010102",
        },

        // emdawnwebgpu's header declares these, but its library_webgpu.js aborts in them at the pinned revision.
        UnsupportedPlatforms = new Dictionary<string, IReadOnlyList<string>>
        {
            ["wgpuGetProcAddress"] = ["browser"],
            ["wgpuSurfacePresent"] = ["browser"],
        },

        // jade_native is built WGSL-only (task 102): Dawn still reports SPIR-V input as an instance feature.
        Notes = new Dictionary<string, string>
        {
            ["wgpuGetProcAddress"] = "emdawnwebgpu declares it but aborts in it (`wgpuGetProcAddress unimplemented`).",
            ["wgpuSurfacePresent"] = "emdawnwebgpu declares it but aborts in it: in a browser, the canvas is presented after each `requestAnimationFrame` callback.",
            ["WGPUShaderSourceSPIRV"] = "jade_native is built WGSL-only: a shader module created from SPIR-V fails validation with `SPIR-V is disallowed.`, " +
                "although `wgpuHasInstanceFeature` reports `WGPUInstanceFeatureName_ShaderSourceSPIRV`.",
            ["WGPUDawnShaderSourceSPIRV"] = "jade_native is built WGSL-only: a shader module created from SPIR-V fails validation with `SPIR-V is disallowed.`.",
            ["WGPUInstanceFeatureName_ShaderSourceSPIRV"] = "jade_native reports it but is built WGSL-only: a shader module created from SPIR-V fails validation.",
        },
    };
}
