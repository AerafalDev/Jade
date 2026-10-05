#!/usr/bin/env dotnet
#:package ClangSharp
// The libclang native library comes from a RID-specific package, which is only restored when the
// app has a runtime identifier; UseCurrentRuntimeIdentifier gives it the host's.
#:property UseCurrentRuntimeIdentifier=true
#:include binding-generator/*.cs
#:include binding-generator/Clang/*.cs
#:include binding-generator/Configuration/*.cs
#:include binding-generator/Dawn/*.cs
#:include binding-generator/Emission/*.cs
#:include binding-generator/Model/*.cs
#:include binding-generator/Projection/*.cs
#:include binding-generator/Reporting/*.cs
#:include binding-generator/Sources/*.cs
#:include binding-generator/Targets/*.cs

using Jade.BindingGenerator;

using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var scriptDirectory = AppContext.GetData("EntryPointFileDirectoryPath") as string;

return await Generator.RunAsync(scriptDirectory, Console.Out, Console.Error, cancellation.Token).ConfigureAwait(false);
