namespace Jade.BindingGenerator.Model;

/// <summary>The direction of a structure chain: who fills the chained structures in.</summary>
internal enum ChainDirection
{
    /// <summary>The structure takes no part in a chain.</summary>
    None,

    /// <summary>The caller fills the chain in, as for descriptors.</summary>
    In,

    /// <summary>The library fills the chain in, as for queried properties.</summary>
    Out,
}
