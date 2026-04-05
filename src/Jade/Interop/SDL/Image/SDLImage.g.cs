using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Jade.Interop.SDL.Image;

internal static unsafe partial class SDLImage
{
    private const string LibraryName = "SDL3_image";

    [LibraryImport(LibraryName, EntryPoint = "IMG_AddAnimationEncoderFrame")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_AddAnimationEncoderFrame(IMG_AnimationEncoder* encoder, SDL.SDL_Surface* surface, ulong duration);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CloseAnimationDecoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_CloseAnimationDecoder(IMG_AnimationDecoder* decoder);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CloseAnimationEncoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_CloseAnimationEncoder(IMG_AnimationEncoder* encoder);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimatedCursor")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Cursor* IMG_CreateAnimatedCursor(IMG_Animation* anim, int hot_x, int hot_y);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimationDecoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationDecoder* IMG_CreateAnimationDecoder(CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimationDecoder_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationDecoder* IMG_CreateAnimationDecoder_IO(SDL.SDL_IOStream* src, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimationDecoderWithProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationDecoder* IMG_CreateAnimationDecoderWithProperties(SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimationEncoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationEncoder* IMG_CreateAnimationEncoder(CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimationEncoder_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationEncoder* IMG_CreateAnimationEncoder_IO(SDL.SDL_IOStream* dst, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_CreateAnimationEncoderWithProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationEncoder* IMG_CreateAnimationEncoderWithProperties(SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "IMG_FreeAnimation")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void IMG_FreeAnimation(IMG_Animation* anim);

    [LibraryImport(LibraryName, EntryPoint = "IMG_GetAnimationDecoderFrame")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_GetAnimationDecoderFrame(IMG_AnimationDecoder* decoder, SDL.SDL_Surface** frame, ulong* duration);

    [LibraryImport(LibraryName, EntryPoint = "IMG_GetAnimationDecoderProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_PropertiesID IMG_GetAnimationDecoderProperties(IMG_AnimationDecoder* decoder);

    [LibraryImport(LibraryName, EntryPoint = "IMG_GetAnimationDecoderStatus")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_AnimationDecoderStatus IMG_GetAnimationDecoderStatus(IMG_AnimationDecoder* decoder);

    [LibraryImport(LibraryName, EntryPoint = "IMG_GetClipboardImage")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_GetClipboardImage();

    [LibraryImport(LibraryName, EntryPoint = "IMG_isANI")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isANI(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isAVIF")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isAVIF(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isBMP")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isBMP(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isCUR")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isCUR(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isGIF")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isGIF(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isICO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isICO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isJPG")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isJPG(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isJXL")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isJXL(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isLBM")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isLBM(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isPCX")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isPCX(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isPNG")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isPNG(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isPNM")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isPNM(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isQOI")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isQOI(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isSVG")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isSVG(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isTIF")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isTIF(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isWEBP")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isWEBP(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isXCF")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isXCF(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isXPM")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isXPM(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_isXV")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_isXV(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_Load")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_Load(CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_Load_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_Load_IO(SDL.SDL_IOStream* src, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadANIAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadANIAnimation_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadAnimation")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadAnimation(CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadAnimation_IO(SDL.SDL_IOStream* src, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadAnimationTyped_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadAnimationTyped_IO(SDL.SDL_IOStream* src, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadAPNGAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadAPNGAnimation_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadAVIF_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadAVIF_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadAVIFAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadAVIFAnimation_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadBMP_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadBMP_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadCUR_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadCUR_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadGIF_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadGIF_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadGIFAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadGIFAnimation_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadGPUTexture")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUTexture* IMG_LoadGPUTexture(SDL.SDL_GPUDevice* device, SDL.SDL_GPUCopyPass* copy_pass, CString file, int* width, int* height);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadGPUTexture_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUTexture* IMG_LoadGPUTexture_IO(SDL.SDL_GPUDevice* device, SDL.SDL_GPUCopyPass* copy_pass, SDL.SDL_IOStream* src, CBool closeio, int* width, int* height);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadGPUTextureTyped_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_GPUTexture* IMG_LoadGPUTextureTyped_IO(SDL.SDL_GPUDevice* device, SDL.SDL_GPUCopyPass* copy_pass, SDL.SDL_IOStream* src, CBool closeio, CString type, int* width, int* height);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadICO_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadICO_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadJPG_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadJPG_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadJXL_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadJXL_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadLBM_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadLBM_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadPCX_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadPCX_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadPNG_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadPNG_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadPNM_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadPNM_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadQOI_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadQOI_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadSizedSVG_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadSizedSVG_IO(SDL.SDL_IOStream* src, int width, int height);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadSVG_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadSVG_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadTexture")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Texture* IMG_LoadTexture(SDL.SDL_Renderer* renderer, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadTexture_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Texture* IMG_LoadTexture_IO(SDL.SDL_Renderer* renderer, SDL.SDL_IOStream* src, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadTextureTyped_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Texture* IMG_LoadTextureTyped_IO(SDL.SDL_Renderer* renderer, SDL.SDL_IOStream* src, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadTGA_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadTGA_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadTIF_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadTIF_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadTyped_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadTyped_IO(SDL.SDL_IOStream* src, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadWEBP_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadWEBP_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadWEBPAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial IMG_Animation* IMG_LoadWEBPAnimation_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadXCF_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadXCF_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadXPM_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadXPM_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_LoadXV_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_LoadXV_IO(SDL.SDL_IOStream* src);

    [LibraryImport(LibraryName, EntryPoint = "IMG_ReadXPMFromArray")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_ReadXPMFromArray(CString* xpm);

    [LibraryImport(LibraryName, EntryPoint = "IMG_ReadXPMFromArrayToRGB888")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_Surface* IMG_ReadXPMFromArrayToRGB888(CString* xpm);

    [LibraryImport(LibraryName, EntryPoint = "IMG_ResetAnimationDecoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_ResetAnimationDecoder(IMG_AnimationDecoder* decoder);

    [LibraryImport(LibraryName, EntryPoint = "IMG_Save")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_Save(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveANIAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveANIAnimation_IO(IMG_Animation* anim, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveAnimation")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveAnimation(IMG_Animation* anim, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveAnimationTyped_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveAnimationTyped_IO(IMG_Animation* anim, SDL.SDL_IOStream* dst, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveAPNGAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveAPNGAnimation_IO(IMG_Animation* anim, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveAVIF")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveAVIF(SDL.SDL_Surface* surface, CString file, int quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveAVIF_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveAVIF_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio, int quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveAVIFAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveAVIFAnimation_IO(IMG_Animation* anim, SDL.SDL_IOStream* dst, CBool closeio, int quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveBMP")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveBMP(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveBMP_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveBMP_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveCUR")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveCUR(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveCUR_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveCUR_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveGIF")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveGIF(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveGIF_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveGIF_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveGIFAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveGIFAnimation_IO(IMG_Animation* anim, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveICO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveICO(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveICO_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveICO_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveJPG")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveJPG(SDL.SDL_Surface* surface, CString file, int quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveJPG_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveJPG_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio, int quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SavePNG")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SavePNG(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SavePNG_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SavePNG_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveTGA")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveTGA(SDL.SDL_Surface* surface, CString file);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveTGA_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveTGA_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveTyped_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveTyped_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio, CString type);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveWEBP")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveWEBP(SDL.SDL_Surface* surface, CString file, float quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveWEBP_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveWEBP_IO(SDL.SDL_Surface* surface, SDL.SDL_IOStream* dst, CBool closeio, float quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_SaveWEBPAnimation_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool IMG_SaveWEBPAnimation_IO(IMG_Animation* anim, SDL.SDL_IOStream* dst, CBool closeio, int quality);

    [LibraryImport(LibraryName, EntryPoint = "IMG_Version")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int IMG_Version();

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_AVIF_ALLOW_INCREMENTAL_BOOLEAN = (CString)"SDL_image.animation_decoder.create.avif.allow_incremental"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_AVIF_ALLOW_PROGRESSIVE_BOOLEAN = (CString)"SDL_image.animation_decoder.create.avif.allow_progressive"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_AVIF_MAX_THREADS_NUMBER = (CString)"SDL_image.animation_decoder.create.avif.max_threads"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_FILENAME_STRING = (CString)"SDL_image.animation_decoder.create.filename"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_GIF_NUM_COLORS_NUMBER = (CString)"SDL_image.animation_encoder.create.gif.num_colors"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_GIF_TRANSPARENT_COLOR_INDEX_NUMBER = (CString)"SDL_image.animation_encoder.create.gif.transparent_color_index"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_IOSTREAM_AUTOCLOSE_BOOLEAN = (CString)"SDL_image.animation_decoder.create.iostream.autoclose"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_IOSTREAM_POINTER = (CString)"SDL_image.animation_decoder.create.iostream"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_TIMEBASE_DENOMINATOR_NUMBER = (CString)"SDL_image.animation_decoder.create.timebase.denominator"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_TIMEBASE_NUMERATOR_NUMBER = (CString)"SDL_image.animation_decoder.create.timebase.numerator"u8;

    internal static readonly CString IMG_PROP_ANIMATION_DECODER_CREATE_TYPE_STRING = (CString)"SDL_image.animation_decoder.create.type"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_AVIF_KEYFRAME_INTERVAL_NUMBER = (CString)"SDL_image.animation_encoder.create.avif.keyframe_interval"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_AVIF_MAX_THREADS_NUMBER = (CString)"SDL_image.animation_encoder.create.avif.max_threads"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_FILENAME_STRING = (CString)"SDL_image.animation_encoder.create.filename"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_GIF_USE_LUT_BOOLEAN = (CString)"SDL_image.animation_encoder.create.gif.use_lut"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_IOSTREAM_AUTOCLOSE_BOOLEAN = (CString)"SDL_image.animation_encoder.create.iostream.autoclose"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_IOSTREAM_POINTER = (CString)"SDL_image.animation_encoder.create.iostream"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_QUALITY_NUMBER = (CString)"SDL_image.animation_encoder.create.quality"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_TIMEBASE_DENOMINATOR_NUMBER = (CString)"SDL_image.animation_encoder.create.timebase.denominator"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_TIMEBASE_NUMERATOR_NUMBER = (CString)"SDL_image.animation_encoder.create.timebase.numerator"u8;

    internal static readonly CString IMG_PROP_ANIMATION_ENCODER_CREATE_TYPE_STRING = (CString)"SDL_image.animation_encoder.create.type"u8;

    internal static readonly CString IMG_PROP_METADATA_AUTHOR_STRING = (CString)"SDL_image.metadata.author"u8;

    internal static readonly CString IMG_PROP_METADATA_COPYRIGHT_STRING = (CString)"SDL_image.metadata.copyright"u8;

    internal static readonly CString IMG_PROP_METADATA_CREATION_TIME_STRING = (CString)"SDL_image.metadata.creation_time"u8;

    internal static readonly CString IMG_PROP_METADATA_DESCRIPTION_STRING = (CString)"SDL_image.metadata.description"u8;

    internal static readonly CString IMG_PROP_METADATA_FRAME_COUNT_NUMBER = (CString)"SDL_image.metadata.frame_count"u8;

    internal static readonly CString IMG_PROP_METADATA_IGNORE_PROPS_BOOLEAN = (CString)"SDL_image.metadata.ignore_props"u8;

    internal static readonly CString IMG_PROP_METADATA_LOOP_COUNT_NUMBER = (CString)"SDL_image.metadata.loop_count"u8;

    internal static readonly CString IMG_PROP_METADATA_TITLE_STRING = (CString)"SDL_image.metadata.title"u8;

    internal static readonly int SDL_IMAGE_MAJOR_VERSION = (int)3;

    internal static readonly int SDL_IMAGE_MICRO_VERSION = (int)0;

    internal static readonly int SDL_IMAGE_MINOR_VERSION = (int)5;

    internal static readonly int SDL_IMAGE_VERSION = (int)3005000;

    [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 8)]
    internal partial struct IMG_Animation
    {
        [FieldOffset(0)]
        internal int w; // size = 4

        [FieldOffset(4)]
        internal int h; // size = 4

        [FieldOffset(8)]
        internal int count; // size = 4

        [FieldOffset(16)]
        internal SDL.SDL_Surface** frames; // size = 8

        [FieldOffset(24)]
        internal int* delays; // size = 8
    }

    internal enum IMG_AnimationDecoderStatus : int
    {
        IMG_DECODER_STATUS_INVALID = -1,
        IMG_DECODER_STATUS_OK = 0,
        IMG_DECODER_STATUS_FAILED = 1,
        IMG_DECODER_STATUS_COMPLETE = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct IMG_AnimationDecoder
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct IMG_AnimationEncoder
    {
    }
}
