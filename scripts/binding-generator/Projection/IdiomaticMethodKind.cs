namespace Jade.BindingGenerator.Projection;

/// <summary>Where and how an idiomatic method is declared on its handle (ADR 0040).</summary>
internal enum IdiomaticMethodKind
{
    /// <summary>An instance method.</summary>
    Method,

    /// <summary>A read-only property, for a getter without argument that returns a value it does not own.</summary>
    Property,

    /// <summary>A static method, for a free function placed on the handle it creates or queries.</summary>
    Static,

    /// <summary><c>AddRef</c>.</summary>
    AddRef,

    /// <summary><c>Release</c>, which <c>Dispose</c> calls too.</summary>
    Release,
}
