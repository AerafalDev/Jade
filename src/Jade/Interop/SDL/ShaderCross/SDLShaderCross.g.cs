using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Jade.Interop.SDL.ShaderCross;

internal static unsafe partial class SDLShaderCross
{
    private const string LibraryName = "SDL3_shadercross";

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileComputePipelineFromSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUComputePipeline* SDL_ShaderCross_CompileComputePipelineFromSPIRV(SDL.SDL_GPUDevice* device, SDL_ShaderCross_SPIRV_Info* info, SDL_ShaderCross_ComputePipelineMetadata* metadata, SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileDXBCFromHLSL")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_CompileDXBCFromHLSL(SDL_ShaderCross_HLSL_Info* info, ulong* size);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileDXBCFromSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_CompileDXBCFromSPIRV(SDL_ShaderCross_SPIRV_Info* info, ulong* size);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileDXILFromHLSL")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_CompileDXILFromHLSL(SDL_ShaderCross_HLSL_Info* info, ulong* size);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileDXILFromSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_CompileDXILFromSPIRV(SDL_ShaderCross_SPIRV_Info* info, ulong* size);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileGraphicsShaderFromSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUShader* SDL_ShaderCross_CompileGraphicsShaderFromSPIRV(SDL.SDL_GPUDevice* device, SDL_ShaderCross_SPIRV_Info* info, SDL_ShaderCross_GraphicsShaderResourceInfo* resource_info, SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_CompileSPIRVFromHLSL")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_CompileSPIRVFromHLSL(SDL_ShaderCross_HLSL_Info* info, ulong* size);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_GetHLSLShaderFormats")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUShaderFormat SDL_ShaderCross_GetHLSLShaderFormats();

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_GetSPIRVShaderFormats")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUShaderFormat SDL_ShaderCross_GetSPIRVShaderFormats();

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_Init")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool SDL_ShaderCross_Init();

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_Quit")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void SDL_ShaderCross_Quit();

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_ReflectComputeSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL_ShaderCross_ComputePipelineMetadata* SDL_ShaderCross_ReflectComputeSPIRV(byte* bytecode, ulong bytecode_size, SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_ReflectGraphicsSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL_ShaderCross_GraphicsShaderMetadata* SDL_ShaderCross_ReflectGraphicsSPIRV(byte* bytecode, ulong bytecode_size, SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_TranspileHLSLFromSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_TranspileHLSLFromSPIRV(SDL_ShaderCross_SPIRV_Info* info);

    [LibraryImport(LibraryName, EntryPoint = "SDL_ShaderCross_TranspileMSLFromSPIRV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void* SDL_ShaderCross_TranspileMSLFromSPIRV(SDL_ShaderCross_SPIRV_Info* info);

    internal static readonly int SDL_SHADERCROSS_MAJOR_VERSION = (int)3;

    internal static readonly int SDL_SHADERCROSS_MICRO_VERSION = (int)0;

    internal static readonly int SDL_SHADERCROSS_MINOR_VERSION = (int)0;

    internal static readonly CString SDL_SHADERCROSS_PROP_SHADER_CULL_UNUSED_BINDINGS_BOOLEAN = (CString)"SDL_shadercross.spirv.cull_unused_bindings"u8;

    internal static readonly CString SDL_SHADERCROSS_PROP_SHADER_DEBUG_ENABLE_BOOLEAN = (CString)"SDL_shadercross.spirv.debug.enable"u8;

    internal static readonly CString SDL_SHADERCROSS_PROP_SHADER_DEBUG_NAME_STRING = (CString)"SDL_shadercross.spirv.debug.name"u8;

    internal static readonly CString SDL_SHADERCROSS_PROP_SPIRV_MSL_VERSION_STRING = (CString)"SDL_shadercross.spirv.msl.version"u8;

    internal static readonly CString SDL_SHADERCROSS_PROP_SPIRV_PSSL_COMPATIBILITY_BOOLEAN = (CString)"SDL_shadercross.spirv.pssl.compatibility"u8;

    [StructLayout(LayoutKind.Explicit, Size = 36, Pack = 4)]
    internal partial struct SDL_ShaderCross_ComputePipelineMetadata
    {
        [FieldOffset(0)]
        internal uint num_samplers; // size = 4

        [FieldOffset(4)]
        internal uint num_readonly_storage_textures; // size = 4

        [FieldOffset(8)]
        internal uint num_readonly_storage_buffers; // size = 4

        [FieldOffset(12)]
        internal uint num_readwrite_storage_textures; // size = 4

        [FieldOffset(16)]
        internal uint num_readwrite_storage_buffers; // size = 4

        [FieldOffset(20)]
        internal uint num_uniform_buffers; // size = 4

        [FieldOffset(24)]
        internal uint threadcount_x; // size = 4

        [FieldOffset(28)]
        internal uint threadcount_y; // size = 4

        [FieldOffset(32)]
        internal uint threadcount_z; // size = 4
    }

    [StructLayout(LayoutKind.Explicit, Size = 48, Pack = 8)]
    internal partial struct SDL_ShaderCross_GraphicsShaderMetadata
    {
        [FieldOffset(0)]
        internal SDL_ShaderCross_GraphicsShaderResourceInfo resource_info; // size = 16

        [FieldOffset(16)]
        internal uint num_inputs; // size = 4

        [FieldOffset(24)]
        internal SDL_ShaderCross_IOVarMetadata* inputs; // size = 8

        [FieldOffset(32)]
        internal uint num_outputs; // size = 4

        [FieldOffset(40)]
        internal SDL_ShaderCross_IOVarMetadata* outputs; // size = 8
    }

    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 4)]
    internal partial struct SDL_ShaderCross_GraphicsShaderResourceInfo
    {
        [FieldOffset(0)]
        internal uint num_samplers; // size = 4

        [FieldOffset(4)]
        internal uint num_storage_textures; // size = 4

        [FieldOffset(8)]
        internal uint num_storage_buffers; // size = 4

        [FieldOffset(12)]
        internal uint num_uniform_buffers; // size = 4
    }

    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 8)]
    internal partial struct SDL_ShaderCross_HLSL_Define
    {
        [FieldOffset(0)]
        internal CString name; // size = 8

