### 6.5.1
- Add net461 to the target frameworks list to force the usage of `System.Text.Json` rather than `Newtonsoft.Json`.

### 6.5.0
- Replace `FileSystemWatcher` with file polling in local file override data source.

### 6.4.12
- Fix various local file override data source issues.

### 6.4.9
- Move the PollingMode option to public scope.

### 6.4.8
- Readd `System.Text.RegularExpressions` version `4.3.1` due to SNYK security report.

### 6.4.7
- Remove unused `System.Text.RegularExpressions` dependency.

### 6.4.6
- Fix the wait time calculation in auto-polling mode.

### 6.4.3
- Fix README links displayed on the NuGet package page.

### 6.4.0
- **Introduced a new configuration API replacing the builder pattern**:

  ```cs
  ConfigCatClientBuilder
      .Initialize(SDKKEY)
      .WithLogger(consoleLogger)
      .WithAutoPoll()
          .WithMaxInitWaitTimeSeconds(5)
          .WithPollIntervalSeconds(60)
      .Create();
  ```
  
  Will look like this:
  ```cs
  new ConfigCatClient(options =>
  {
      options.SdkKey = SDKKEY;
      options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(5));
      options.Logger = consoleLogger;
  });
  ```
  
  The old API is still available it's just marked with the `[Obsolete]` attribute.

- **`GetAllValues()` and `GetAllValuesAsync()`**:  
  These methods are now evaluating all feature flags and settings into an `IDictionary<string, object>`.

- **FlagOverrides**:  
  It's now possible to feed the SDK with local feature flag and setting values. 
  - **Dictionary**
    ```cs
    var dict = new Dictionary<string, object>
    {
        {"enabledFeature", true},
        {"intSetting", 5},
    };

    using var client = new ConfigCatClient(options =>
    {
        options.SdkKey = "localhost";
        options.FlagOverrides = FlagOverrides.LocalDictionary(dict, 
            OverrideBehaviour.LocalOnly);
    });
    ```
  - **File**
    ```cs
    using var client = new ConfigCatClient(options =>
    {
        options.SdkKey = "localhost";
        options.FlagOverrides = FlagOverrides.LocalFile("path/to/file", 
            autoReload: false, 
            overrideBehaviour: OverrideBehaviour.LocalOnly);
    });
    ```
  Three behaviours available: `LocalOnly`, `LocalOverRemote`, and `RemoteOverLocal`.
  With `LocalOnly` the SDK switches into a complete offline state, and only the override values are served.
  `LocalOverRemote` and `RemoteOverLocal` merge the local and remote feature flag values respecting one or another in case of key duplications.

- **Changes in JSON handling**:  
  In respect of [#30](https://github.com/configcat/.net-sdk/issues/30) `System.Text.Json` is favored over `Newtonsoft.Json` in frameworks newer than `net45`. `System.Text.Json` is not available for `net45` so that target remains using `Newtonsoft.Json`.

- **`net5.0` and `net6.0` target frameworks**.

- **HttpTimeout configuration option**.

- **Solution for** [#26](https://github.com/configcat/.net-sdk/issues/26).
  To prevent possible deadlocks the following changes were applied:
  - Created a synchronous extension for the existing fully async `IConfigCache`. In the future we will replace that interface with the new one (`IConfigCatCache`) that has now the sync API and inherits the async API from `IConfigCache`.  `IConfigCache` was marked with `[Obsolete]` to maintain backward compatibility. `InMemoryConfigCache` now implements both sync and async APIs through `IConfigCatCache`.
  - Extended the config services (`AutoPoll`, `LazyLoad`, `ManualPoll`) with synchronous branches that are using the new cache's sync / async methods in respect of sync and async customer calls.
  - Extended the `HttpConfigFetcher` with a synchronous `Fetch` that uses the `HttpClient`'s `Send()` method where it's available (`net5.0` and above). Below `net5.0` the synchronous path falls back to a functionality that queues the HTTP request to a thread pool thread and waits for its completion. This solution prevents deadlocks however, it puts more load on the thread pool.

- **CI Changes**:
  - Introduced new [GitHub actions for Linux and macOS builds](https://github.com/configcat/.net-sdk/actions/workflows/linux-macOS-CI.yml). 
  - The [sonarcloud analysis](https://github.com/configcat/.net-sdk/actions/workflows/sonar-analysis.yml) is moved to a separate Action from the appveyor task. 
  - Removed codecov completely, it will be replaced by the coverage data from sonarcloud.

### 6.2.1
- Reducing the number of json deserializations between `GetValue` calls.
### 6.1.20
- Bugfix: The SDK's json serialization behavior is not depending on the `JsonConvert.DefaultSettings` anymore.
### 6.1.0
- Bugfix ([#17](https://github.com/configcat/.net-sdk/issues/17))
### 6.0.0
- Addressing global data handling and processing trends via Data Governance feature. Customers can control the geographic location where their config JSONs get published to. [See the docs](https://configcat.com/docs/advanced/data-governance/).
We are introducing a new DataGovernance initialization parameter. Set this parameter to be in sync with the Data Governance preference on the [Dashboard](https://app.configcat.com/organization/data-governance).
       
#### Breaking change:
- Custom cache implementations should implement the new cache interface using key parameter in the get/set methods.
### 5.3.0
- VariationID, bugfix ([#11](https://github.com/configcat/.net-sdk/issues/11))
### 5.2.0
- Bugfix (config fetch, caching)
### 5.1.0
- Remove semver nuget packages
### 5.0.0
- Breaking change: Renamed `API Key` to `SDK Key`.
### 4.0.0
- Supporting sensitive text comparators.
### 3.2.0
- Minor fix in info level logging
### 3.1.0
- Added new semantic version tests
### 3.0.0
- Support new types (number, semver), detailed log entries, compressed http communication
### 2.5.0
- Support custom HttpClientHandler
### 2.4.0
- Add GetAllKeys() function
### 2.3.0
- BaseUrl override oppurtunity
- IConfigCache override oppurtunity
### 2.3.0
- BaseUrl override oppurtunity
- IConfigCache override oppurtunity
### 2.2.1
- Bugfix (logger level)
### 2.2.0
- Namespace unification
### 2.1.0
- Rollout handling v2
### 2.0.1
- Bugfix
### 2.0.0
- Implement rollout feature
### 1.0.7
- Implement LazyLoad, AutoPoll, ManualPoll feature
### 1.0.6
- Finalize logging
### 1.0.5
- Implement tracing, add clear cache ability to client
### 1.0.4
- Initial release