# ConfigCat SDK for .NET

### Supported frameworks:
- .NET Core
- .NET Framework
- Xamarin
- .NET Standard

https://configcat.com

ConfigCat SDK for .NET provides easy integration for your application to ConfigCat.

ConfigCat is a feature flag and configuration management service that lets you separate releases from deployments. You can turn your features ON/OFF using <a href="https://app.configcat.com" target="_blank">ConfigCat Dashboard</a> even after they are deployed. ConfigCat lets you target specific groups of users based on region, email or any other custom user attribute.

ConfigCat is a <a href="https://configcat.com" target="_blank">hosted feature flag service</a>. Manage feature toggles across frontend, backend, mobile, desktop apps. <a href="https://configcat.com" target="_blank">Alternative to LaunchDarkly</a>. Management app + feature flag SDKs.

[![Build status](https://ci.appveyor.com/api/projects/status/3kygp783vc2uv9xr?svg=true)](https://ci.appveyor.com/project/ConfigCat/net-sdk) [![NuGet Version](https://buildstats.info/nuget/ConfigCat.Client)](https://www.nuget.org/packages/ConfigCat.Client/)
[![codecov](https://codecov.io/gh/configcat/.net-sdk/branch/master/graph/badge.svg)](https://codecov.io/gh/configcat/.net-sdk)
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

### 3. Go to <a href="https://app.configcat.com/sdkkey" target="_blank">Connect your application</a> tab to get your *SDK Key*:
![SDK-KEY](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/media/readme02-2.png  "SDK-KEY")

### 4. Create a **ConfigCat** client instance:
```c#
var client = new ConfigCatClient("#YOUR-SDK-KEY#");
```

> We strongly recommend using the *ConfigCat Client* as a Singleton object in your application.

### 5. Get your setting value:
```c#
var isMyAwesomeFeatureEnabled = client.GetValue("isMyAwesomeFeatureEnabled", false);

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
> To ensure graceful shutdown of the client you should invoke ```.Dispose()``` method. (Client implements [IDisposable](https://msdn.microsoft.com/en-us/library/system.idisposable(v=vs.110).aspx) interface)

## Getting user specific setting values with Targeting
Using this feature, you will be able to get different setting values for different users in your application by passing a `User Object` to the `GetValue()` function.

Read more about [Targeting here](https://configcat.com/docs/advanced/targeting).
```c#
User currentUser = new User("435170f4-8a8b-4b67-a723-505ac7cdea92");

var isMyAwesomeFeatureEnabled = client.GetValue(
	"isMyAwesomeFeatureEnabled",
	defaultValue: false,
	user: currentUser);
```

## Sample/Demo apps
  * [Sample Console App](https://github.com/configcat/.net-sdk/tree/master/samples/ConsoleApp)
  * [Sample Web App](https://github.com/configcat/.net-sdk/tree/master/samples/ASP.NETCore)

## Polling Modes
The ConfigCat SDK supports 3 different polling mechanisms to acquire the setting values from ConfigCat. After latest setting values are downloaded, they are stored in the internal cache then all requests are served from there. Read more about Polling Modes and how to use them at [ConfigCat Docs](https://configcat.com/docs/sdk-reference/csharp/).

## Need help?
https://configcat.com/support

## Contributing
Contributions are welcome. For more info please read the [Contribution Guideline](CONTRIBUTING.md).

## About ConfigCat
- [Documentation](https://configcat.com/docs)
- [Blog](https://configcat.com/blog)
