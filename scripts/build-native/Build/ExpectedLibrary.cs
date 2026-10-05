namespace Jade.NativeBuild.Build;

/// <summary>A library the build must produce for a target, and functions it must export.</summary>
/// <param name="FileName">The file name, as the managed side loads it from <c>runtimes/{rid}/native/</c>.</param>
/// <param name="Exports">Functions whose export proves that the library is the expected one.</param>
internal sealed record ExpectedLibrary(string FileName, IReadOnlyList<string> Exports);
