using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Override;

internal interface IOverrideDataSource
{
    Dictionary<string, Setting> GetOverrides();

    Task<Dictionary<string, Setting>> GetOverridesAsync(CancellationToken cancellationToken = default);
}
