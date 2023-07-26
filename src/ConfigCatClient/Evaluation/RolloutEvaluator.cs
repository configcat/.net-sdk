using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConfigCat.Client.Versioning;

using static System.FormattableString;

namespace ConfigCat.Client.Evaluation;

internal sealed class RolloutEvaluator : IRolloutEvaluator
{
    private readonly LoggerWrapper logger;

    public RolloutEvaluator(LoggerWrapper logger)
    {
        this.logger = logger;
    }

    public EvaluateResult Evaluate(Setting setting, string key, string? logDefaultValue, User? user)
    {
        var evaluateLog = new EvaluateLogger
        {
            ReturnValue = logDefaultValue,
            User = user,
            KeyName = key,
            VariationId = null
        };

        try
        {
            EvaluateResult evaluateResult;

            if (user is not null)
            {
                // evaluate targeting rules

                if (TryEvaluateRules(setting.RolloutRules, user, evaluateLog, out evaluateResult))
                {
                    evaluateLog.ReturnValue = evaluateResult.Value.ToString();
                    evaluateLog.VariationId = evaluateResult.VariationId;

                    return evaluateResult;
                }

                // evaluate percentage options

                if (TryEvaluatePercentageRules(setting.RolloutPercentageItems, key, user, evaluateLog, out evaluateResult))
                {
                    evaluateLog.ReturnValue = evaluateResult.Value.ToString();
                    evaluateLog.VariationId = evaluateResult.VariationId;

                    return evaluateResult;
                }
            }
            else if (setting.RolloutRules.Any() || setting.RolloutPercentageItems.Any())
            {
                this.logger.TargetingIsNotPossible(key);
            }

            // regular evaluate

            evaluateLog.ReturnValue = setting.Value.ToString();
            evaluateLog.VariationId = setting.VariationId;

            evaluateResult = new EvaluateResult(setting.Value, setting.VariationId);
            return evaluateResult;
        }
        finally
        {
            this.logger.SettingEvaluated(evaluateLog);
        }
    }

    private static bool TryEvaluatePercentageRules(ICollection<RolloutPercentageItem> rolloutPercentageItems, string key, User user, EvaluateLogger evaluateLog, out EvaluateResult result)
    {
        if (rolloutPercentageItems.Count > 0)
        {
            var hashCandidate = key + user.Identifier;

            var hashValue = hashCandidate.Hash().Substring(0, 7);

            var hashScale = int.Parse(hashValue, NumberStyles.HexNumber) % 100;
            evaluateLog.Log(Invariant($"Applying the % option that matches the User's pseudo-random '{hashScale}' (this value is sticky and consistent across all SDKs):"));

            var bucket = 0;

            foreach (var percentageRule in rolloutPercentageItems.OrderBy(o => o.Order))
            {
                bucket += percentageRule.Percentage;

                if (hashScale >= bucket)
                {
                    evaluateLog.Log(Invariant($"  - % option: [IF {bucket} > {hashScale} THEN '{percentageRule.Value}'] => no match"));
                    continue;
                }
                result = new EvaluateResult(percentageRule.Value, percentageRule.VariationId, matchedPercentageOption: percentageRule);
                evaluateLog.Log(Invariant($"  - % option: [IF {bucket} > {hashScale} THEN '{percentageRule.Value}'] => MATCH, applying % option"));
                return true;
            }
        }

        result = default;
        return false;
    }

    private static bool TryEvaluateRules(ICollection<RolloutRule> rules, User user, EvaluateLogger logger, out EvaluateResult result)
    {
        if (rules.Count > 0)
        {
            logger.Log(Invariant($"Applying the first targeting rule that matches the User '{user.Serialize()}':"));
            foreach (var rule in rules.OrderBy(o => o.Order))
            {
                result = new EvaluateResult(rule.Value, rule.VariationId, matchedTargetingRule: rule);

                var l = Invariant($"  - rule: [IF User.{rule.ComparisonAttribute} {RolloutRule.FormatComparator(rule.Comparator)} '{rule.ComparisonValue}' THEN {rule.Value}] => ");
                if (!user.AllAttributes.ContainsKey(rule.ComparisonAttribute))
                {
                    logger.Log(l + "no match");
                    continue;
                }

                var comparisonAttributeValue = user.AllAttributes[rule.ComparisonAttribute]!;
                if (string.IsNullOrEmpty(comparisonAttributeValue))
                {
                    logger.Log(l + "no match");
                    continue;
                }

                switch (rule.Comparator)
                {
                    case Comparator.In:

                        if (rule.ComparisonValue
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Contains(comparisonAttributeValue))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;

                    case Comparator.NotIn:

                        if (!rule.ComparisonValue
                           .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim())
                           .Contains(comparisonAttributeValue))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;
                    case Comparator.Contains:

                        if (comparisonAttributeValue.Contains(rule.ComparisonValue))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;
                    case Comparator.NotContains:

                        if (!comparisonAttributeValue.Contains(rule.ComparisonValue))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;
                    case Comparator.SemVerIn:
                    case Comparator.SemVerNotIn:
                    case Comparator.SemVerLessThan:
                    case Comparator.SemVerLessThanEqual:
                    case Comparator.SemVerGreaterThan:
                    case Comparator.SemVerGreaterThanEqual:

                        if (EvaluateSemVer(comparisonAttributeValue, rule.ComparisonValue, rule.Comparator))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;

                    case Comparator.NumberEqual:
                    case Comparator.NumberNotEqual:
                    case Comparator.NumberLessThan:
                    case Comparator.NumberLessThanEqual:
                    case Comparator.NumberGreaterThan:
                    case Comparator.NumberGreaterThanEqual:

                        if (EvaluateNumber(comparisonAttributeValue, rule.ComparisonValue, rule.Comparator))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;
                    case Comparator.SensitiveOneOf:
                        if (rule.ComparisonValue
                            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Contains(comparisonAttributeValue.Hash()))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;
                    case Comparator.SensitiveNotOneOf:
                        if (!rule.ComparisonValue
                           .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim())
                           .Contains(comparisonAttributeValue.Hash()))
                        {
                            logger.Log(l + "MATCH, applying rule");

                            return true;
                        }

                        logger.Log(l + "no match");

                        break;
                    default:
                        break;
                }
            }
        }

        result = default;
        return false;
    }

    private static bool EvaluateNumber(string s1, string s2, Comparator comparator)
    {
        if (!double.TryParse(s1.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1)
            || !double.TryParse(s2.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var d2))
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
            case Comparator.SemVerIn:

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

            case Comparator.SemVerNotIn:

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
}
