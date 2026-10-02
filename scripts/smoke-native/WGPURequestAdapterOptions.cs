/// <summary><c>WGPURequestAdapterOptions</c>, with enums as their 32-bit values.</summary>
internal unsafe struct WGPURequestAdapterOptions
{
    /// <summary><c>nextInChain</c>: extension structs, null here.</summary>
    public void* NextInChain;

    /// <summary><c>featureLevel</c> (<c>WGPUFeatureLevel</c>).</summary>
    public uint FeatureLevel;

    /// <summary><c>powerPreference</c> (<c>WGPUPowerPreference</c>).</summary>
    public uint PowerPreference;

    /// <summary><c>forceFallbackAdapter</c> (<c>WGPUBool</c>).</summary>
    public uint ForceFallbackAdapter;

    /// <summary><c>backendType</c> (<c>WGPUBackendType</c>).</summary>
    public uint BackendType;

    /// <summary><c>compatibleSurface</c>, null here.</summary>
    public nint CompatibleSurface;
}
