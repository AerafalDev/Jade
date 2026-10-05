using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>Reads a chain direction, which <c>dawn.json</c> writes as <c>false</c>, <c>"in"</c> or <c>"out"</c>.</summary>
internal sealed class DawnDirectionConverter : JsonConverter<DawnDirection>
{
    /// <inheritdoc/>
    public override DawnDirection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.False
            ? DawnDirection.None
            : reader.TokenType == JsonTokenType.String && reader.ValueTextEquals("in"u8)
                ? DawnDirection.In
                : reader.TokenType == JsonTokenType.String && reader.ValueTextEquals("out"u8)
                    ? DawnDirection.Out
                    : throw new JsonException("Expected false, \"in\" or \"out\".");
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always: the generator only reads <c>dawn.json</c>.</exception>
    public override void Write(Utf8JsonWriter writer, DawnDirection value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
