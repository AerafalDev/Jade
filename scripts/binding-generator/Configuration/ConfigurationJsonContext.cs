using System.Text.Json.Serialization;

namespace Jade.BindingGenerator.Configuration;

/// <summary>Source-generated serialization of the repository's JSON files, whose members are camel-cased.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PinnedVersions))]
[JsonSerializable(typeof(LibraryConfiguration))]
internal sealed partial class ConfigurationJsonContext : JsonSerializerContext;
