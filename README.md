# ConfigCat SDK for .NET

### Supported runtimes:
- .NET 5+
- .NET Framework 4.5+
- Other runtimes which implement .NET Standard 2.0+ like .NET Core 2.0+, Xamarin.Android 8.0+, Xamarin.iOS 10.14+, etc. (For more details, please refer to [this table](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0).)

Starting with v9.3.0, the ConfigCat SDK can be used in applications that employ [trimmed self-contained](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trim-self-contained) or various ahead-of-time (AOT) compilation deployment models.
The SDK has been tested with the following AOT solutions:
* [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) - see also [Sample .NET Console app](https://github.com/configcat/.net-sdk/tree/master/samples/ConsoleApp)
* [Mono WebAssembly AOT/Emscripten (wasm-tools)](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-build-tools-and-aot?view=aspnetcore-8.0) - see also [Sample ASP.NET Core Blazor WebAssembly app](https://github.com/configcat/.net-sdk/tree/master/samples/BlazorWasm)
* [IL2CPP](https://docs.unity3d.com/2021.3/Documentation/Manual/IL2CPP.html) - see also [Sample Unity WebGL scripts](https://github.com/configcat/.net-sdk/tree/master/samples/UnityWebGL)

ConfigCat SDK for .NET provides easy integration for your application to ConfigCat.

ConfigCat is a feature flag and configuration management service that lets you separate releases from deployments. You can turn your features ON/OFF using [ConfigCat Dashboard](https://app.configcat.com) even after they are deployed. ConfigCat lets you target specific groups of users based on region, email or any other custom user attribute.

ConfigCat is a [hosted feature flag service](https://configcat.com). Manage feature toggles across frontend, backend, mobile, desktop apps. [Alternative to LaunchDarkly](https://configcat.com). Management app + feature flag SDKs.

[![Build status](https://ci.appveyor.com/api/projects/status/3kygp783vc2uv9xr?svg=true)](https://ci.appveyor.com/project/ConfigCat/net-sdk) [![NuGet Version](https://buildstats.info/nuget/ConfigCat.Client)](https://www.nuget.org/packages/ConfigCat.Client/)
[![Sonar Coverage](https://img.shields.io/sonar/coverage/net-sdk?logo=SonarCloud&server=https%3A%2F%2Fsonarcloud.io)](https://sonarcloud.io/project/overview?id=net-sdk) 
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=net-sdk&metric=alert_status)](https://sonarcloud.io/dashboard?id=net-sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/configcat/.net-sdk/blob/master/LICENSE)


## Getting Started

### 1. Install the [package](https://www.nuget.org/packages/ConfigCat.Client) with [NuGet](http://docs.nuget.org/docs/start-here/using-the-package-manager-console) 
```PowerShell
Install-Package ConfigCat.Client
```
or
```bash
dotnet add package ConfigCat.Client
```

### 2. Import *ConfigCat.Client* to your application
```c#
using ConfigCat.Client;
```

### 3. Go to the [ConfigCat Dashboard](https://app.configcat.com/sdkkey) to get your *SDK Key*:
![SDK-KEY](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/media/readme02-3.png  "SDK-KEY")

### 4. Create a **ConfigCat** client instance:
```c#
var client = ConfigCatClient.Get("#YOUR-SDK-KEY#");
```

> You can acquire singleton client instances for your SDK keys using the `ConfigCatClient.Get(sdkKey: <sdkKey>)` static factory method.
(However, please keep in mind that subsequent calls to `ConfigCatClient.Get()` with the *same SDK Key* return a *shared* client instance, which was set up by the first call.)

### 5. Get your setting value:
```c#
var isMyAwesomeFeatureEnabled = await client.GetValueAsync("isMyAwesomeFeatureEnabled", false);

if(isMyAwesomeFeatureEnabled)
{
    doTheNewThing();
}
else
{
    doTheOldThing();
}
```

### 6. On application exit:
``` c#
client.Dispose();
```
> To ensure graceful shutdown of the client you should invoke ```.Dispose()``` method. (Client implements [IDisposable](https://msdn.microsoft.com/en-us/library/system.idisposable(v=vs.110).aspx) interface.)
> Alternatively, you can also close all open clients at once using the `ConfigCatClient.DisposeAll()` method.

## Getting user specific setting values with Targeting
Using this feature, you will be able to get different setting values for different users in your application by passing a `User Object` to the `GetValue()` function.

Read more about [Targeting here](https://configcat.com/docs/advanced/targeting).
```c#
User currentUser = new User("435170f4-8a8b-4b67-a723-505ac7cdea92");

var isMyAwesomeFeatureEnabled = await client.GetValueAsync(
	"isMyAwesomeFeatureEnabled",
	defaultValue: false,
	user: currentUser);
```

## Sample/Demo apps
  * [Sample Console App](https://github.com/configcat/.net-sdk/tree/master/samples/ConsoleApp)
  * [Sample Multi Page Web App](https://github.com/configcat/.net-sdk/tree/master/samples/ASP.NETCore)
  * [Sample Single Page Web App](https://github.com/configcat/.net-sdk/tree/master/samples/BlazorWasm)
  * [Sample Mobile/Windows Store App](https://github.com/configcat/.net-sdk/tree/master/samples/MAUI)
  
## Polling Modes
The ConfigCat SDK supports 3 different polling mechanisms to acquire the setting values from ConfigCat. After latest setting values are downloaded, they are stored in the internal cache then all requests are served from there. Read more about Polling Modes and how to use them at [ConfigCat Docs](https://configcat.com/docs/sdk-reference/dotnet/).

## Need help?
https://configcat.com/support

## Contributing
Contributions are welcome. For more info please read the [Contribution Guideline](CONTRIBUTING.md).

## About ConfigCat
- [Documentation](https://configcat.com/docs)
- [Blog](https://configcat.com/blog)
