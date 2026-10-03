using System.Runtime.InteropServices;

/// <summary>The leading members of <c>ImDrawData</c> (<c>dcimgui.h</c>), up to <c>CmdLists</c>; only reached through <c>ImGui_GetDrawData</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ImDrawData
{
    /// <summary><c>Valid</c>: set by <c>ImGui_Render</c>.</summary>
    public bool Valid;

    /// <summary><c>FrameCount</c>.</summary>
    public int FrameCount;

    /// <summary><c>TotalIdxCount</c>: the indices of every draw list.</summary>
    public int TotalIdxCount;

    /// <summary><c>TotalVtxCount</c>: the vertices of every draw list.</summary>
    public int TotalVtxCount;

    /// <summary><c>CmdLists.Size</c>: the number of draw lists.</summary>
    public int CmdListsSize;

    /// <summary><c>CmdLists.Capacity</c>.</summary>
    public int CmdListsCapacity;

    /// <summary><c>CmdLists.Data</c>: the <c>ImDrawList*</c> array.</summary>
    public void** CmdListsData;
}
