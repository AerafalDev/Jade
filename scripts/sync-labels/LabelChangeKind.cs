/// <summary>What a sync does to one label.</summary>
internal enum LabelChangeKind
{
    /// <summary>The label already matches its declaration.</summary>
    Unchanged,

    /// <summary>No label with the name or one of its aliases exists: it is created.</summary>
    Create,

    /// <summary>The label exists with another color or description, or with its name in another case.</summary>
    Update,

    /// <summary>A label named after one of the aliases exists: it takes the declared name, color and description.</summary>
    Rename,

    /// <summary>The label exists but .github/labels.yml does not declare it: deleted with --delete, kept otherwise.</summary>
    Undeclared,
}
