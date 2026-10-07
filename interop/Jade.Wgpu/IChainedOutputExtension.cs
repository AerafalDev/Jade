namespace Jade.Wgpu;

/// <summary>An extension that the library fills in when a call chains it to a structure of type <typeparamref name="TRoot"/> (ADR 0040).</summary>
/// <typeparam name="TSelf">The type of the extension: a value structure or a managed copy.</typeparam>
/// <typeparam name="TRoot">The type of the structure it extends.</typeparam>
/// <remarks>The members are internal, so that only the generated extensions implement the interface.</remarks>
public unsafe interface IChainedOutputExtension<TSelf, TRoot>
    where TSelf : IChainedOutputExtension<TSelf, TRoot>
{
    /// <summary>Allocates the raw form of an extension in the memory of a call, initialized and with its structure type set.</summary>
    /// <param name="arena">The memory of the call.</param>
    /// <returns>The chain header of the raw extension, not linked to anything.</returns>
    internal static abstract Raw.ChainedStruct* Allocate(scoped ref Arena arena);

    /// <summary>Reads an extension the library filled in, and frees what it allocated for it.</summary>
    /// <param name="chain">The chain header that <see cref="Allocate"/> returned.</param>
    /// <returns>The extension.</returns>
    internal static abstract TSelf Raise(Raw.ChainedStruct* chain);
}
