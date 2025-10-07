# How to Deploy

## Before deployment

Make sure the CI is running: https://github.com/configcat/.net-sdk/actions/workflows/dotnet-sdk-ci.yml

## Steps

1. Run tests

   ```PowerShell
   dotnet test src/ConfigCatClient.sln
   ```

1. Create tag on GitHub or push tag to remote

    If you tag the commit, a GitHub action automatically publishes the package to NPM.
    ```PowerShell
    git push origin <new version>
    ```
    Example: `git push origin v1.1.15`

    You can follow the build status [here](https://github.com/configcat/.net-sdk/actions/workflows/dotnet-sdk-ci.yml).

1. Add release notes: https://github.com/configcat/.net-sdk/releases

1. Update and test sample apps with the new SDK version

   Usually it takes a few minutes for NuGet packages to become available.
