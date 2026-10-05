using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Jade.BindingGenerator;

/// <summary>Reads the generator's JSON inputs through source-generated serialization.</summary>
internal static class JsonFile
{
    /// <summary>Deserializes a JSON file.</summary>
    /// <typeparam name="T">The type the file holds.</typeparam>
    /// <param name="path">The path of the file.</param>
    /// <param name="typeInfo">The source-generated metadata of <typeparamref name="T"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="InvalidDataException">The file is empty or does not match <typeparamref name="T"/>.</exception>
    public static T Read<T>(string path, JsonTypeInfo<T> typeInfo)
    {
        try
        {
            using var stream = File.OpenRead(path);

            return JsonSerializer.Deserialize(stream, typeInfo) ?? throw new InvalidDataException($"'{path}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"'{path}': {exception.Message}", exception);
        }
    }
}
