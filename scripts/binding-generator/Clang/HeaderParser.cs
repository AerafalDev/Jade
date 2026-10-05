using ClangSharp;
using ClangSharp.Interop;
using Jade.BindingGenerator.Configuration;
using Jade.BindingGenerator.Targets;

namespace Jade.BindingGenerator.Clang;

/// <summary>Parses a library's C headers with libclang, once for every target (ADR 0026).</summary>
internal sealed class HeaderParser
{
    /// <summary>The flags of every parse; function bodies carry nothing the bindings need.</summary>
    private const CXTranslationUnit_Flags ParseFlags = CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies;

    /// <summary>The repository layout, which locates the generator's headers and the forced includes.</summary>
    private readonly RepositoryLayout _layout;

    /// <summary>The targets every library is parsed for.</summary>
    private readonly IReadOnlyList<Target> _targets;

    /// <summary>Initializes a new instance of the <see cref="HeaderParser"/> class.</summary>
    /// <param name="layout">The repository layout.</param>
    /// <param name="targets">The targets to parse for, in the order of ADR 0012.</param>
    /// <exception cref="InvalidDataException">The loaded libclang is not the one of the ClangSharp package.</exception>
    public HeaderParser(RepositoryLayout layout, IReadOnlyList<Target> targets)
    {
        LibClang.EnsurePackageVersion();

        _layout = layout;
        _targets = targets;
    }

    /// <summary>Parses a library's headers for every target.</summary>
    /// <param name="library">The key of the library, which names its entry file in errors.</param>
    /// <param name="sourceDirectory">The directory of the library's sources.</param>
    /// <param name="configuration">The headers, include directories and forced includes to parse with.</param>
    /// <returns>The declarations of the library's own headers for each target.</returns>
    /// <exception cref="InvalidDataException">libclang fails, or reports a warning or an error.</exception>
    public ParsedHeaders Parse(string library, string sourceDirectory, ClangConfiguration configuration)
    {
        // The entry file only exists in memory: it includes the configured headers in order.
        var entryFile = Path.Combine(_layout.SourceCacheDirectory, $"{library}.c");
        var entryContents = string.Concat(configuration.Headers.Select(static header => $"#include <{header}>\n"));
        var declarations = new Dictionary<Target, IReadOnlySet<HeaderDeclaration>>();

        using var index = CXIndex.Create();
        using var unsavedFile = CXUnsavedFile.Create(entryFile, entryContents);

        foreach (var target in _targets)
        {
            var arguments = GetArguments(sourceDirectory, configuration, target);
            var error = CXTranslationUnit.TryParse(index, entryFile, arguments, [unsavedFile], ParseFlags, out var handle);

            if (error != CXErrorCode.CXError_Success)
            {
                throw new InvalidDataException($"{library} ({target.Triple}): libclang failed to parse the headers ({error}).");
            }

            using var translationUnit = TranslationUnit.GetOrCreate(handle);

            EnsureNoDiagnostics(library, target, handle);
            declarations.Add(target, CollectDeclarations(translationUnit, sourceDirectory));
        }

        return new ParsedHeaders(_targets, declarations);
    }

    /// <summary>Gets the kind of a declaration the generator collects.</summary>
    /// <param name="declaration">A top-level declaration.</param>
    /// <returns>Its kind, or <see langword="null"/> for any other declaration.</returns>
    private static HeaderDeclarationKind? GetKind(Decl declaration)
    {
        return declaration switch
        {
            FunctionDecl => HeaderDeclarationKind.Function,
            RecordDecl => HeaderDeclarationKind.Record,
            EnumDecl => HeaderDeclarationKind.Enum,
            TypedefNameDecl => HeaderDeclarationKind.Typedef,
            VarDecl => HeaderDeclarationKind.Variable,
            _ => null,
        };
    }

    /// <summary>Fails when libclang reports any warning or error.</summary>
    /// <param name="library">The key of the library, for the error message.</param>
    /// <param name="target">The target of the parse, for the error message.</param>
    /// <param name="handle">The parsed translation unit.</param>
    /// <exception cref="InvalidDataException">libclang reported at least one warning or error.</exception>
    /// <remarks>A warning means the parse differs from what the native build compiles.</remarks>
    private static void EnsureNoDiagnostics(string library, Target target, CXTranslationUnit handle)
    {
        var messages = new List<string>();

        for (var i = 0u; i < handle.NumDiagnostics; i++)
        {
            using var diagnostic = handle.GetDiagnostic(i);

            if (diagnostic.Severity >= CXDiagnosticSeverity.CXDiagnostic_Warning)
            {
                using var message = diagnostic.Format(CXDiagnostic.DefaultDisplayOptions);
                messages.Add(message.ToString());
            }
        }

        if (messages.Count > 0)
        {
            throw new InvalidDataException($"{library} ({target.Triple}):{Environment.NewLine}{string.Join(Environment.NewLine, messages)}");
        }
    }

    /// <summary>Collects the named top-level declarations that come from the library's own headers.</summary>
    /// <param name="translationUnit">The parsed translation unit.</param>
    /// <param name="sourceDirectory">The directory of the library's sources.</param>
    /// <returns>The declarations.</returns>
    /// <remarks>
    /// Builtin declarations have no file, and the generator's runtime headers lie outside the
    /// sources; neither belongs to the library.
    /// </remarks>
    private static HashSet<HeaderDeclaration> CollectDeclarations(TranslationUnit translationUnit, string sourceDirectory)
    {
        var root = Path.GetFullPath(sourceDirectory) + Path.DirectorySeparatorChar;
        var declarations = new HashSet<HeaderDeclaration>();

        foreach (var declaration in translationUnit.TranslationUnitDecl.Decls)
        {
            if (declaration is not NamedDecl named || declaration.Handle.IsAnonymous || GetKind(declaration) is not { } kind)
            {
                continue;
            }

            declaration.Location.GetFileLocation(out var file, out _, out _, out _);
            using var fileName = file.Name;
            var path = fileName.ToString();

            if (path.Length > 0 && Path.GetFullPath(path).StartsWith(root, StringComparison.Ordinal))
            {
                _ = declarations.Add(new HeaderDeclaration(kind, named.Name));
            }
        }

        return declarations;
    }

    /// <summary>Builds the libclang command line for one target.</summary>
    /// <param name="sourceDirectory">The directory of the library's sources.</param>
    /// <param name="configuration">The include directories and forced includes.</param>
    /// <param name="target">The target to parse for.</param>
    /// <returns>The arguments.</returns>
    /// <remarks>
    /// <c>-nostdinc</c> keeps every system and builtin header out: the C runtime comes from the
    /// generator's own headers, so every triple parses the same way on every machine.
    /// </remarks>
    private string[] GetArguments(string sourceDirectory, ClangConfiguration configuration, Target target)
    {
        return
        [
            "-x", "c",
            "-std=c17",
            "-ffreestanding",
            "-nostdinc",
            "-target", target.Triple,
            "-isystem", _layout.RuntimeHeadersDirectory,
            .. configuration.IncludeDirectories.SelectMany(directory => new[] { "-I", Path.Combine(sourceDirectory, directory) }),
            .. configuration.ForcedIncludes.SelectMany(file => new[] { "-include", Path.Combine(_layout.Root, file) }),
        ];
    }
}
