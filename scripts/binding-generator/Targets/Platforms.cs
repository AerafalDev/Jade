namespace Jade.BindingGenerator.Targets;

/// <summary>A set of platform families, which says where a declaration is available (ADR 0026).</summary>
[Flags]
internal enum Platforms
{
    /// <summary>No platform.</summary>
    None = 0,

    /// <summary>Windows.</summary>
    Windows = 1 << 0,

    /// <summary>Linux.</summary>
    Linux = 1 << 1,

    /// <summary>macOS.</summary>
    MacOS = 1 << 2,

    /// <summary>iOS, devices and simulators.</summary>
    IOS = 1 << 3,

    /// <summary>Android.</summary>
    Android = 1 << 4,

    /// <summary>The browser, through WebAssembly.</summary>
    Browser = 1 << 5,

    /// <summary>Every platform that runs native code, the browser excluded.</summary>
    Native = Windows | Linux | MacOS | IOS | Android,

    /// <summary>Every supported platform.</summary>
    All = Native | Browser,
}
