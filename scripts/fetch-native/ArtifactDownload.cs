namespace Jade.NativeFetch;

/// <summary>An artifact of the native workflow to install, and where.</summary>
/// <param name="Name">The artifact's name, such as <c>native-linux-x64</c>.</param>
/// <param name="Directory">The directory whose content the artifact's files replace.</param>
internal sealed record ArtifactDownload(string Name, string Directory);
