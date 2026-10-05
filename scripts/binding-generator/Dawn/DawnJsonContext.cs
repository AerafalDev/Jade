using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>Source-generated serialization of <c>dawn.json</c>, whose member names are set explicitly.</summary>
[JsonSerializable(typeof(DawnMetadata))]
[JsonSerializable(typeof(DawnEntry))]
[JsonSerializable(typeof(DawnReturn))]
internal sealed partial class DawnJsonContext : JsonSerializerContext;
