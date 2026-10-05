using Jade.BindingGenerator.Model;

namespace Jade.BindingGenerator.Projection;

/// <summary>Projects a library's structures onto its layout tests (ADR 0036).</summary>
/// <remarks>
/// Every generated structure is compared, public or raw, unions and anonymous records included, and
/// so is every structure the configuration maps to a .NET type. An inline array has no C type of its
/// own: it is compared as the array member that holds it. Opaque types have no managed layout.
/// </remarks>
internal static class LayoutProjection
{
    /// <summary>The prefix of the interop projects, which the name of a layout library drops.</summary>
    private const string ProjectPrefix = "Jade.";

    /// <summary>Projects the layout tests of a library.</summary>
    /// <param name="model">The model the raw layer was projected from.</param>
    /// <param name="library">The projected raw layer.</param>
    /// <param name="names">The naming rules of the raw layer, which name the members of the mapped .NET types.</param>
    /// <param name="headers">What the C side includes.</param>
    /// <returns>The layout tests.</returns>
    /// <exception cref="InvalidDataException">A record cannot be reached from C code, or two records would be measured by methods of the same name.</exception>
    public static ProjectedLayouts Project(ApiModel model, ProjectedLibrary library, DotNetNames names, LayoutHeaders headers)
    {
        var records = library.Types.OfType<ProjectedStructure>()
            .Select(structure => ProjectStructure(model, library.Namespace, structure))
            .Concat(model.MappedStructures.Select(structure => ProjectMappedStructure(structure, names)))
            .OrderBy(static record => record.CName, StringComparer.Ordinal)
            .ToList();

        RawProjection.CheckUnique("layout method", $"the layout tests of {library.Namespace}", records.Select(static record => record.MethodName), StringComparer.Ordinal);

        return new ProjectedLayouts(library.Namespace, GetName(library.Namespace), headers, records);
    }

    /// <summary>Projects a generated structure.</summary>
    /// <param name="model">The model.</param>
    /// <param name="namespace">The namespace of the library.</param>
    /// <param name="structure">The projected structure.</param>
    /// <returns>The record to compare.</returns>
    private static ProjectedRecordLayout ProjectStructure(ApiModel model, string @namespace, ProjectedStructure structure)
    {
        var declaration = model.Get<StructureDeclaration>(structure.CName);
        var (root, path) = GetCLocation(model, declaration);

        // The projection keeps one field per member, in the same order.
        var members = declaration.Members
            .Zip(structure.Fields, static (member, field) => new ProjectedMemberLayout(member.CName, field.Name, member.Type is ArrayTypeReference))
            .ToList();
        var type = structure.IsPublic
            ? $"global::{@namespace}.{structure.Name}"
            : $"global::{@namespace}.{RawProjection.RawNamespace}.{structure.Name}";

        return new ProjectedRecordLayout(structure.CName, type, $"Measure{structure.Name}", root, path, structure.Availability, members);
    }

    /// <summary>Projects a structure that the configuration maps to a .NET type, whose fields are expected under the .NET names of its members.</summary>
    /// <param name="structure">The structure.</param>
    /// <param name="names">The naming rules.</param>
    /// <returns>The record to compare.</returns>
    private static ProjectedRecordLayout ProjectMappedStructure(StructureDeclaration structure, DotNetNames names)
    {
        var members = structure.Members
            .Select(member => new ProjectedMemberLayout(member.CName, names.GetName($"{structure.CName}.{member.CName}", member.Words), member.Type is ArrayTypeReference))
            .ToList();
        var root = structure.CTypeName ?? throw new InvalidDataException($"'{structure.CName}' is mapped to {structure.DotNetType} but has no C type name.");

        return new ProjectedRecordLayout(structure.CName, $"global::{structure.DotNetType}", $"Measure{names.GetName(structure.CName, structure.Words)}", root, null, structure.Availability, members);
    }

    /// <summary>Gets how C code reaches a record: through its own type name, or through the members that lead to it from a named record.</summary>
    /// <param name="model">The model.</param>
    /// <param name="structure">The record.</param>
    /// <returns>The C type name of the named record, and the member designators from it, or <see langword="null"/> for the named record itself.</returns>
    /// <exception cref="InvalidDataException">The record has neither a C type name nor a position in another record.</exception>
    private static (string Root, string? Path) GetCLocation(ApiModel model, StructureDeclaration structure)
    {
        if (structure.CTypeName is { } typeName)
        {
            return (typeName, null);
        }

        var position = structure.Position ?? throw new InvalidDataException($"'{structure.CName}' has neither a C type name nor a position in another record.");
        var (root, path) = GetCLocation(model, model.Get<StructureDeclaration>(position.ParentCName));

        return (root, path is null ? position.Designator : $"{path}.{position.Designator}");
    }

    /// <summary>Gets the short name of a library, which names its layout library.</summary>
    /// <param name="namespace">The namespace of the library, such as <c>Jade.Sdl</c>.</param>
    /// <returns>The name, such as <c>sdl</c>.</returns>
    /// <exception cref="InvalidDataException">The namespace is not one of a Jade interop project.</exception>
    private static string GetName(string @namespace)
    {
        return @namespace.StartsWith(ProjectPrefix, StringComparison.Ordinal) && @namespace.Length > ProjectPrefix.Length
            ? AsciiText.ToLower(@namespace.AsSpan(ProjectPrefix.Length))
            : throw new InvalidDataException($"'{@namespace}' is not a Jade interop project, whose name starts with '{ProjectPrefix}'.");
    }
}
