#if USE_NEWTONSOFT_JSON
using JsonValue = Newtonsoft.Json.Linq.JValue;
#else
using JsonValue = System.Text.Json.JsonElement;
#endif

namespace ConfigCat.Client.Evaluation;

internal readonly struct EvaluateResult
{
    public EvaluateResult(JsonValue value, string? variationId, RolloutRule? matchedTargetingRule = null, RolloutPercentageItem? matchedPercentageOption = null)
    {
        Value = value;
        VariationId = variationId;
        MatchedTargetingRule = matchedTargetingRule;
        MatchedPercentageOption = matchedPercentageOption;
    }

    public JsonValue Value { get; }
    public string? VariationId { get; }
    public RolloutRule? MatchedTargetingRule { get; }
    public RolloutPercentageItem? MatchedPercentageOption { get; }
}
