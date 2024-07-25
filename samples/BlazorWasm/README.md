# Sample ASP.NET Core Blazor WebAssembly app

This is a simple [ASP.NET Core Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor) application to demonstrate how to use the ConfigCat SDK.

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. Run app
    ```bash 
    dotnet run -- urls=http://localhost:5000
    ```
3. Open http://localhost:5000 in browser

## Ahead-of-time (AOT) compilation

The sample app also demonstrates that the ConfigCat SDK can be used in [Blazor Wasm applications using AOT compilation](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot).

1. Make sure you have [the .NET WebAssembly build tools](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-8.0#net-webassembly-build-tools) installed in your development environment.
    ```bash 
    dotnet workload install wasm-tools
    ```
2. Execute the build script corresponding to your OS (`build-aot.cmd` on Windows, `build-aot.sh` on Linux).
3. Locate the web assets in the publish output directory (`bin/Release/net8.0/publish/wwwroot`).
4. Start a local web server in this directory to serve the files over HTTP. E.g.
    ```bash 
    dotnet serve --port 5000
    ```
5. Navigate to http://localhost:5000 in your browser.
