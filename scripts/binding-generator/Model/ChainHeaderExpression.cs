namespace Jade.BindingGenerator.Model;

/// <summary>The chain header of an extension: not linked yet, and identifying the extension's structure type.</summary>
/// <param name="TypeMember">The C name of the header member that holds the structure type.</param>
/// <param name="StructureType">The value of the structure type enum that identifies the extension.</param>
internal sealed record ChainHeaderExpression(string TypeMember, EnumValueReferenceExpression StructureType) : ValueExpression;
