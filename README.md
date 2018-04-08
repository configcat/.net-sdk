# ConfigCat SDK for .NET
ConfigCat is a cloud based configuration as a service. It integrates with your apps, backends, websites, and other programs, so you can configure them through this website even after they are deployed.
https://configcat.com  

[![Build status](https://ci.appveyor.com/api/projects/status/3kygp783vc2uv9xr?svg=true)](https://ci.appveyor.com/project/ConfigCat/net-sdk) [![NuGet Version](https://buildstats.info/nuget/ConfigCat.Client)](https://www.nuget.org/packages/ConfigCat.Client/)
## Getting Started

 1. Install [NuGet](http://docs.nuget.org/docs/start-here/using-the-package-manager-console) package: [ConfigCat.Client](https://www.nuget.org/packages/ConfigCat.Client)
 ```PowerShell
 Install-Package ConfigCat.Client
 ```
 2. Get your Project secret from [configcat.com](https://configcat.com) portal:
![ProjectSecret](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/media/readme02.png  "ProjectSecret")

 3. Create a **ConfigCatClient** instance:
```c#
var client = new ConfigCatClient("#YOUR-PROJECT-SECRET#");
```
 4. Get your config value:
```c#
var isMyAwesomeFeatureEnabled = client.GetValue("isMyAwesomeFeatureEnabled", false);

if(isMyAwesomeFeatureEnabled)
{
    //show your awesome feature to the world!
}
```
5. On application exit:
``` c#
client.Dispose();
```

## Configuration
Client supports three different caching policies to acquire the configuration from ConfigCat. When the client downloads the latest configuration, puts it into the internal cache and serves any configuration acquisition from cache. With these caching policies you can manage your configurations' lifetimes easily.

### Auto polling (default)
Client downloads the latest configuration and puts into a cache repeatedly. Use ```PollingIntervalSeconds``` parameter to manage polling interval.
You can subscribe to the ```OnConfigurationChanged``` event to get notification about configuration changes. 

### Lazy loading
Client downloads the latest configuration only when it is not present or expired in the cache. Use ```CacheTimeToLiveSeconds``` parameter to manage configuration lifetime.

### Manual polling
With this mode you always have to invoke ```.ForceRefresh()``` method to download a latest configuration into the cache. When the cache is empty (for example after client initialization) and you try to acquire any value you'll get the default value!

---

Configuration parameters are different in each mode:
### Base configuration
| PropertyName        | Description           | Default  |
| --- | --- | --- |
| ```ProjectSecret```      | Project secret to access your configuration.  | REQUIRED |
| ```LoggerFactory``` | Factory to create an `ILogger` instance for tracing.        | `NullTrace` (no default tracing method) | 
### Auto polling
| PropertyName        | Description           | Default  |
| --- | --- | --- |
| ```PollIntervalSeconds ```      | Polling interval in seconds.|   60 | 
| ```MaxInitWaitTimeSeconds```      | Maximum waiting time between the client initialization and the first config acquisition in seconds.|   5 |
### Lazy loading
| PropertyName        | Description           | Default  |
| --- | --- | --- | 
| ```CacheTimeToLiveSeconds```      | Use this value to manage the cache's TTL. |   60 |

### Example - increase CacheTimeToLiveSeconds to 600 seconds
``` c#
var clientConfiguration = new ConfigCat.Client.Configuration.LazyLoadConfiguration
            {
                ProjectSecret = "#YOUR-PROJECT-SECRET#",
                CacheTimeToLiveSeconds = 600
            };

IConfigCatClient client = new ConfigCatClient(clientConfiguration);
```
### Example - OnConfigurationChanged 
In Auto polling mode you can subscribe an event to get notification about changes.
``` c#
var client = new ConfigCatClient(projectSecret);

client.OnConfigurationChanged += (s, a) => 
{
	  // Configuration changed. Update UI!
}
```
### Example - default value handling
You can easily manage default values with this technique when you use your configuration in many locations in the code.
``` c#
var client = new ConfigCatClient("#YOUR-PROJECT-SECRET#");

bool isMyAwesomeFeatureEnabled = client.GetConfiguration(MyApplicationFeatureConfig.Default).MyNewFeatureEnabled;

if (isMyAwesomeFeatureEnabled)
{
	//show your awesome feature to the world!
}

internal sealed class MyApplicationFeatureConfig
{
	public static readonly MyApplicationFeatureConfig Default = new MyApplicationFeatureConfig
		{
			// set my default values here
			MyNewFeatureEnabled = false
		};

	public bool MyNewFeatureEnabled { get; set; }
}
```
You can customize deserialization settings with [```Newtonsoft.Json.JsonProperty```](https://www.newtonsoft.com/json/help/html/JsonPropertyName.htm):
``` c#
[JsonProperty("My_New_Feature_Enabled")]
public bool MyNewFeatureEnabled { get; set; }
```
### Configuration with clientbuilder
It is possible to use ```ConfigCatClientBuilder``` to build ConfigCatClient instance:

``` c#
IConfigCatClient client = ConfigCatClientBuilder
	.Initialize("YOUR-PROJECT-SECRET")
	.WithLazyLoad()
	.WithCacheTimeToLiveSeconds(120)
	.Build();
```

## Members
### Methods
| Name        | Description           |
| :------- | :--- |
| ``` GetValue<T>(string key, T defaultValue) ``` | Returns the value of the key |
| ``` ForceRefresh() ``` | Fetches the latest configuration from the server. You can use this method with WebHooks to ensure up to date configuration values in your application. ([see ASP.Net sample project to use webhook for cache invalidation](https://github.com/ConfigCat/.net-sdk/blob/master/samples/ASP.NETCore/WebApplication/Controllers/BackdoorController.cs)) |
| ``` GetConfigurationJsonString() ``` | Return configuration as a json string |
| ``` T GetConfiguration<T>(T defaultValue) ``` | Serialize the configuration to a passed **T** type. You can customize your **T** with Newtonsoft attributes |
### Events
| Name        | Description           |
| :------- | :--- |
| ``` OnConfigurationChanged ``` | Only with AutoPolling policy. Occurs when the configuration changed |


## Lifecycle of the client
We're recommend to use client as a singleton in your application. Today you can do this easily with any IoC contanier ([see ASP.Net sample project](https://github.com/ConfigCat/.net-sdk/blob/master/samples/ASP.NETCore/WebApplication/Startup.cs#L25)).
### Dispose
To ensure graceful shutdown of the client you should use ```.Dispose()``` method. (Client implements [IDisposable](https://msdn.microsoft.com/en-us/library/system.idisposable(v=vs.110).aspx) interface)
 
## Logging
The client doesn't use any external logging framework. If you want to add your favourite logging library you have to create an adapter to ```ILogger``` and setup a ```.LoggerFactory``` in ```ConfigurationBase```.

## License
[MIT](https://raw.githubusercontent.com/ConfigCat/.net-sdk/master/LICENSE)
