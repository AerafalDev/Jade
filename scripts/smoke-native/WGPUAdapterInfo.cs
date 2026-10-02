/// <summary><c>WGPUAdapterInfo</c>, with enums as their 32-bit values.</summary>
internal unsafe struct WGPUAdapterInfo
{
    /// <summary><c>nextInChain</c>: extension structs, null here.</summary>
    public void* NextInChain;

    /// <summary><c>vendor</c>.</summary>
    public WGPUStringView Vendor;

    /// <summary><c>architecture</c>.</summary>
    public WGPUStringView Architecture;

    /// <summary><c>device</c>.</summary>
    public WGPUStringView Device;

    /// <summary><c>description</c>.</summary>
    public WGPUStringView Description;

    /// <summary><c>backendType</c> (<c>WGPUBackendType</c>).</summary>
    public uint BackendType;

    /// <summary><c>adapterType</c> (<c>WGPUAdapterType</c>).</summary>
    public uint AdapterType;

    /// <summary><c>vendorID</c>.</summary>
    public uint VendorId;

    /// <summary><c>deviceID</c>.</summary>
    public uint DeviceId;

    /// <summary><c>subgroupMinSize</c>.</summary>
    public uint SubgroupMinSize;

    /// <summary><c>subgroupMaxSize</c>.</summary>
    public uint SubgroupMaxSize;
}