        [FieldOffset(8)]
        internal CString value; // size = 8
    }

    [StructLayout(LayoutKind.Explicit, Size = 40, Pack = 8)]
    internal partial struct SDL_ShaderCross_HLSL_Info
    {
        [FieldOffset(0)]
        internal CString source; // size = 8

        [FieldOffset(8)]
        internal CString entrypoint; // size = 8

        [FieldOffset(16)]
        internal CString include_dir; // size = 8

        [FieldOffset(24)]
        internal SDL_ShaderCross_HLSL_Define* defines; // size = 8

        [FieldOffset(32)]
        internal SDL_ShaderCross_ShaderStage shader_stage; // size = 4

        [FieldOffset(36)]
        internal SDL.SDL_PropertiesID props; // size = 4
    }

    [StructLayout(LayoutKind.Explicit, Size = 24, Pack = 8)]
    internal partial struct SDL_ShaderCross_IOVarMetadata
    {
        [FieldOffset(0)]
        internal CString name; // size = 8

        [FieldOffset(8)]
        internal uint location; // size = 4

        [FieldOffset(12)]
        internal SDL_ShaderCross_IOVarType vector_type; // size = 4

        [FieldOffset(16)]
        internal uint vector_size; // size = 4
    }

    [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 8)]
    internal partial struct SDL_ShaderCross_SPIRV_Info
    {
        [FieldOffset(0)]
        internal byte* bytecode; // size = 8

        [FieldOffset(8)]
        internal ulong bytecode_size; // size = 8

        [FieldOffset(16)]
        internal CString entrypoint; // size = 8

        [FieldOffset(24)]
        internal SDL_ShaderCross_ShaderStage shader_stage; // size = 4

        [FieldOffset(28)]
        internal SDL.SDL_PropertiesID props; // size = 4
    }

    internal enum SDL_ShaderCross_IOVarType : int
    {
        SDL_SHADERCROSS_IOVAR_TYPE_UNKNOWN = 0,
        SDL_SHADERCROSS_IOVAR_TYPE_INT8 = 1,
        SDL_SHADERCROSS_IOVAR_TYPE_UINT8 = 2,
        SDL_SHADERCROSS_IOVAR_TYPE_INT16 = 3,
        SDL_SHADERCROSS_IOVAR_TYPE_UINT16 = 4,
        SDL_SHADERCROSS_IOVAR_TYPE_INT32 = 5,
        SDL_SHADERCROSS_IOVAR_TYPE_UINT32 = 6,
        SDL_SHADERCROSS_IOVAR_TYPE_INT64 = 7,
        SDL_SHADERCROSS_IOVAR_TYPE_UINT64 = 8,
        SDL_SHADERCROSS_IOVAR_TYPE_FLOAT16 = 9,
        SDL_SHADERCROSS_IOVAR_TYPE_FLOAT32 = 10,
        SDL_SHADERCROSS_IOVAR_TYPE_FLOAT64 = 11
    }

    internal enum SDL_ShaderCross_ShaderStage : int
    {
        SDL_SHADERCROSS_SHADERSTAGE_VERTEX = 0,
        SDL_SHADERCROSS_SHADERSTAGE_FRAGMENT = 1,
        SDL_SHADERCROSS_SHADERSTAGE_COMPUTE = 2
    }
}
