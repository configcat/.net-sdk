# Sample Windows Service

This is a simple Windows Service to demonstrate how to use the ConfigCat SDK.

1. Install [.NET](https://dotnet.microsoft.com/download)

2. Run sample app as a plain console application:
```bash
dotnet run
```

3. Install and run sample app as a Windows Service

Follow [these instructions](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service).

## Ahead-of-time (AOT) compilation

The sample app also demonstrates that the ConfigCat SDK can be used in .NET 8+ applications compiled to native code using [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

1. Make sure you have [the prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#prerequisites) installed in your development environment.
2. Execute the build script (`build-aot.cmd`).
3. Locate the executable in the publish output directory (`bin/Release/net8.0/win-x64/publish`).
4. Install the executable as a Windows Service.