using ConfigCat.Client.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConfigCat.Client.Override
{
    internal sealed class LocalDictionaryDataSource : IOverrideDataSource
    {
        private readonly IDictionary<string, Setting> initialSettings;
        private readonly IDictionary<string, object> overrideValues;

        public LocalDictionaryDataSource(IDictionary<string, object> overrideValues, bool watchChanges)
        {
            this.initialSettings = overrideValues.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting());
            if (watchChanges)
            {
                this.overrideValues = overrideValues;
            }
        }

        public IDictionary<string, Setting> GetOverrides() => GetSettingsFromSource();

        public Task<IDictionary<string, Setting>> GetOverridesAsync() => Task.FromResult(GetSettingsFromSource());

        public void Dispose() { /* no need to dispose anything */ }

        private IDictionary<string, Setting> GetSettingsFromSource() => overrideValues != null
            ? overrideValues.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting())
            : this.initialSettings;
    }
}
