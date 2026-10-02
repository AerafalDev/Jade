/// <summary><c>WGPURequestAdapterCallbackInfo</c>, with enums as their 32-bit values.</summary>
internal unsafe struct WGPURequestAdapterCallbackInfo
{
    /// <summary><c>nextInChain</c>: extension structs, null here.</summary>
    public void* NextInChain;

    /// <summary><c>mode</c> (<c>WGPUCallbackMode</c>).</summary>
    public uint Mode;

    /// <summary><c>callback</c>: <c>void (WGPURequestAdapterStatus, WGPUAdapter, WGPUStringView, void*, void*)</c>.</summary>
    public delegate* unmanaged[Cdecl]<uint, nint, WGPUStringView, void*, void*, void> Callback;

    /// <summary><c>userdata1</c>, passed back to <see cref="Callback"/>.</summary>
    public void* Userdata1;

    /// <summary><c>userdata2</c>, passed back to <see cref="Callback"/>.</summary>
    public void* Userdata2;
}
