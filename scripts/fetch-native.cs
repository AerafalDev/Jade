#!/usr/bin/env dotnet
#:include fetch-native/*.cs
#:include build-native/Build/HostPlatform.cs
#:include build-native/Build/NativePlatform.cs
#:include build-native/Configuration/BuildLayout.cs
#:include build-native/Tools/Command.cs
#:include build-native/Tools/CommandFailedException.cs

using Jade.NativeFetch;

using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var scriptDirectory = AppContext.GetData("EntryPointFileDirectoryPath") as string;

return await NativeFetch.RunAsync(scriptDirectory, args, Console.Out, Console.Error, cancellation.Token).ConfigureAwait(false);
