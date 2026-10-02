using System.Text.Json.Serialization;

/// <summary>Source-generated serializers: scripts run the AOT and trim analyzers, which reject reflection-based JSON.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(Upstream))]
[JsonSerializable(typeof(Versions))]
internal sealed partial class JsonContext : JsonSerializerContext;
