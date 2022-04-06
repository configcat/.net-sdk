# Steps to Deploy
1. Run tests
2. Set version in `appveyor.yml` (e.g: from `build_version: 6.5.0` to `build_version: 6.5.1`)
3. Update release notes in ConfigCatClient.csproj (PackageReleaseNotes)
4. Push to `master`
5. Deploy to NuGet.org

    Create a new deployment on  https://ci.appveyor.com/project/configcat/net-sdk/deployments
6. Make sure new package is available via Nuget.org: https://www.nuget.org/packages/ConfigCat.Client
7. Update and test sample apps with the new SDK version.

    *Usually it takes a few minutes to propagate.*
8. Add release notes: https://github.com/configcat/.net-sdk/releases