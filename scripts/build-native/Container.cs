using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

/// <summary>Runs this script inside the Linux build container of native/linux/Dockerfile (ADR-0013).</summary>
internal static class Container
{
    private const string DockerHint = "Install Docker and make sure the daemon is running.";

    /// <summary>Builds the image if needed, then runs build-native.cs inside it with <paramref name="arguments"/>.</summary>
    /// <param name="repositoryRoot">The repository, mounted at /jade.</param>
    /// <param name="rid">The Linux RID to build; its architecture selects the image platform.</param>
    /// <param name="arguments">The script's arguments inside the container, without <c>--container</c>.</param>
    /// <exception cref="InvalidOperationException">docker cannot start or fails, or global.json has no SDK version.</exception>
    public static void Run(string repositoryRoot, string rid, IReadOnlyList<string> arguments)
    {
        var platform = rid.EndsWith("-arm64", StringComparison.Ordinal) ? "linux/arm64" : "linux/amd64";
        var contextDirectory = Path.Combine(repositoryRoot, "native", "linux");
        var sdkVersion = SdkVersion(repositoryRoot);

        // The tag names the content, so a Dockerfile edit or an SDK bump builds a new image, and an
        // unchanged one is not rebuilt.
        var content = File.ReadAllText(Path.Combine(contextDirectory, "Dockerfile")) + "\n" + sdkVersion + "\n" + platform;
        var tag = "jade-native-build:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..16];
        if (!Command.Succeeds("docker", repositoryRoot, ["image", "inspect", tag], DockerHint))
        {
            Command.Run("docker", repositoryRoot, ["build", "--platform", platform, "--build-arg", $"DOTNET_SDK_VERSION={sdkVersion}", "--tag", tag, contextDirectory], DockerHint);
        }

        // The container gets its own build tree, xmake configuration and home (packages, NuGet), so that
        // its packages, built against old system headers, never mix with those of a host build. They are
        // mounted where a host build keeps them, which leaves the script unaware of the container.
        var stateDirectory = Path.Combine(repositoryRoot, "native", "build", "container", rid);
        var buildDirectory = Directory.CreateDirectory(Path.Combine(stateDirectory, "build")).FullName;
        var configDirectory = Directory.CreateDirectory(Path.Combine(stateDirectory, "config")).FullName;
        var homeDirectory = Directory.CreateDirectory(Path.Combine(stateDirectory, "home")).FullName;

        // Mount points that do not exist yet would be created by the Docker daemon, owned by root.
        Directory.CreateDirectory(Path.Combine(repositoryRoot, "native", ".xmake"));

        List<string> run =
        [
            "run", "--rm", "--platform", platform,
            "--volume", $"{repositoryRoot}:/jade",
            "--volume", $"{buildDirectory}:/jade/native/build",
            "--volume", $"{configDirectory}:/jade/native/.xmake",
            "--volume", $"{homeDirectory}:/home/jade",
            "--env", "HOME=/home/jade",
            "--workdir", "/jade",
        ];

        // xmake refuses to run as root, and files written to the mounts must belong to the caller. A
        // rootless daemon maps container root to the caller instead, so this assumes a rootful one.
        if (!OperatingSystem.IsWindows())
        {
            run.AddRange(["--user", $"{Posix.GetUserId()}:{Posix.GetGroupId()}"]);
        }

        run.AddRange([tag, "dotnet", "scripts/build-native.cs", .. arguments]);
        Command.Run("docker", repositoryRoot, run, DockerHint);
    }

    private static string SdkVersion(string repositoryRoot)
    {
        using var globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, "global.json")));
        return globalJson.RootElement.TryGetProperty("sdk", out var sdk) && sdk.TryGetProperty("version", out var version) && version.GetString() is { } value
            ? value
            : throw new InvalidOperationException("global.json has no sdk.version.");
    }
}
