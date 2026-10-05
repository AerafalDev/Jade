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

    /// <summary>The flags of the parse that collects declarations, which also records the macro definitions.</summary>
    private const CXTranslationUnit_Flags CollectFlags = ParseFlags | CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord;

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
    /// <param name="configuration">The headers, include directories, forced includes and annotations to parse with.</param>
    /// <param name="exclude">The declarations the configuration leaves out, with the reason.</param>
    /// <returns>The declarations of the library's own headers for each target.</returns>
    /// <exception cref="InvalidDataException">libclang fails or reports a warning or an error, or a declaration cannot be bound.</exception>
    public ParsedHeaders Parse(string library, string sourceDirectory, ClangConfiguration configuration, IReadOnlyDictionary<string, string> exclude)
    {
        // The entry file only exists in memory: it includes the configured headers in order, then the shims.
        var entryFile = Path.Combine(_layout.SourceCacheDirectory, $"{library}.c");
        var files = new HeaderFiles(sourceDirectory, configuration.IncludeDirectories, _layout.Root, configuration.Shims);
        var entryContents = string.Concat(configuration.Headers.Select(static header => $"#include <{header}>\n"))
            + string.Concat(files.ShimPaths.Select(static shim => $"#include \"{shim.Replace('\\', '/')}\"\n"));
        var patterns = new MacroPatterns(configuration);
        var names = new Dictionary<Target, IReadOnlySet<HeaderDeclaration>>();
        var byTarget = new List<TargetDeclarations>();

        using var index = CXIndex.Create();

        foreach (var target in _targets)
        {
            var arguments = GetArguments(sourceDirectory, configuration, target);
            TargetDeclarations collected;

            using (var translationUnit = ParseTranslationUnit(index, library, target, entryFile, entryContents, arguments, CollectFlags))
            {
                names.Add(target, CollectDeclarationNames(translationUnit, files));
                collected = TargetModelBuilder.Collect(translationUnit, target, files, configuration, exclude);
            }

            // A macro defined twice, identically, is recorded twice.
            var selected = collected.Macros
                .Where(macro => !configuration.ExcludeHeaders.ContainsKey(macro.Header) && patterns.IsSelected(macro.Name))
                .DistinctBy(static macro => macro.Name, StringComparer.Ordinal)
                .ToDictionary(static macro => macro.Name, StringComparer.Ordinal);

            if (selected.Count > 0)
            {
                using var evaluation = ParseTranslationUnit(index, library, target, entryFile, entryContents + MacroEvaluator.GetSource(selected.Values), arguments, ParseFlags);

                collected = collected with { MacroValues = MacroEvaluator.Evaluate(evaluation, selected, library, target) };
            }

            byTarget.Add(collected);
        }

        return new ParsedHeaders(_targets, names, byTarget);
    }

    /// <summary>Parses the entry file for one target.</summary>
    /// <param name="index">The libclang index.</param>
    /// <param name="library">The key of the library, for error messages.</param>
    /// <param name="target">The target.</param>
    /// <param name="entryFile">The path of the entry file, which only exists in memory.</param>
    /// <param name="entryContents">The contents of the entry file.</param>
    /// <param name="arguments">The command line.</param>
    /// <param name="flags">The parse options.</param>
    /// <returns>The translation unit, which the caller disposes.</returns>
    /// <exception cref="InvalidDataException">libclang fails, or reports a warning or an error.</exception>
    private static TranslationUnit ParseTranslationUnit(CXIndex index, string library, Target target, string entryFile, string entryContents, string[] arguments, CXTranslationUnit_Flags flags)
    {
        using var unsavedFile = CXUnsavedFile.Create(entryFile, entryContents);
        var error = CXTranslationUnit.TryParse(index, entryFile, arguments, [unsavedFile], flags, out var handle);

        if (error != CXErrorCode.CXError_Success)
        {
            throw new InvalidDataException($"{library} ({target.Triple}): libclang failed to parse the headers ({error}).");
        }

        var translationUnit = TranslationUnit.GetOrCreate(handle);

        try
        {
            EnsureNoDiagnostics(library, target, handle);
        }
        catch
        {
            translationUnit.Dispose();
            throw;
        }

        return translationUnit;
    }

    /// <summary>Gets the kind of a declaration the input report lists.</summary>
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

    /// <summary>Collects the names of the top-level declarations that come from the library's own headers, for the input report.</summary>
    /// <param name="translationUnit">The parsed translation unit.</param>
    /// <param name="files">The files that belong to the library.</param>
    /// <returns>The declarations.</returns>
    /// <remarks>
    /// Builtin declarations have no file, and the generator's runtime headers lie outside the
    /// library; neither belongs to it.
    /// </remarks>
    private static HashSet<HeaderDeclaration> CollectDeclarationNames(TranslationUnit translationUnit, HeaderFiles files)
    {
        var declarations = new HashSet<HeaderDeclaration>();

        foreach (var declaration in translationUnit.TranslationUnitDecl.Decls)
        {
            if (declaration is not NamedDecl named || declaration.Handle.IsAnonymous || GetKind(declaration) is not { } kind)
            {
                continue;
            }

            declaration.Location.GetFileLocation(out var file, out _, out _, out _);
            using var fileName = file.Name;

            if (files.GetHeader(fileName.ToString()) is not null)
            {
                _ = declarations.Add(new HeaderDeclaration(kind, named.Name));
            }
        }

        return declarations;
    }

    /// <summary>Builds the libclang command line for one target.</summary>
    /// <param name="sourceDirectory">The directory of the library's sources.</param>
    /// <param name="configuration">The include directories, forced includes and defines.</param>
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
            .. configuration.Defines.Select(static define => $"-D{define}"),
            .. configuration.IncludeDirectories.SelectMany(directory => new[] { "-I", Path.Combine(sourceDirectory, directory) }),
            .. configuration.ForcedIncludes.SelectMany(file => new[] { "-include", Path.Combine(_layout.Root, file) }),
        ];
    }
}
