using System.Collections.Generic;

namespace ConfigCat.Client.Evaluate
{
    internal interface IRolloutEvaluator
    {
        T Evaluate<T>(IDictionary<string, Setting> settings, string key, T defaultValue, User user = null);

        object Evaluate(IDictionary<string, Setting> settings, string key, object defaultValue, User user = null);

        string EvaluateVariationId(IDictionary<string, Setting> settings, string key, string defaultVariationId, User user = null);
    }
}