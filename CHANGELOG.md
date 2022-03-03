### 6.2.1
- Reducing the number of json deserializations between `GetValue` calls.
### 6.1.20
- Bugfix: The SDK's json serialization behavior is not depending on the `JsonConvert.DefaultSettings` anymore.
### 6.1.0
- Bugfix (#17)
### 6.0.0
- Addressing global data handling and processing trends via Data Governance feature. Customers can control the geographic location where their config JSONs get published to. [See the docs](https://configcat.com/docs/advanced/data-governance/).
We are introducing a new DataGovernance initialization parameter. Set this parameter to be in sync with the Data Governance preference on the [Dashboard](https://app.configcat.com/organization/data-governance).
       
### Breaking change:
- Custom cache implementations should implement the new cache interface using key parameter in the get/set methods.
### 5.3.0
- VariationID, bugfix (#11)
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