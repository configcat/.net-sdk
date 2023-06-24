using System;
using System.Globalization;
using System.Linq;
using ConfigCat.Client.Versioning;

using static System.FormattableString;

namespace ConfigCat.Client.Evaluation;

internal sealed class RolloutEvaluator : IRolloutEvaluator
{
    public const string InvalidValuePlaceholder = "<invalid value>";
    public const string InvalidOperatorPlaceholder = "<invalid operator>";

    private readonly LoggerWrapper logger;

    public RolloutEvaluator(LoggerWrapper logger)
    {
        this.logger = logger;
    }

    public EvaluateResult Evaluate(in EvaluateContext context)
    {
        var evaluateLog = context.Log;

        try
        {
            EvaluateResult evaluateResult;

            var setting = context.Setting;
            var targetingRules = setting.TargetingRules;
            var percentageOptions = setting.PercentageOptions;

            if (context.User is not null)
            {
                // evaluate targeting rules

                if (TryEvaluateRules(targetingRules, context, out evaluateResult))
                {
                    evaluateLog.ReturnValue = evaluateResult.Value.ToString();
                    evaluateLog.VariationId = evaluateResult.VariationId;

                    return evaluateResult;
                }

                // evaluate percentage options

                if (TryEvaluatePercentageRules(percentageOptions, context, out evaluateResult))
                {
                    evaluateLog.ReturnValue = evaluateResult.Value.ToString();
                    evaluateLog.VariationId = evaluateResult.VariationId;

                    return evaluateResult;
                }
            }
            else if (targetingRules.Length > 0 || percentageOptions.Length > 0)
            {
                this.logger.TargetingIsNotPossible(context.Key);
            }

            // regular evaluate

            evaluateResult = new EvaluateResult(setting.Value, setting.VariationId);

            evaluateLog.ReturnValue = evaluateResult.Value.ToString();
            evaluateLog.VariationId = setting.VariationId;

            return evaluateResult;
        }
        finally
        {
            this.logger.SettingEvaluated(evaluateLog);
        }
    }

    private static bool TryEvaluatePercentageRules(PercentageOption[] percentageOptions, in EvaluateContext context, out EvaluateResult result)
    {
        if (percentageOptions.Length > 0)
        {
            var evaluateLog = context.Log;
            var user = context.User!;

            var hashCandidate = context.Key + user.Identifier;

            var hashValue = hashCandidate.Sha1().Substring(0, 7);

            var hashScale = int.Parse(hashValue, NumberStyles.HexNumber) % 100;
            evaluateLog.Log(Invariant($"Applying the % option that matches the User's pseudo-random '{hashScale}' (this value is sticky and consistent across all SDKs):"));

            var bucket = 0;

            foreach (var percentageRule in percentageOptions)
            {
                bucket += percentageRule.Percentage;

                if (hashScale >= bucket)
                {
                    evaluateLog.Log(Invariant($"  - % option: [IF {bucket} > {hashScale} THEN '{percentageRule.Value}'] => no match"));
                    continue;
                }
                evaluateLog.Log(Invariant($"  - % option: [IF {bucket} > {hashScale} THEN '{percentageRule.Value}'] => MATCH, applying % option"));
                result = new EvaluateResult(percentageRule.Value, percentageRule.VariationId, matchedPercentageOption: percentageRule);
                return true;
            }
        }

        result = default;
        return false;
    }

