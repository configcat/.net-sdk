using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client.Override;

internal sealed class LocalDictionaryDataSource : IOverrideDataSource
{
    private readonly IDictionary<string, Setting> initialSettings;
    private readonly IDictionary<string, object>? overrideValues;

    public LocalDictionaryDataSource(IDictionary<string, object> overrideValues, bool watchChanges)
    {
        this.initialSettings = overrideValues.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting());
        if (watchChanges)
        {
            this.overrideValues = overrideValues;
        }
    }

    public void Dispose() { /* no need to dispose anything */ }

    public IDictionary<string, Setting> GetOverrides() => GetSettingsFromSource();

    public Task<IDictionary<string, Setting>> GetOverridesAsync() => Task.FromResult(GetSettingsFromSource());

    private IDictionary<string, Setting> GetSettingsFromSource() => this.overrideValues is not null
        ? this.overrideValues.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting())
        : this.initialSettings;
}
