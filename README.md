# ConfigCat SDK for .NET

[![Build status](https://github.com/configcat/.net-sdk/actions/workflows/dotnet-sdk-ci.yml/badge.svg?branch=master)](https://github.com/configcat/.net-sdk/actions/workflows/dotnet-sdk-ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/ConfigCat.Client)](https://www.nuget.org/packages/ConfigCat.Client/)
[![Sonar Coverage](https://img.shields.io/sonar/coverage/net-sdk?logo=SonarCloud&server=https%3A%2F%2Fsonarcloud.io)](https://sonarcloud.io/project/overview?id=net-sdk)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=net-sdk&metric=alert_status)](https://sonarcloud.io/dashboard?id=net-sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/configcat/.net-sdk/blob/master/LICENSE)

ConfigCat SDK for .NET provides easy integration for your application to [ConfigCat](https://configcat.com).

## Supported runtimes

- .NET 6+
- .NET Framework 4.6.2+
- Other runtimes which implement .NET Standard 2.0+ like .NET Core 2.0+, Xamarin.Android 8.0+, Xamarin.iOS 10.14+, etc. (For more details, see the [Platform compatiblity](#platform-compatibility) section below.)

## Getting started

### 1. Install the package with [NuGet](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)

The ___core package___, which works across all supported .NET applications, can be installed as follows:

```bash
dotnet add package ConfigCat.Client
```

or

```PowerShell
Install-Package ConfigCat.Client
```

For ASP.NET Core applications and other modern .NET applications built on [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) (`Microsoft.Extensions.Hosting`)
or [.NET's standard dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview) (`Microsoft.Extensions.DependencyInjection`),
it's recommended to install the ___integration package___:

```bash
dotnet add package ConfigCat.Extensions.Hosting
```

or

```PowerShell
Install-Package ConfigCat.Extensions.Hosting
```

For Unity, see the instructions [here](https://configcat.com/docs/sdk-reference/unity/).

### 2. Import ConfigCat namespaces to your application

**If using the core package:**

```c#
using ConfigCat.Client;
```

**If using the integration package:**

```c#
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting;
```

### 3. Go to the <a href="https://app.configcat.com/sdkkey" target="_blank">ConfigCat Dashboard</a> to get your _SDK Key_:

![SDK-KEY](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/media/readme02-3.png  "SDK-KEY")

### 4. Obtain a _ConfigCat client_ instance:

**If using the core package:**

```c#
var client = ConfigCatClient.Get("#YOUR-SDK-KEY#");
```

> [!NOTE]
> You can acquire singleton client instances for your SDK keys using the `ConfigCatClient.Get(sdkKey: <sdkKey>)` static factory method.
(However, please keep in mind that subsequent calls to `ConfigCatClient.Get()` with the _same SDK Key_ return a _shared_ client instance, which was set up by the first call.)

**If using the integration package:**

1. First, configure your application host or DI services:

   - If your application uses the [modern, linear, property-based configuration style](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder#host-builder-options):
 
     ```c#
     var configCatBuilder = builder.UseConfigCat();
     configCatBuilder.AddDefaultClient(options =>
     {
         options.SdkKey = "#YOUR-SDK-KEY#";
     });
     ```
 
   - If your application uses the [traditional, callback-based approach](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder#host-builder-options):
 
     ```c#
     builder.ConfigureConfigCat(configCatBuilder =>
     {
         configCatBuilder.AddDefaultClient(options =>
         {
             options.SdkKey = "#YOUR-SDK-KEY#";
         });
     });
     ```
 
   - If your application performs setup via a host builder not compatible with [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
     or manually builds a DI container using [ServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollection):
 
     ```c#
     services.AddConfigCat(configCatBuilder =>
     {
         configCatBuilder.AddDefaultClient(options =>
         {
             options.SdkKey = "#YOUR-SDK-KEY#";
         });
     });
     ```

1. Build the application host or DI container.

1. Finally, resolve the singleton `IConfigCatClient` service from the DI container via constructor injection, method injection, etc.,
    or by `ServiceProvider.GetRequiredService<IConfigCatClient>()`. E.g.:

    ```c#
    var client = app.Services.GetRequiredService<IConfigCatClient>();
    ```

### 5. Get your setting value:

```c#
var isMyAwesomeFeatureEnabled = await client.GetValueAsync("isMyAwesomeFeatureEnabled", false);

if (isMyAwesomeFeatureEnabled)
{
    doTheNewThing();
}
else
{
    doTheOldThing();
}
```

### 6. On application exit:

**If using the core package:**

```c#
client.Dispose();
```

> [!NOTE]
> To ensure graceful shutdown of the client, you should invoke the `Dispose()` method. (The client implements the [IDisposable](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable) interface.)
> Alternatively, you can close all open clients at once using the `ConfigCatClient.DisposeAll()` method.

**If using the integration package:**

You don't need to explicitly dispose of the resolved `IConfigCatClient` service. It will be automatically disposed when the application host or DI container is disposed.

## Getting user-specific setting values with targeting

This feature allows you to get different setting values for different users in your application by passing a [User Object](https://configcat.com/docs/targeting/user-object/) to `GetValueAsync()`.

Read more about targeting [here](https://configcat.com/docs/advanced/targeting/).

```c#
var currentUser = new User("#USER-IDENTIFIER#");

var isMyAwesomeFeatureEnabled = await client.GetValueAsync(
	"isMyAwesomeFeatureEnabled",
	defaultValue: false,
	user: currentUser);
```

## Sample/demo apps
  * [Sample Console App](https://github.com/configcat/.net-sdk/tree/master/samples/ConsoleApp)
  * [Sample Multi-Page Web App](https://github.com/configcat/.net-sdk/tree/master/samples/ASP.NETCore)
  * [Sample Single-Page Web App](https://github.com/configcat/.net-sdk/tree/master/samples/BlazorWasm)
  * [Sample Mobile/Windows Store App](https://github.com/configcat/.net-sdk/tree/master/samples/MAUI)
  * [Sample Windows Service (Worker Service built on .NET Generic Host)](https://github.com/configcat/.net-sdk/tree/master/samples/WindowsService)

## Polling modes

The ConfigCat SDK supports 3 different polling strategies to fetch feature flags and settings from the ConfigCat CDN. Once the latest data is downloaded, it is stored in the cache, then the SDK uses the cached data to evaluate feature flags and settings. Read more about polling modes and how to use them at [ConfigCat Docs](https://configcat.com/docs/sdk-reference/dotnet/#polling-modes).

## Platform compatibility

The ConfigCat SDK supports all the widespread .NET JIT runtimes, everything that implements [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)+ and supports TLS 1.2 should work.
Starting with v9.3.0, it can also be used in applications that employ [trimmed self-contained](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained) or various [ahead-of-time (AOT) compilation](https://en.wikipedia.org/wiki/Ahead-of-time_compilation) deployment models.

Based on our tests, the SDK is compatible with the following runtimes/deployment models:
* .NET Framework 4.6.2+ (including Ngen)
* .NET Core 2.0+, .NET 5+ (including Crossgen2/ReadyToRun and Native AOT)
* Mono 5.10+
* .NET for Android (formerly known as Xamarin.Android)
* .NET for iOS (formerly known as Xamarin.iOS)
* Unity 2021.3+ (Mono JIT)
* Unity 2021.3+ (IL2CPP)<sup><small>*</small></sup>
* Universal Windows Platform 10.0.16299.0+ (.NET Native)<sup><small>**</small></sup>
* WebAssembly (Mono AOT/Emscripten, also known as wasm-tools)

<sup><small>*</small></sup>Unity WebGL also works but needs a bit of extra effort: you will need to enable WebGL compatibility by calling the `ConfigCatClient.PlatformCompatibilityOptions.EnableUnityWebGLCompatibility` method. For more details, see [Sample Scripts](https://github.com/configcat/.net-sdk/tree/master/samples/UnityWebGL).<br/>
<sup><small>**</small></sup>To make the SDK work in Release builds on UWP, you will need to add `<Namespace Name="System.Text.Json.Serialization.Converters" Browse="Required All"/>` to your application's [.rd.xml](https://learn.microsoft.com/en-us/windows/uwp/dotnet-native/runtime-directives-rd-xml-configuration-file-reference) file. See also [this discussion](https://github.com/dotnet/runtime/issues/29912#issuecomment-638471351).

> [!NOTE]
> We strive to provide an extensive support for the various .NET runtimes and versions. If you still encounter an issue with the SDK on some platform, please open a [GitHub issue](https://github.com/configcat/.net-sdk/issues/new/choose) or [contact support](https://configcat.com/support).

## Need help?

https://configcat.com/support

## Contributing

Contributions are welcome. For more info please read the [Contribution Guideline](CONTRIBUTING.md).

## About ConfigCat

ConfigCat is a feature flag and configuration management service that lets you separate releases from deployments. You can turn your features ON/OFF using <a href="https://app.configcat.com" target="_blank">ConfigCat Dashboard</a> even after they are deployed. ConfigCat lets you target specific groups of users based on region, email or any other custom user attribute.

ConfigCat is a <a href="https://configcat.com" target="_blank">hosted feature flag service</a>. Manage feature toggles across frontend, backend, mobile, desktop apps. <a href="https://configcat.com" target="_blank">Alternative to LaunchDarkly</a>. Management app + feature flag SDKs.

- [Official ConfigCat SDKs for other platforms](https://github.com/configcat#-official-open-source-sdks)
- [Documentation](https://configcat.com/docs)
- [Blog](https://configcat.com/blog)
