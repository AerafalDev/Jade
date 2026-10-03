/// <summary>A .NET RID and the clang target triple its headers are parsed for.</summary>
internal sealed class Target
{
    /// <summary>Creates a target.</summary>
    /// <param name="rid">The .NET runtime identifier.</param>
    /// <param name="triple">The clang target triple.</param>
    /// <param name="platform">The <c>OperatingSystem.IsOSPlatform</c> name of the RID's OS.</param>
    public Target(string rid, string triple, string platform)
    {
        Rid = rid;
        Triple = triple;
        Platform = platform;
    }

    /// <summary>Gets the .NET runtime identifier.</summary>
    public string Rid { get; }

    /// <summary>Gets the clang target triple.</summary>
    public string Triple { get; }

    /// <summary>Gets the <c>OperatingSystem.IsOSPlatform</c> name of the RID's OS, as <c>[SupportedOSPlatform]</c> spells it.</summary>
    public string Platform { get; }
}
