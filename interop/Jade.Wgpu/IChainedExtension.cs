namespace Jade.Wgpu;

/// <summary>An extension that a call chains to a structure of type <typeparamref name="TRoot"/> (ADR 0029).</summary>
/// <typeparam name="TSelf">The type of the extension.</typeparam>
/// <typeparam name="TRoot">The type of the structure it extends.</typeparam>
/// <remarks>
/// The members are internal, so that only the generated extensions implement the interface: the
/// compiler then rejects an extension passed with a structure it does not extend.
/// </remarks>
public unsafe interface IChainedExtension<TSelf, TRoot>
    where TSelf : IChainedExtension<TSelf, TRoot>, allows ref struct
    where TRoot : allows ref struct
{
    /// <summary>Writes the raw form of an extension into the memory of a call, with its structure type set.</summary>
    /// <param name="extension">The extension.</param>
    /// <param name="arena">The memory of the call.</param>
    /// <returns>The chain header of the raw extension, not linked to anything.</returns>
    internal static abstract Raw.ChainedStruct* Lower(scoped in TSelf extension, scoped ref Arena arena);
}
