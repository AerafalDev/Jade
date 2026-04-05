using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Jade.Interop.SDL.Mixer;

internal static unsafe partial class SDLMixer
{
    private const string LibraryName = "SDL3_mixer";

    [LibraryImport(LibraryName, EntryPoint = "MIX_AudioFramesToMS")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_AudioFramesToMS(MIX_Audio* audio, long frames);

    [LibraryImport(LibraryName, EntryPoint = "MIX_AudioMSToFrames")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_AudioMSToFrames(MIX_Audio* audio, long ms);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateAudioDecoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_AudioDecoder* MIX_CreateAudioDecoder(CString path, SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateAudioDecoder_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_AudioDecoder* MIX_CreateAudioDecoder_IO(SDL.SDL_IOStream* io, CBool closeio, SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateGroup")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Group* MIX_CreateGroup(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateMixer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Mixer* MIX_CreateMixer(SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateMixerDevice")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Mixer* MIX_CreateMixerDevice(SDL.SDL_AudioDeviceID devid, SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateSineWaveAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_CreateSineWaveAudio(MIX_Mixer* mixer, int hz, float amplitude, long ms);

    [LibraryImport(LibraryName, EntryPoint = "MIX_CreateTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Track* MIX_CreateTrack(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_DecodeAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int MIX_DecodeAudio(MIX_AudioDecoder* audiodecoder, void* buffer, int buflen, SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_DestroyAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_DestroyAudio(MIX_Audio* audio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_DestroyAudioDecoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_DestroyAudioDecoder(MIX_AudioDecoder* audiodecoder);

    [LibraryImport(LibraryName, EntryPoint = "MIX_DestroyGroup")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_DestroyGroup(MIX_Group* group);

    [LibraryImport(LibraryName, EntryPoint = "MIX_DestroyMixer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_DestroyMixer(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_DestroyTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_DestroyTrack(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_FramesToMS")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_FramesToMS(int sample_rate, long frames);

    [LibraryImport(LibraryName, EntryPoint = "MIX_Generate")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int MIX_Generate(MIX_Mixer* mixer, void* buffer, int buflen);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetAudioDecoder")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CString MIX_GetAudioDecoder(int index);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetAudioDecoderFormat")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_GetAudioDecoderFormat(MIX_AudioDecoder* audiodecoder, SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetAudioDecoderProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_PropertiesID MIX_GetAudioDecoderProperties(MIX_AudioDecoder* audiodecoder);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetAudioDuration")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_GetAudioDuration(MIX_Audio* audio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetAudioFormat")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_GetAudioFormat(MIX_Audio* audio, SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetAudioProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_PropertiesID MIX_GetAudioProperties(MIX_Audio* audio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetGroupMixer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Mixer* MIX_GetGroupMixer(MIX_Group* group);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetGroupProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_PropertiesID MIX_GetGroupProperties(MIX_Group* group);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetMixerFormat")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_GetMixerFormat(MIX_Mixer* mixer, SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetMixerFrequencyRatio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial float MIX_GetMixerFrequencyRatio(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetMixerGain")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial float MIX_GetMixerGain(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetMixerProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_PropertiesID MIX_GetMixerProperties(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetNumAudioDecoders")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int MIX_GetNumAudioDecoders();

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTaggedTracks")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Track** MIX_GetTaggedTracks(MIX_Mixer* mixer, CString tag, int* count);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrack3DPosition")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_GetTrack3DPosition(MIX_Track* track, MIX_Point3D* position);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_GetTrackAudio(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackAudioStream")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_AudioStream* MIX_GetTrackAudioStream(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackFadeFrames")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_GetTrackFadeFrames(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackFrequencyRatio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial float MIX_GetTrackFrequencyRatio(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackGain")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial float MIX_GetTrackGain(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackLoops")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int MIX_GetTrackLoops(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackMixer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Mixer* MIX_GetTrackMixer(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackPlaybackPosition")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_GetTrackPlaybackPosition(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial SDL.SDL_PropertiesID MIX_GetTrackProperties(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackRemaining")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_GetTrackRemaining(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_GetTrackTags")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CString* MIX_GetTrackTags(MIX_Track* track, int* count);

    [LibraryImport(LibraryName, EntryPoint = "MIX_Init")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_Init();

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadAudio(MIX_Mixer* mixer, CString path, CBool predecode);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadAudio_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadAudio_IO(MIX_Mixer* mixer, SDL.SDL_IOStream* io, CBool predecode, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadAudioNoCopy")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadAudioNoCopy(MIX_Mixer* mixer, void* data, ulong datalen, CBool free_when_done);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadAudioWithProperties")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadAudioWithProperties(SDL.SDL_PropertiesID props);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadRawAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadRawAudio(MIX_Mixer* mixer, void* data, ulong datalen, SDL.SDL_AudioSpec* spec);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadRawAudio_IO")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadRawAudio_IO(MIX_Mixer* mixer, SDL.SDL_IOStream* io, SDL.SDL_AudioSpec* spec, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LoadRawAudioNoCopy")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial MIX_Audio* MIX_LoadRawAudioNoCopy(MIX_Mixer* mixer, void* data, ulong datalen, SDL.SDL_AudioSpec* spec, CBool free_when_done);

    [LibraryImport(LibraryName, EntryPoint = "MIX_LockMixer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_LockMixer(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_MSToFrames")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_MSToFrames(int sample_rate, long ms);

    [LibraryImport(LibraryName, EntryPoint = "MIX_PauseAllTracks")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_PauseAllTracks(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_PauseTag")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_PauseTag(MIX_Mixer* mixer, CString tag);

    [LibraryImport(LibraryName, EntryPoint = "MIX_PauseTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_PauseTrack(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_PlayAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_PlayAudio(MIX_Mixer* mixer, MIX_Audio* audio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_PlayTag")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_PlayTag(MIX_Mixer* mixer, CString tag, SDL.SDL_PropertiesID options);

    [LibraryImport(LibraryName, EntryPoint = "MIX_PlayTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_PlayTrack(MIX_Track* track, SDL.SDL_PropertiesID options);

    [LibraryImport(LibraryName, EntryPoint = "MIX_Quit")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_Quit();

    [LibraryImport(LibraryName, EntryPoint = "MIX_ResumeAllTracks")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_ResumeAllTracks(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_ResumeTag")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_ResumeTag(MIX_Mixer* mixer, CString tag);

    [LibraryImport(LibraryName, EntryPoint = "MIX_ResumeTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_ResumeTrack(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetGroupPostMixCallback")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetGroupPostMixCallback(MIX_Group* group, MIX_GroupMixCallback cb, void* userdata);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetMixerFrequencyRatio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetMixerFrequencyRatio(MIX_Mixer* mixer, float ratio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetMixerGain")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetMixerGain(MIX_Mixer* mixer, float gain);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetPostMixCallback")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetPostMixCallback(MIX_Mixer* mixer, MIX_PostMixCallback cb, void* userdata);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTagGain")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTagGain(MIX_Mixer* mixer, CString tag, float gain);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrack3DPosition")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrack3DPosition(MIX_Track* track, MIX_Point3D* position);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackAudio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackAudio(MIX_Track* track, MIX_Audio* audio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackAudioStream")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackAudioStream(MIX_Track* track, SDL.SDL_AudioStream* stream);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackCookedCallback")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackCookedCallback(MIX_Track* track, MIX_TrackMixCallback cb, void* userdata);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackFrequencyRatio")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackFrequencyRatio(MIX_Track* track, float ratio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackGain")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackGain(MIX_Track* track, float gain);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackGroup")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackGroup(MIX_Track* track, MIX_Group* group);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackIOStream")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackIOStream(MIX_Track* track, SDL.SDL_IOStream* io, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackLoops")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackLoops(MIX_Track* track, int num_loops);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackOutputChannelMap")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackOutputChannelMap(MIX_Track* track, int* chmap, int count);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackPlaybackPosition")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackPlaybackPosition(MIX_Track* track, long frames);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackRawCallback")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackRawCallback(MIX_Track* track, MIX_TrackMixCallback cb, void* userdata);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackRawIOStream")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackRawIOStream(MIX_Track* track, SDL.SDL_IOStream* io, SDL.SDL_AudioSpec* spec, CBool closeio);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackStereo")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackStereo(MIX_Track* track, MIX_StereoGains* gains);

    [LibraryImport(LibraryName, EntryPoint = "MIX_SetTrackStoppedCallback")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_SetTrackStoppedCallback(MIX_Track* track, MIX_TrackStoppedCallback cb, void* userdata);

    [LibraryImport(LibraryName, EntryPoint = "MIX_StopAllTracks")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_StopAllTracks(MIX_Mixer* mixer, long fade_out_ms);

    [LibraryImport(LibraryName, EntryPoint = "MIX_StopTag")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_StopTag(MIX_Mixer* mixer, CString tag, long fade_out_ms);

    [LibraryImport(LibraryName, EntryPoint = "MIX_StopTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_StopTrack(MIX_Track* track, long fade_out_frames);

    [LibraryImport(LibraryName, EntryPoint = "MIX_TagTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_TagTrack(MIX_Track* track, CString tag);

    [LibraryImport(LibraryName, EntryPoint = "MIX_TrackFramesToMS")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_TrackFramesToMS(MIX_Track* track, long frames);

    [LibraryImport(LibraryName, EntryPoint = "MIX_TrackMSToFrames")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial long MIX_TrackMSToFrames(MIX_Track* track, long ms);

    [LibraryImport(LibraryName, EntryPoint = "MIX_TrackPaused")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_TrackPaused(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_TrackPlaying")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial CBool MIX_TrackPlaying(MIX_Track* track);

    [LibraryImport(LibraryName, EntryPoint = "MIX_UnlockMixer")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_UnlockMixer(MIX_Mixer* mixer);

    [LibraryImport(LibraryName, EntryPoint = "MIX_UntagTrack")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial void MIX_UntagTrack(MIX_Track* track, CString tag);

    [LibraryImport(LibraryName, EntryPoint = "MIX_Version")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static partial int MIX_Version();

    internal static readonly int MIX_DURATION_INFINITE = (int)-2;

    internal static readonly int MIX_DURATION_UNKNOWN = (int)-1;

    internal static readonly CString MIX_PROP_AUDIO_DECODER_STRING = (CString)"SDL_mixer.audio.decoder"u8;

    internal static readonly CString MIX_PROP_AUDIO_LOAD_CLOSEIO_BOOLEAN = (CString)"SDL_mixer.audio.load.closeio"u8;

    internal static readonly CString MIX_PROP_AUDIO_LOAD_IOSTREAM_POINTER = (CString)"SDL_mixer.audio.load.iostream"u8;

    internal static readonly CString MIX_PROP_AUDIO_LOAD_PREDECODE_BOOLEAN = (CString)"SDL_mixer.audio.load.predecode"u8;

    internal static readonly CString MIX_PROP_AUDIO_LOAD_PREFERRED_MIXER_POINTER = (CString)"SDL_mixer.audio.load.preferred_mixer"u8;

    internal static readonly CString MIX_PROP_AUDIO_LOAD_SKIP_METADATA_TAGS_BOOLEAN = (CString)"SDL_mixer.audio.load.skip_metadata_tags"u8;

    internal static readonly CString MIX_PROP_METADATA_ALBUM_STRING = (CString)"SDL_mixer.metadata.album"u8;

    internal static readonly CString MIX_PROP_METADATA_ARTIST_STRING = (CString)"SDL_mixer.metadata.artist"u8;

    internal static readonly CString MIX_PROP_METADATA_COPYRIGHT_STRING = (CString)"SDL_mixer.metadata.copyright"u8;

    internal static readonly CString MIX_PROP_METADATA_DURATION_FRAMES_NUMBER = (CString)"SDL_mixer.metadata.duration_frames"u8;

    internal static readonly CString MIX_PROP_METADATA_DURATION_INFINITE_BOOLEAN = (CString)"SDL_mixer.metadata.duration_infinite"u8;

    internal static readonly CString MIX_PROP_METADATA_TITLE_STRING = (CString)"SDL_mixer.metadata.title"u8;

    internal static readonly CString MIX_PROP_METADATA_TOTAL_TRACKS_NUMBER = (CString)"SDL_mixer.metadata.total_tracks"u8;

    internal static readonly CString MIX_PROP_METADATA_TRACK_NUMBER = (CString)"SDL_mixer.metadata.track"u8;

    internal static readonly CString MIX_PROP_METADATA_YEAR_NUMBER = (CString)"SDL_mixer.metadata.year"u8;

    internal static readonly CString MIX_PROP_MIXER_DEVICE_NUMBER = (CString)"SDL_mixer.mixer.device"u8;

    internal static readonly CString MIX_PROP_PLAY_APPEND_SILENCE_FRAMES_NUMBER = (CString)"SDL_mixer.play.append_silence_frames"u8;

    internal static readonly CString MIX_PROP_PLAY_APPEND_SILENCE_MILLISECONDS_NUMBER = (CString)"SDL_mixer.play.append_silence_milliseconds"u8;

    internal static readonly CString MIX_PROP_PLAY_FADE_IN_FRAMES_NUMBER = (CString)"SDL_mixer.play.fade_in_frames"u8;

    internal static readonly CString MIX_PROP_PLAY_FADE_IN_MILLISECONDS_NUMBER = (CString)"SDL_mixer.play.fade_in_milliseconds"u8;

    internal static readonly CString MIX_PROP_PLAY_FADE_IN_START_GAIN_FLOAT = (CString)"SDL_mixer.play.fade_in_start_gain"u8;

    internal static readonly CString MIX_PROP_PLAY_HALT_WHEN_EXHAUSTED_BOOLEAN = (CString)"SDL_mixer.play.halt_when_exhausted"u8;

    internal static readonly CString MIX_PROP_PLAY_LOOP_START_FRAME_NUMBER = (CString)"SDL_mixer.play.loop_start_frame"u8;

    internal static readonly CString MIX_PROP_PLAY_LOOP_START_MILLISECOND_NUMBER = (CString)"SDL_mixer.play.loop_start_millisecond"u8;

    internal static readonly CString MIX_PROP_PLAY_LOOPS_NUMBER = (CString)"SDL_mixer.play.loops"u8;

    internal static readonly CString MIX_PROP_PLAY_MAX_FRAME_NUMBER = (CString)"SDL_mixer.play.max_frame"u8;

    internal static readonly CString MIX_PROP_PLAY_MAX_MILLISECONDS_NUMBER = (CString)"SDL_mixer.play.max_milliseconds"u8;

    internal static readonly CString MIX_PROP_PLAY_START_FRAME_NUMBER = (CString)"SDL_mixer.play.start_frame"u8;

    internal static readonly CString MIX_PROP_PLAY_START_MILLISECOND_NUMBER = (CString)"SDL_mixer.play.start_millisecond"u8;

    internal static readonly CString MIX_PROP_PLAY_START_ORDER_NUMBER = (CString)"SDL_mixer.play.start_order"u8;

    internal static readonly int SDL_MIXER_MAJOR_VERSION = (int)3;

    internal static readonly int SDL_MIXER_MICRO_VERSION = (int)0;

    internal static readonly int SDL_MIXER_MINOR_VERSION = (int)3;

    internal static readonly int SDL_MIXER_VERSION = (int)3003000;

    [StructLayout(LayoutKind.Explicit, Size = 12, Pack = 4)]
    internal partial struct MIX_Point3D
    {
        [FieldOffset(0)]
        internal float x; // size = 4

        [FieldOffset(4)]
        internal float y; // size = 4

        [FieldOffset(8)]
        internal float z; // size = 4
    }

    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 4)]
    internal partial struct MIX_StereoGains
    {
        [FieldOffset(0)]
        internal float left; // size = 4

        [FieldOffset(4)]
        internal float right; // size = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_Audio
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_AudioDecoder
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_Group
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_Mixer
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_Track
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_GroupMixCallback
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Group*, SDL.SDL_AudioSpec*, float*, int, void> Pointer;

        internal MIX_GroupMixCallback(delegate* unmanaged[Cdecl]<void*, MIX_Group*, SDL.SDL_AudioSpec*, float*, int, void> pointer)
        {
            Pointer = pointer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_PostMixCallback
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Mixer*, SDL.SDL_AudioSpec*, float*, int, void> Pointer;

        internal MIX_PostMixCallback(delegate* unmanaged[Cdecl]<void*, MIX_Mixer*, SDL.SDL_AudioSpec*, float*, int, void> pointer)
        {
            Pointer = pointer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_TrackMixCallback
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Track*, SDL.SDL_AudioSpec*, float*, int, void> Pointer;

        internal MIX_TrackMixCallback(delegate* unmanaged[Cdecl]<void*, MIX_Track*, SDL.SDL_AudioSpec*, float*, int, void> pointer)
        {
            Pointer = pointer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct MIX_TrackStoppedCallback
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Track*, void> Pointer;

        internal MIX_TrackStoppedCallback(delegate* unmanaged[Cdecl]<void*, MIX_Track*, void> pointer)
        {
            Pointer = pointer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FnPtr_VoidPtr_MIXMixerPtr_SDLAudioSpecPtr_FloatPtr_Int_Void
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Mixer*, SDL.SDL_AudioSpec*, float*, int, void> Pointer;

        internal FnPtr_VoidPtr_MIXMixerPtr_SDLAudioSpecPtr_FloatPtr_Int_Void(delegate* unmanaged[Cdecl]<void*, MIX_Mixer*, SDL.SDL_AudioSpec*, float*, int, void> pointer)
        {
            Pointer = pointer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FnPtr_VoidPtr_MIXTrackPtr_SDLAudioSpecPtr_FloatPtr_Int_Void
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Track*, SDL.SDL_AudioSpec*, float*, int, void> Pointer;

        internal FnPtr_VoidPtr_MIXTrackPtr_SDLAudioSpecPtr_FloatPtr_Int_Void(delegate* unmanaged[Cdecl]<void*, MIX_Track*, SDL.SDL_AudioSpec*, float*, int, void> pointer)
        {
            Pointer = pointer;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal partial struct FnPtr_VoidPtr_MIXTrackPtr_Void
    {
        internal delegate* unmanaged[Cdecl]<void*, MIX_Track*, void> Pointer;

        internal FnPtr_VoidPtr_MIXTrackPtr_Void(delegate* unmanaged[Cdecl]<void*, MIX_Track*, void> pointer)
        {
            Pointer = pointer;
        }
    }
}
