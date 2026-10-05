using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>
/// Reads a method's return type, which <c>dawn.json</c> writes as a type name or as an object that
/// also says whether the result is optional.
/// </summary>
internal sealed class DawnReturnConverter : JsonConverter<DawnReturn>
{
    /// <inheritdoc/>
    public override DawnReturn? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.String
            ? new DawnReturn { Type = reader.GetString()! }
            : reader.TokenType == JsonTokenType.StartObject
                ? JsonSerializer.Deserialize(ref reader, DawnJsonContext.Default.DawnReturn)
                : throw new JsonException("Expected a type name or an object.");
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always: the generator only reads <c>dawn.json</c>.</exception>
    public override void Write(Utf8JsonWriter writer, DawnReturn value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
