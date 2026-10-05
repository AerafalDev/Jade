namespace Jade.BindingGenerator.Configuration;

/// <summary>An interop project whose bindings the generator produces.</summary>
/// <param name="Project">The project name, such as <c>Jade.Sdl</c>.</param>
/// <param name="ConfigurationFile">The path of the project's <c>bindings.json</c>.</param>
internal sealed record GeneratedLibrary(string Project, string ConfigurationFile);
