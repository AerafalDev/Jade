using System.Globalization;

namespace Jade.BindingGenerator.Dawn;

/// <summary>
/// The C names that Dawn's generator gives to the items of <c>dawn.json</c>, whose canonical names
/// are space-separated words.
/// </summary>
/// <remarks>
/// The rules follow the <c>Name</c> class and the <c>as_c*</c> helpers of
/// <c>generator/dawn_json_generator.py</c> at the pinned commit. A word keeps its own casing
/// except for its first letter, which <c>CamelCase</c> raises: <c>RGBA8 unorm</c> gives
/// <c>RGBA8Unorm</c>.
/// </remarks>
internal static class DawnNames
{
    /// <summary>Splits a canonical name into its words.</summary>
    /// <param name="name">The canonical name, such as <c>texture view dimension</c>.</param>
    /// <returns>The words.</returns>
    public static IReadOnlyList<string> GetWords(string name)
    {
        return name.Split(' ');
    }

    /// <summary>Gets the C name of a type: <c>WGPUTextureViewDimension</c>.</summary>
    /// <param name="prefix">The C prefix, <c>WGPU</c>.</param>
    /// <param name="name">The canonical name of the type.</param>
    /// <returns>The C name.</returns>
    public static string GetTypeName(string prefix, string name)
    {
        return prefix + ToCamelCase(name);
    }

    /// <summary>Gets the C name of an enum or bitmask value: <c>WGPUTextureFormat_RGBA8Unorm</c>.</summary>
    /// <param name="prefix">The C prefix, <c>WGPU</c>.</param>
    /// <param name="typeName">The canonical name of the enum or bitmask.</param>
    /// <param name="valueName">The canonical name of the value.</param>
    /// <returns>The C name.</returns>
    public static string GetEnumValueName(string prefix, string typeName, string valueName)
    {
        return prefix + ToCamelCase(typeName) + "_" + ToCamelCase(valueName);
    }

    /// <summary>Gets the C name of a constant: <c>WGPU_WHOLE_SIZE</c>.</summary>
    /// <param name="prefix">The C prefix, <c>WGPU</c>.</param>
    /// <param name="name">The canonical name of the constant.</param>
    /// <returns>The C name.</returns>
    public static string GetConstantName(string prefix, string name)
    {
        return prefix + "_" + ToUpperSnakeCase(name);
    }

    /// <summary>Gets the C name of the macro that initializes a structure: <c>WGPU_EXTENT_3D_INIT</c>.</summary>
    /// <param name="prefix">The C prefix, <c>WGPU</c>.</param>
    /// <param name="name">The canonical name of the structure.</param>
    /// <returns>The C name.</returns>
    public static string GetInitializerName(string prefix, string name)
    {
        return prefix + "_" + ToUpperSnakeCase(name) + "_INIT";
    }

    /// <summary>Gets the C name of a function: <c>wgpuCreateInstance</c>, or <c>wgpuDeviceCreateBuffer</c> for a method.</summary>
    /// <param name="prefix">The C prefix, <c>WGPU</c>, which functions use in lower case.</param>
    /// <param name="ownerName">The canonical name of the object or structure that owns the method, or <see langword="null"/> for a free function.</param>
    /// <param name="name">The canonical name of the function or method.</param>
    /// <returns>The C name.</returns>
    public static string GetFunctionName(string prefix, string? ownerName, string name)
    {
        return AsciiText.ToLower(prefix) + (ownerName is null ? string.Empty : ToCamelCase(ownerName)) + ToCamelCase(name);
    }

    /// <summary>Gets the C name of a structure member or a parameter: <c>depthOrArrayLayers</c>.</summary>
    /// <param name="name">The canonical name.</param>
    /// <returns>The C name, whose first word keeps its casing.</returns>
    public static string GetVariableName(string name)
    {
        var words = GetWords(name);

        return words[0] + string.Concat(words.Skip(1).Select(RaiseFirstLetter));
    }

    /// <summary>Joins the words of a name with their first letter raised, as <c>Name.CamelCase</c> does.</summary>
    /// <param name="name">The canonical name.</param>
    /// <returns>The joined words.</returns>
    private static string ToCamelCase(string name)
    {
        return string.Concat(GetWords(name).Select(RaiseFirstLetter));
    }

    /// <summary>Joins the words of a name in upper case with underscores, as <c>Name.SNAKE_CASE</c> does.</summary>
    /// <param name="name">The canonical name.</param>
    /// <returns>The joined words.</returns>
    private static string ToUpperSnakeCase(string name)
    {
        return string.Join('_', GetWords(name).Select(static word => word.ToUpperInvariant()));
    }

    /// <summary>Raises the first letter of a word and keeps the rest, as <c>Name.CamelChunk</c> does.</summary>
    /// <param name="word">The word.</param>
    /// <returns>The word with its first character in upper case.</returns>
    private static string RaiseFirstLetter(string word)
    {
        return word.Length == 0 ? word : char.ToUpper(word[0], CultureInfo.InvariantCulture) + word[1..];
    }
}
