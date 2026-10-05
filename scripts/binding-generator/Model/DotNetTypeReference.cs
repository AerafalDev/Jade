namespace Jade.BindingGenerator.Model;

/// <summary>An existing .NET type that the configuration maps a C structure to, such as <c>System.Numerics.Vector3</c> for <c>ma_vec3f</c>.</summary>
/// <param name="FullName">The full name of the .NET type, a blittable structure with the layout of the C type.</param>
/// <remarks>The layout tests check that the C type and the .NET type agree (ADR 0033).</remarks>
internal sealed record DotNetTypeReference(string FullName) : TypeReference;
