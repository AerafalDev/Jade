using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Jade.BindingGenerator.Dawn;

/// <summary>The WebGPU API described by <c>dawn.json</c> at the pinned Dawn commit.</summary>
/// <param name="Metadata">The <c>_metadata</c> object of the file.</param>
/// <param name="Entries">The entries, by canonical name in ordinal order.</param>
/// <remarks>
/// The schema is described in <c>docs/dawn/codegen.md</c> of Dawn, but the file is the reference.
/// Every object rejects unknown members: a Dawn update that changes the schema fails the generator
/// instead of producing bindings that silently miss the new information.
/// </remarks>
internal sealed record DawnApi(DawnMetadata Metadata, IReadOnlyDictionary<string, DawnEntry> Entries)
{
    /// <summary>The prefix of the top-level keys that are not API entries (<c>_comment</c>, <c>_doc</c>, <c>_metadata</c>).</summary>
    private const char ReservedKeyPrefix = '_';

    /// <summary>The key of the metadata object.</summary>
    private const string MetadataKey = "_metadata";

    /// <summary>Reads <c>dawn.json</c>.</summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The API it describes.</returns>
    /// <exception cref="InvalidDataException">The file does not match the expected schema.</exception>
    public static DawnApi Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        DawnMetadata? metadata = null;
        var entries = new SortedDictionary<string, DawnEntry>(StringComparer.Ordinal);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name == MetadataKey)
            {
                metadata = Deserialize(property, DawnJsonContext.Default.DawnMetadata);
            }
            else if (!property.Name.StartsWith(ReservedKeyPrefix, StringComparison.Ordinal))
            {
                entries.Add(property.Name, Deserialize(property, DawnJsonContext.Default.DawnEntry));
            }
        }

        return new DawnApi(metadata ?? throw new InvalidDataException($"'{path}' has no '{MetadataKey}'."), entries);
    }

    /// <summary>Deserializes one top-level property, naming it in errors.</summary>
    /// <typeparam name="T">The type of the property's value.</typeparam>
    /// <param name="property">The property.</param>
    /// <param name="typeInfo">The source-generated metadata of <typeparamref name="T"/>.</param>
    /// <returns>The deserialized value.</returns>
    /// <exception cref="InvalidDataException">The value is null or does not match <typeparamref name="T"/>.</exception>
    private static T Deserialize<T>(JsonProperty property, JsonTypeInfo<T> typeInfo)
    {
        try
        {
            return property.Value.Deserialize(typeInfo) ?? throw new InvalidDataException($"dawn.json: '{property.Name}' is null.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"dawn.json: '{property.Name}': {exception.Message}", exception);
        }
    }
}
