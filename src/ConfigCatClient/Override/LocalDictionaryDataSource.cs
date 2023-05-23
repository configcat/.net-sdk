using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Override;

internal sealed class LocalDictionaryDataSource : IOverrideDataSource
{
    private readonly Dictionary<string, Setting> initialSettings;
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

    public Dictionary<string, Setting> GetOverrides() => GetSettingsFromSource();

    public Task<Dictionary<string, Setting>> GetOverridesAsync(CancellationToken cancellationToken = default) => Task.FromResult(GetSettingsFromSource());

    private Dictionary<string, Setting> GetSettingsFromSource() => this.overrideValues is not null
        ? this.overrideValues.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting())
        : this.initialSettings;
}
