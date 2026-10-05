namespace Jade.BindingGenerator.Configuration;

/// <summary>An interop project whose bindings the generator produces.</summary>
/// <param name="Project">The project name, such as <c>Jade.Sdl</c>, which is also the namespace of its bindings (ADR 0027).</param>
/// <param name="ConfigurationFile">The path of the project's <c>bindings.json</c>.</param>
internal sealed record GeneratedLibrary(string Project, string ConfigurationFile)
{
    /// <summary>Gets the directory that receives the generated files, <c>Generated/</c> in the project (ADR 0023).</summary>
    public string GeneratedDirectory => Path.Combine(Path.GetDirectoryName(ConfigurationFile)!, "Generated");
}
