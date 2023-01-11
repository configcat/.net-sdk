using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client.Override;

internal interface IOverrideDataSource : IDisposable
{
    IDictionary<string, Setting> GetOverrides();

    Task<IDictionary<string, Setting>> GetOverridesAsync();
}
