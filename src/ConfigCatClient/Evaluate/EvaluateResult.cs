#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace ConfigCat.Client.Evaluate
{
    internal class EvaluateResult
    {
#if USE_NEWTONSOFT_JSON
        public JValue Value { get; set; }
#else
        public JsonElement Value { get; set; }
#endif

        public string VariationId { get; set; }

        public SettingTypeEnum SettingType { get; set; }
    }
}