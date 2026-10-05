using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>
/// Reads a value that <c>dawn.json</c> writes either as a number or as a name, such as a length or
/// a default, and keeps its source text.
/// </summary>
internal sealed class DawnLiteralConverter : JsonConverter<string>
{
    /// <inheritdoc/>
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType == JsonTokenType.String
            ? reader.GetString()
            : reader.TokenType != JsonTokenType.Number
                ? throw new JsonException("Expected a number or a string.")
                : reader.TryGetInt64(out var integer)
                    ? integer.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString("R", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">Always: the generator only reads <c>dawn.json</c>.</exception>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
