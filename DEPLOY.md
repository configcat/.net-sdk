# Steps to Deploy
1. Run tests
2. Set version in `appveyor.yml`
   
   e.g: from `version: 2.5.{build}` to `version: 2.6.{build}`
3. Push to `master`
4. Deploy to NuGet.org

    Create a new deployment on  https://ci.appveyor.com/project/configcat/net-sdk/deployments
5. Make sure new package is available via Nuget.org: https://www.nuget.org/packages/ConfigCat.Client

    *Usually it takes a few minutes to propagate.*
6. Add release notes: https://github.com/configcat/.net-sdk/releases