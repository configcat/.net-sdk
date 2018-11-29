OpenCover.Console.exe -register:user -target:dotnet.exe -targetargs:"test src\ConfigCatClientTests\ConfigCatClientTests.csproj -f net45 -c Release" -output:src/coverage.xml -filter:"+[*]ConfigCat.Client.* -[ConfigCatClientTests]*"

pause
