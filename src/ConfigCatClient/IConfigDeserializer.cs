using ConfigCat.Client.Evaluate;
using System.Collections.Generic;

namespace ConfigCat.Client
{
    internal interface IConfigDeserializer
    {
        bool TryDeserialize(string config, out SettingsWithPreferences settings);
    }
}
