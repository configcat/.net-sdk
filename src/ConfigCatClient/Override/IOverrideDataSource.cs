using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client.Override;

internal interface IOverrideDataSource : IDisposable
{
    Dictionary<string, Setting> GetOverrides();

    Task<Dictionary<string, Setting>> GetOverridesAsync(CancellationToken cancellationToken = default);
}
