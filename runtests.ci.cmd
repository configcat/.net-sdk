cd src\ConfigCat.Client.Tests
nuget install Appveyor.TestLogger -Version 2.0.0

dotnet build --nologo

dotnet test --test-adapter-path:. --logger:Appveyor --logger:trx;LogFileName=%APPVEYOR_BUILD_FOLDER%\testresult.xml --no-restore --no-build --nologo

cd %APPVEYOR_BUILD_FOLDER%