    private static bool TryEvaluateRules(TargetingRule[] targetingRules, in EvaluateContext context, out EvaluateResult result)
    {
        if (targetingRules.Length > 0)
        {
            var evaluateLog = context.Log;
            var user = context.User!;

            evaluateLog.Log(Invariant($"Applying the first targeting rule that matches the User '{user.Serialize()}':"));
            foreach (var targetingRule in targetingRules)
            {
                var rule = targetingRule.Conditions.First().ComparisonCondition ?? throw new InvalidOperationException();

                // TODO: how to handle this?
                if (rule.ComparisonAttribute is null)
                {
                    continue;
                }

                var l = Invariant($"  - rule: [IF User.{rule.ComparisonAttribute} {ToDisplayText(rule.Comparator)} '{rule.GetComparisonValue()}' THEN {targetingRule.SimpleValueOrDefault()}] => ");
                if (!user.AllAttributes.ContainsKey(rule.ComparisonAttribute))
                {
                    evaluateLog.Log(l + "no match");
                    continue;
                }

                var comparisonAttributeValue = user.AllAttributes[rule.ComparisonAttribute]!;
                if (string.IsNullOrEmpty(comparisonAttributeValue))
                {
                    evaluateLog.Log(l + "no match");
                    continue;
                }

                switch (rule.Comparator)
                {
                    case Comparator.Contains:

                        if (rule.StringListValue!.Any(value => comparisonAttributeValue.Contains(value)))
                        {
                            evaluateLog.Log(l + "MATCH, applying rule");

                            result = new EvaluateResult(targetingRule.SimpleValueOrDefault(), targetingRule.SimpleValue?.VariationId, matchedTargetingRule: targetingRule);
                            return true;
                        }

                        evaluateLog.Log(l + "no match");

                        break;
                    case Comparator.NotContains:

                        if (!rule.StringListValue!.Any(value => comparisonAttributeValue.Contains(value)))
                        {
                            evaluateLog.Log(l + "MATCH, applying rule");

                            result = new EvaluateResult(targetingRule.SimpleValueOrDefault(), targetingRule.SimpleValue?.VariationId, matchedTargetingRule: targetingRule);
                            return true;
                        }

                        evaluateLog.Log(l + "no match");

                        break;
                    case Comparator.SemVerOneOf:
                    case Comparator.SemVerNotOneOf:
                    case Comparator.SemVerLessThan:
                    case Comparator.SemVerLessThanEqual:
                    case Comparator.SemVerGreaterThan:
                    case Comparator.SemVerGreaterThanEqual:
                        // TODO: handle value list
                        var stringValue = rule.Comparator is Comparator.SemVerOneOf or Comparator.SemVerNotOneOf
                            ? string.Join(", ", rule.StringListValue!)
                            : rule.StringValue!;

                        if (EvaluateSemVer(comparisonAttributeValue, stringValue, rule.Comparator))
                        {
                            evaluateLog.Log(l + "MATCH, applying rule");

                            result = new EvaluateResult(targetingRule.SimpleValueOrDefault(), targetingRule.SimpleValue?.VariationId, matchedTargetingRule: targetingRule);
                            return true;
                        }

                        evaluateLog.Log(l + "no match");

                        break;

                    case Comparator.NumberEqual:
                    case Comparator.NumberNotEqual:
                    case Comparator.NumberLessThan:
                    case Comparator.NumberLessThanEqual:
                    case Comparator.NumberGreaterThan:
                    case Comparator.NumberGreaterThanEqual:

                        if (EvaluateNumber(comparisonAttributeValue, rule.DoubleValue!.Value, rule.Comparator))
                        {
                            evaluateLog.Log(l + "MATCH, applying rule");

                            result = new EvaluateResult(targetingRule.SimpleValueOrDefault(), targetingRule.SimpleValue?.VariationId, matchedTargetingRule: targetingRule);
                            return true;
                        }

                        evaluateLog.Log(l + "no match");

                        break;
                    case Comparator.SensitiveOneOf:
                        // TODO: handle missing configJsonSalt
                        if (rule.StringListValue!.Contains(HashComparisonAttribute(comparisonAttributeValue, context)))
                        {
                            evaluateLog.Log(l + "MATCH, applying rule");

                            result = new EvaluateResult(targetingRule.SimpleValueOrDefault(), targetingRule.SimpleValue?.VariationId, matchedTargetingRule: targetingRule);
                            return true;
                        }

                        evaluateLog.Log(l + "no match");

                        break;
                    case Comparator.SensitiveNotOneOf:
                        // TODO: handle missing configJsonSalt
                        if (!rule.StringListValue!.Contains(HashComparisonAttribute(comparisonAttributeValue, context)))
                        {
                            evaluateLog.Log(l + "MATCH, applying rule");

                            result = new EvaluateResult(targetingRule.SimpleValueOrDefault(), targetingRule.SimpleValue?.VariationId, matchedTargetingRule: targetingRule);
                            return true;
                        }

                        evaluateLog.Log(l + "no match");

                        break;
                    default:
                        break;
                }
            }
        }

        result = default;
        return false;
    }

    private static bool EvaluateNumber(string s1, double d2, Comparator comparator)
    {
        if (!double.TryParse(s1.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1))
        {
            return false;
        }

        return comparator switch
        {
            Comparator.NumberEqual => d1 == d2,
            Comparator.NumberNotEqual => d1 != d2,
            Comparator.NumberLessThan => d1 < d2,
            Comparator.NumberLessThanEqual => d1 <= d2,
            Comparator.NumberGreaterThan => d1 > d2,
            Comparator.NumberGreaterThanEqual => d1 >= d2,
            _ => false
        };
    }

