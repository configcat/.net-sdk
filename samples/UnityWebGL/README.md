# Sample script files for Unity WebGL

This folder contains a few C# script files that show how you can integrate and use the ConfigCat SDK in your Unity WebGL application or game.

Since NuGet packages cannot be referenced in Unity projects directly, the SDK's assembly file (`ConfigCat.Client.dll`) and its dependencies must be added manually. You will need to include the *netstandard2.0* builds of the following assemblies:
* [ConfigCat.Client v9.3.0+](https://www.nuget.org/packages/ConfigCat.Client)
* [Microsoft.Bcl.AsyncInterfaces v6.0.0](https://www.nuget.org/packages/Microsoft.Bcl.AsyncInterfaces/6.0.0)
* [System.Buffers v4.5.1](https://www.nuget.org/packages/System.Buffers/4.5.1)
* [System.Memory v4.5.4](https://www.nuget.org/packages/System.Memory/4.5.4)
* [System.Numerics.Vectors v4.5.0](https://www.nuget.org/packages/System.Numerics.Vectors/4.5.0)
* [System.Runtime.CompilerServices.Unsafe v6.0.0](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0)
* [System.Text.Encodings.Web v6.0.0](https://www.nuget.org/packages/System.Text.Encodings.Web/6.0.0)
* [System.Text.Json v6.0.10](https://www.nuget.org/packages/System.Text.Json/6.0.10)
* [System.Threading.Tasks.Extensions v4.5.4](https://www.nuget.org/packages/System.Threading.Tasks.Extensions/4.5.4)

Tested on Unity 2021.3 LTS and 6000.0.