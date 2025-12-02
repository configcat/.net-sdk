using System.Collections.Generic;
using System.Linq;

namespace ConfigCat.Client.Override;

internal sealed class LocalDictionaryDataSource : IOverrideDataSource
{
    private readonly Dictionary<string, Setting> initialSettings;
    private readonly IDictionary<string, object>? overrideValues;

    public LocalDictionaryDataSource(IDictionary<string, object> overrideValues, bool watchChanges)
    {
        this.initialSettings = overrideValues.ToDictionary(kv => kv.Key, kv => Setting.FromValue(kv.Value));
        if (watchChanges)
        {
            this.overrideValues = overrideValues;
        }
    }

    public IReadOnlyDictionary<string, Setting> GetOverrides() => this.overrideValues is not null
        ? this.overrideValues.ToDictionary(kv => kv.Key, kv => Setting.FromValue(kv.Value))
        : this.initialSettings;
}
