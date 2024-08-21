# Sample .NET Console app

This is a simple .NET Console application to demonstrate how to use the ConfigCat SDK.

1. Install [.NET](https://dotnet.microsoft.com/download)

2. Run sample app:
```bash
dotnet run
```

## Ahead-of-time (AOT) compilation

The sample app also demonstrates that the ConfigCat SDK can be used in .NET 8+ applications compiled to native code using [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

1. Make sure you have [the prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#prerequisites) installed in your development environment.
2. Execute the build script corresponding to your OS (`build-aot.cmd` on Windows, `build-aot.sh` on Linux).
3. Locate the executable in the publish output directory (`bin/Release/net8.0/win-x64/native` on Windows, `bin/Release/net8.0/linux-x64/native` on Linux).
4. Run the executable.