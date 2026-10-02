using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/// <summary>Reads the JSON files exchanged with xmake.</summary>
internal static class Json
{
    /// <summary>Deserializes the file at <paramref name="path"/>.</summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="path">The JSON file.</param>
    /// <param name="typeInfo">Source-generated metadata for <typeparamref name="T"/>, from <see cref="JsonContext"/>.</param>
    /// <returns>The document.</returns>
    /// <exception cref="InvalidOperationException">The file holds <c>null</c>.</exception>
    public static T Read<T>(string path, JsonTypeInfo<T> typeInfo) =>
        JsonSerializer.Deserialize(File.ReadAllText(path), typeInfo) ?? throw new InvalidOperationException($"{path} is empty.");
}
