using ConfigCat.Client.Evaluate;
using System.Collections.Generic;

namespace ConfigCat.Client
{
    internal interface IConfigDeserializer
    {
        bool TryDeserialize(ProjectConfig projectConfig, out IDictionary<string, Setting> settings);
    }
}
