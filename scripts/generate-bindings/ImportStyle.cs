/// <summary>How raw functions are imported. Both forms name <c>jade_native</c> and need blittable signatures.</summary>
internal enum ImportStyle
{
    /// <summary><c>[DllImport] static extern</c>: the runtime binds the call directly.</summary>
    DllImport,

    /// <summary><c>[LibraryImport] static partial</c>: a source generator writes the stub at build time.</summary>
    LibraryImport,
}
