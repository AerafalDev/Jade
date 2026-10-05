namespace Jade.NativeBuild.Sources;

/// <summary>A dependency pinned by a gclient <c>DEPS</c> file.</summary>
/// <param name="Path">The path the dependency is checked out to, relative to the repository that pins it.</param>
/// <param name="Repository">The URL of the dependency's git repository.</param>
/// <param name="Commit">The full hash of the pinned commit.</param>
internal sealed record DepsEntry(string Path, string Repository, string Commit);
