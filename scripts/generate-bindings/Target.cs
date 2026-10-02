/// <summary>A .NET RID and the clang target triple its headers are parsed for.</summary>
internal sealed class Target
{
    /// <summary>Creates a target.</summary>
    /// <param name="rid">The .NET runtime identifier.</param>
    /// <param name="triple">The clang target triple.</param>
    public Target(string rid, string triple)
    {
        Rid = rid;
        Triple = triple;
    }

    /// <summary>Gets the .NET runtime identifier.</summary>
    public string Rid { get; }

    /// <summary>Gets the clang target triple.</summary>
    public string Triple { get; }
}
