using System.Numerics;
using System.Runtime.InteropServices;

/// <summary>The leading members of <c>ImGuiIO</c> (<c>dcimgui.h</c>), up to <c>LogFilename</c>; only reached through <c>ImGui_GetIO</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ImGuiIO
{
    /// <summary><c>ConfigFlags</c> (<c>ImGuiConfigFlags</c>).</summary>
    public int ConfigFlags;

    /// <summary><c>BackendFlags</c> (<c>ImGuiBackendFlags</c>).</summary>
    public int BackendFlags;

    /// <summary><c>DisplaySize</c> (<c>ImVec2</c>).</summary>
    public Vector2 DisplaySize;

    /// <summary><c>DisplayFramebufferScale</c> (<c>ImVec2</c>).</summary>
    public Vector2 DisplayFramebufferScale;

    /// <summary><c>DeltaTime</c>.</summary>
    public float DeltaTime;

    /// <summary><c>IniSavingRate</c>.</summary>
    public float IniSavingRate;

    /// <summary><c>IniFilename</c>: the settings file, relative to the working directory, or null for none.</summary>
    public byte* IniFilename;

    /// <summary><c>LogFilename</c>.</summary>
    public byte* LogFilename;
}
