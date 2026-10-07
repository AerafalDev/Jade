namespace Jade.BindingGenerator.Projection;

/// <summary>The idiomatic layer of an interop library, ready to be emitted (ADR 0040).</summary>
/// <param name="Namespace">The namespace of the library.</param>
/// <param name="Handles">The handles and their members.</param>
/// <param name="Structures">The mirrors, element mirrors and snapshots.</param>
/// <param name="ValueStructures">The value structures that gain extension interfaces or constants.</param>
/// <param name="Slots">The slots of the extensions of nested chain roots.</param>
/// <param name="Omitted">The functions, members and structures left out or written by hand, with the reason, for the report.</param>
/// <param name="InternalConstants">The C names of the constants that stay internal.</param>
/// <param name="ChainNext">The name of the field of the raw chain header that links the next extension.</param>
/// <param name="ChainType">The name of the field of the raw chain header that identifies the structure.</param>
internal sealed record IdiomaticLibrary(
    string Namespace,
    IReadOnlyList<IdiomaticHandle> Handles,
    IReadOnlyList<IdiomaticStructure> Structures,
    IReadOnlyList<IdiomaticValueStructure> ValueStructures,
    IReadOnlyList<IdiomaticSlots> Slots,
    IReadOnlyList<(string Name, string Reason)> Omitted,
    IReadOnlyList<string> InternalConstants,
    string ChainNext,
    string ChainType);
