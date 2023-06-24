namespace ConfigCat.Client.Evaluation;

internal readonly struct EvaluateResult
{
    public EvaluateResult(SettingValue value, string? variationId, TargetingRule? matchedTargetingRule = null, PercentageOption? matchedPercentageOption = null)
    {
        Value = value;
        VariationId = variationId;
        MatchedTargetingRule = matchedTargetingRule;
        MatchedPercentageOption = matchedPercentageOption;
    }

    public SettingValue Value { get; }
    public string? VariationId { get; }
    public TargetingRule? MatchedTargetingRule { get; }
    public PercentageOption? MatchedPercentageOption { get; }
}
