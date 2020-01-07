using ConfigCat.Client.Evaluate;

namespace ConfigCat.Client
{
    internal interface IConfigDeserializer
    {
        bool TryDeserialize(ProjectConfig projectConfig, out ConfigJson settings);
    }
}
