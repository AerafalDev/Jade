namespace Jade.BindingGenerator.Model;

/// <summary>Where an anonymous structure or union lies in the record that holds it; C code has no other way to name it.</summary>
/// <param name="ParentCName">The C name of the record that holds it, itself possibly anonymous.</param>
/// <param name="Designator">The member designator from the parent: <c>input</c>, or <c>nativeDataFormats[0]</c> for the element of an array member.</param>
internal sealed record RecordPosition(string ParentCName, string Designator);
