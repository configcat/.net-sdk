cd src\ConfigCat.Client.Tests

nuget install Appveyor.TestLogger -Version 2.0.0

OpenCover.Console.exe -register:user -target:dotnet.exe -targetargs:"test ConfigCat.Client.Tests.csproj -c Release --test-adapter-path:. --logger:Appveyor --logger:trx;LogFileName=%APPVEYOR_BUILD_FOLDER%\testresult.xml" -output:%APPVEYOR_BUILD_FOLDER%\coverage.xml -filter:"+[*]ConfigCat.Client.* -[ConfigCatClientTests]* -[*]ConfigCat.Client.Versioning.*" -oldstyle -returntargetcode

cd %APPVEYOR_BUILD_FOLDER%