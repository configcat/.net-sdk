namespace ConfigCat.Client.Evaluation;

internal readonly struct EvaluateResult
{
    public EvaluateResult(SettingValueContainer selectedValue, TargetingRule? matchedTargetingRule = null, PercentageOption? matchedPercentageOption = null)
    {
        this.SelectedValue = selectedValue;
        this.MatchedTargetingRule = matchedTargetingRule;
        this.MatchedPercentageOption = matchedPercentageOption;
    }

    public readonly SettingValueContainer SelectedValue;
    public readonly TargetingRule? MatchedTargetingRule;
    public readonly PercentageOption? MatchedPercentageOption;
}
