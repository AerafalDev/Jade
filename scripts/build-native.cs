#!/usr/bin/env dotnet
#:include build-native/*.cs
#:include build-native/Build/*.cs
#:include build-native/Configuration/*.cs
#:include build-native/Sources/*.cs
#:include build-native/Tools/*.cs

using Jade.NativeBuild;

using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var scriptDirectory = AppContext.GetData("EntryPointFileDirectoryPath") as string;

return await NativeBuild.RunAsync(scriptDirectory, args, Console.Out, Console.Error, cancellation.Token).ConfigureAwait(false);
