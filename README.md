# ConfigCat SDK for .NET

[![Build status](https://ci.appveyor.com/api/projects/status/3kygp783vc2uv9xr?svg=true)](https://ci.appveyor.com/project/ConfigCat/net-sdk)
[![NuGet Version](https://img.shields.io/nuget/v/ConfigCat.Client)](https://www.nuget.org/packages/ConfigCat.Client/)
[![Sonar Coverage](https://img.shields.io/sonar/coverage/net-sdk?logo=SonarCloud&server=https%3A%2F%2Fsonarcloud.io)](https://sonarcloud.io/project/overview?id=net-sdk) 
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=net-sdk&metric=alert_status)](https://sonarcloud.io/dashboard?id=net-sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/configcat/.net-sdk/blob/master/LICENSE)

ConfigCat SDK for .NET provides easy integration for your application to [ConfigCat](https://configcat.com).

## Supported runtimes
- .NET 6+
- .NET Framework 4.6.2+
- Other runtimes which implement .NET Standard 2.0+ like .NET Core 2.0+, Xamarin.Android 8.0+, Xamarin.iOS 10.14+, etc. (For more details, see the [Platform compatiblity](#platform-compatibility) section below.)

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
Using this feature, you will be able to get different setting values for different users in your application by passing a `User Object` to the `GetValueAsync()` function.

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
  * [Sample Multi-page Web App](https://github.com/configcat/.net-sdk/tree/master/samples/ASP.NETCore)
  * [Sample Single-page Web App](https://github.com/configcat/.net-sdk/tree/master/samples/BlazorWasm)
  * [Sample Mobile/Windows Store App](https://github.com/configcat/.net-sdk/tree/master/samples/MAUI)
  
## Polling Modes
The ConfigCat SDK supports 3 different polling mechanisms to acquire the setting values from ConfigCat. After latest setting values are downloaded, they are stored in the internal cache then all requests are served from there. Read more about Polling Modes and how to use them at [ConfigCat Docs](https://configcat.com/docs/sdk-reference/dotnet/).

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

> We strive to provide an extensive support for the various .NET runtimes and versions. If you still encounter an issue with the SDK on some platform, please open a [GitHub issue](https://github.com/configcat/.net-sdk/issues/new/choose) or [contact support](https://configcat.com/support).

## Need help?
https://configcat.com/support

## Contributing
Contributions are welcome. For more info please read the [Contribution Guideline](CONTRIBUTING.md).

## About ConfigCat
ConfigCat is a feature flag and configuration management service that lets you separate releases from deployments. You can turn your features ON/OFF using <a href="https://app.configcat.com" target="_blank">ConfigCat Dashboard</a> even after they are deployed. ConfigCat lets you target specific groups of users based on region, email or any other custom user attribute.

ConfigCat is a <a href="https://configcat.com" target="_blank">hosted feature flag service</a>. Manage feature toggles across frontend, backend, mobile, desktop apps. <a href="https://configcat.com" target="_blank">Alternative to LaunchDarkly</a>. Management app + feature flag SDKs.

- [Official ConfigCat SDKs for other platforms](https://github.com/configcat)
- [Documentation](https://configcat.com/docs)
- [Blog](https://configcat.com/blog)
