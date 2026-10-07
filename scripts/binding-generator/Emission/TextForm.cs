namespace Jade.BindingGenerator.Emission;

/// <summary>How an overload takes the text parameters of its method (ADR 0016).</summary>
internal enum TextForm
{
    /// <summary>The method takes no text.</summary>
    None,

    /// <summary>Text as UTF-8 spans, the overload that resolution prefers.</summary>
    Utf8,

    /// <summary>Text as strings, encoded to UTF-8 for the call.</summary>
    String,
}
