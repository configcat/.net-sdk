using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client.Override;

internal sealed class LocalDictionaryDataSource : IOverrideDataSource
{
    private readonly IDictionary<string, Setting> settings;

    public LocalDictionaryDataSource(IDictionary<string, object> overrideValues)
    {
        this.settings = overrideValues.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting());
    }

    public IDictionary<string, Setting> GetOverrides() => this.settings;

    public Task<IDictionary<string, Setting>> GetOverridesAsync() => Task.FromResult(this.settings);

    public void Dispose() { /* no need to dispose anything */ }
}
