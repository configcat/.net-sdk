namespace ConfigCat.Client.Evaluation;

internal readonly struct EvaluateResult
{
    public EvaluateResult(SettingValueContainer selectedValue, TargetingRule? matchedTargetingRule = null, PercentageOption? matchedPercentageOption = null)
    {
        this.selectedValue = selectedValue;
        this.MatchedTargetingRule = matchedTargetingRule;
        this.MatchedPercentageOption = matchedPercentageOption;
    }

    private readonly SettingValueContainer selectedValue;
    public SettingValue Value => this.selectedValue.Value;
    public string? VariationId => this.selectedValue.VariationId;

    public readonly TargetingRule? MatchedTargetingRule;
    public readonly PercentageOption? MatchedPercentageOption;
}
