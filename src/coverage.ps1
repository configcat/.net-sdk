.\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe -register:user -target:dotnet.exe -targetargs:`"test ConfigCatClientTests\ConfigCatClientTests.csproj -f net45 -c Release`" -output:.\coverage.xml -filter:`"+[ConfigCat*]* -[ConfigCat.Client]Tests.*`"

