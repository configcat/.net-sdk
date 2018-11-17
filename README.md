# ConfigCat SDK for .NET

ConfigCat is a feature flag, feature toggle, and configuration management service. That lets you launch new features and change your software configuration remotely without actually (re)deploying code. ConfigCat even helps you do controlled roll-outs like canary releases and blue-green deployments.
https://configcat.com  

[![Build status](https://ci.appveyor.com/api/projects/status/3kygp783vc2uv9xr?svg=true)](https://ci.appveyor.com/project/ConfigCat/net-sdk) [![NuGet Version](https://buildstats.info/nuget/ConfigCat.Client)](https://www.nuget.org/packages/ConfigCat.Client/)

## Getting Started

### 1. Install [NuGet](http://docs.nuget.org/docs/start-here/using-the-package-manager-console) package: [ConfigCat.Client](https://www.nuget.org/packages/ConfigCat.Client)
 ```PowerShell
 Install-Package ConfigCat.Client
 ```
### 2. <a href="https://configcat.com/Account/Login" target="_blank">Log in to ConfigCat Management Console</a> and go to your *Project* to get your *API Key*:
![ApiKey](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/media/readme01.png  "ApiKey")

### 3. Create a **ConfigCatClient** instance:
```c#
var client = new ConfigCatClient("#YOUR-API-KEY#");
```

> We strongly recommend using the *ConfigCat Client* as a Singleton object in your application.

### 4. Get your setting value:
```c#
var isMyAwesomeFeatureEnabled = client.GetValue("isMyAwesomeFeatureEnabled", false);

if(isMyAwesomeFeatureEnabled)
{
    //show your awesome feature to the world!
}
```
### 5. On application exit:
``` c#
client.Dispose();
```
> To ensure graceful shutdown of the client you should invoke ```.Dispose()``` method. (Client implements [IDisposable](https://msdn.microsoft.com/en-us/library/system.idisposable(v=vs.110).aspx) interface)

## Getting user specific setting values with Targeting
Using this feature, you will be able to get different setting values for different users in your application by passing a `User Object` to the ```GetValue()``` function.

Read more about [Targeting here](https://docs.configcat.com/docs/advanced/targeting/).
```c#

User currentUser = new User("435170f4-8a8b-4b67-a723-505ac7cdea92");

var isMyAwesomeFeatureEnabled = client.GetValue(
	"isMyAwesomeFeatureEnabled",
	defaultValue: false,
	user: currentUser);
```

## Sample/Demo app
  * [Sample Console App](https://github.com/configcat/.net-sdk/tree/master/samples/ConsoleApp)
  * [Sample WebApplication](https://github.com/configcat/.net-sdk/tree/master/samples/ASP.NETCore)

## Polling Modes
The ConfigCat SDK supports 3 different polling mechanisms to acquire the setting values from ConfigCat. After latest setting values are downloaded, they are stored in the internal cache then all requests are served from there. Read more about Polling Modes and how to use them at [ConfigCat Docs](https://docs.configcat.com/docs/sdk-reference/csharp/).

## Support
If you need help how to use this SDK feel free to to contact the ConfigCat Staff on https://configcat.com. We're happy to help.

## Contributing
Contributions are welcome.

## License
[MIT](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/LICENSE)