    private static bool EvaluateSemVer(string s1, string s2, Comparator comparator)
    {
        if (!SemVersion.TryParse(s1?.Trim(), out SemVersion v1, true)) return false;
        s2 = string.IsNullOrWhiteSpace(s2) ? string.Empty : s2.Trim();

        switch (comparator)
        {
            case Comparator.SemVerOneOf:

                var rsvi = s2
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        if (SemVersion.TryParse(s.Trim(), out SemVersion ns, true))
                        {
                            return ns;
                        }

                        return null;
                    })
                    .ToList();

                return !rsvi.Contains(null) && rsvi.Any(v => v!.PrecedenceMatches(v1));

            case Comparator.SemVerNotOneOf:

                var rsvni = s2
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s =>
                    {
                        if (SemVersion.TryParse(s?.Trim(), out SemVersion ns, true))
                        {
                            return ns;
                        }

                        return null;
                    })
                    .ToList();

                return !rsvni.Contains(null) && !rsvni.Any(v => v!.PrecedenceMatches(v1));

            case Comparator.SemVerLessThan:

                if (SemVersion.TryParse(s2, out SemVersion v20, true))
                {
                    return v1.CompareByPrecedence(v20) < 0;
                }

                break;
            case Comparator.SemVerLessThanEqual:

                if (SemVersion.TryParse(s2, out SemVersion v21, true))
                {
                    return v1.CompareByPrecedence(v21) <= 0;
                }

                break;
            case Comparator.SemVerGreaterThan:

                if (SemVersion.TryParse(s2, out SemVersion v22, true))
                {
                    return v1.CompareByPrecedence(v22) > 0;
                }

                break;
            case Comparator.SemVerGreaterThanEqual:

                if (SemVersion.TryParse(s2, out SemVersion v23, true))
                {
                    return v1.CompareByPrecedence(v23) >= 0;
                }

                break;
        }

        return false;
    }

    private static string HashComparisonAttribute(string comparisonValue, in EvaluateContext context)
    {
        return (comparisonValue + context.Setting.ConfigJsonSalt + context.Key).Sha256();
    }

    private static string ToDisplayText(Comparator comparator)
    {
        return comparator switch
        {
            Comparator.Contains => "CONTAINS ANY OF",
            Comparator.NotContains => "NOT CONTAINS ANY OF",
            Comparator.SemVerOneOf => "IS ONE OF (semver)",
            Comparator.SemVerNotOneOf => "IS NOT ONE OF (semver)",
            Comparator.SemVerLessThan => "< (semver)",
            Comparator.SemVerLessThanEqual => "<= (semver)",
            Comparator.SemVerGreaterThan => "> (semver)",
            Comparator.SemVerGreaterThanEqual => ">= (semver)",
            Comparator.NumberEqual => "= (number)",
            Comparator.NumberNotEqual => "!= (number)",
            Comparator.NumberLessThan => "< (number)",
            Comparator.NumberLessThanEqual => "<= (number)",
            Comparator.NumberGreaterThan => "> (number)",
            Comparator.NumberGreaterThanEqual => ">= (number)",
            Comparator.SensitiveOneOf => "IS ONE OF (hashed)",
            Comparator.SensitiveNotOneOf => "IS NOT ONE OF (hashed)",
            Comparator.DateTimeBefore => "BEFORE (UTC datetime)",
            Comparator.DateTimeAfter => "AFTER (UTC datetime)",
            Comparator.SensitiveTextEquals => "EQUALS (hashed)",
            Comparator.SensitiveTextNotEquals => "NOT EQUALS (hashed)",
            Comparator.SensitiveTextStartsWith => "STARTS WITH ANY OF (hashed)",
            Comparator.SensitiveTextNotStartsWith => "NOT STARTS WITH ANY OF (hashed)",
            Comparator.SensitiveTextEndsWith => "ENDS WITH ANY OF (hashed)",
            Comparator.SensitiveTextNotEndsWith => "NOT ENDS WITH ANY OF (hashed)",
            Comparator.SensitiveArrayContains => "ARRAY CONTAINS (hashed)",
            Comparator.SensitiveArrayNotContains => "ARRAY NOT CONTAINS (hashed)",
            _ => InvalidOperatorPlaceholder
        };
    }
}
