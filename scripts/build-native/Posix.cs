using System.Runtime.InteropServices;

/// <summary>The POSIX calls .NET has no API for.</summary>
internal static class Posix
{
    /// <summary><c>getuid</c>: the real user ID of the process.</summary>
    /// <returns>The user ID.</returns>
    [DllImport("libc", EntryPoint = "getuid")]
    public static extern uint GetUserId();

    /// <summary><c>getgid</c>: the real group ID of the process.</summary>
    /// <returns>The group ID.</returns>
    [DllImport("libc", EntryPoint = "getgid")]
    public static extern uint GetGroupId();
}
