using ConfigCat.Client.Versioning;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ConfigCat.Client.Security;

namespace ConfigCat.Client.Evaluate
{
    internal class RolloutEvaluator : IRolloutEvaluator
    {
        private readonly ILogger log;
        private readonly IConfigDeserializer configDeserializer;

        public RolloutEvaluator(ILogger logger, IConfigDeserializer configDeserializer)
        {
            this.log = logger;
            this.configDeserializer = configDeserializer;
        }

        public T Evaluate<T>(ProjectConfig projectConfig, string key, T defaultValue, User user = null)
        {
            var result = EvaluateLogic(projectConfig, key, defaultValue?.ToString(), null, user);

            if (result == null)
            {
                return defaultValue;
            }

            return new JValue(result.RawValue).Value<T>();
        }

        public string EvaluateVariationId(ProjectConfig projectConfig, string key, string defaultVariationId, User user = null)
        {
            var result = EvaluateLogic(projectConfig, key, null, defaultVariationId, user);

            if (result == null)
            {
                return defaultVariationId;
            }

            return result.VariationId;
        }

        private EvaluateResult EvaluateLogic(ProjectConfig projectConfig, string key, string logDefaultValue, string logDefaultVariationId, User user = null)
        {
            if (!this.configDeserializer.TryDeserialize(projectConfig.JsonString, out var deserialized))
            {
                this.log.Warning("Config deserialization failed, returning defaultValue");

                return null;
            }

            var settings = deserialized.Settings;
            if (!settings.TryGetValue(key, out var setting))
            {
                var keys = string.Join(",", settings.Keys.Select(s => $"'{s}'").ToArray());

                this.log.Error($"Evaluating '{key}' failed. Returning default value: '{logDefaultValue}'. Here are the available keys: {keys}.");

                return null;
            }

            var evaluateLog = new EvaluateLogger<string>
            {
                ReturnValue = logDefaultValue,
                User = user,
                KeyName = key,
                VariationId = logDefaultVariationId
            };

            try
            {
                EvaluateResult result = null;

                if (user != null)
                {
                    // evaluate rules

                    if (TryEvaluateRules(setting.RolloutRules, user, evaluateLog, out result))
                    {
                        evaluateLog.ReturnValue = result.RawValue;
                        evaluateLog.VariationId = result.VariationId;

                        return result;
                    }

                    // evaluate variations

                    if (TryEvaluateVariations(setting.RolloutPercentageItems, key, user, out result))
                    {
                        evaluateLog.Log("evaluate % option => user targeted");
                        evaluateLog.ReturnValue = result.RawValue;
                        evaluateLog.VariationId = result.VariationId;

                        return result;
                    }
                    else
                    {
                        evaluateLog.Log("evaluate % option => user not targeted");
                    }
                }
                else if (user == null && (setting.RolloutRules.Any() || setting.RolloutPercentageItems.Any()))
                {
                    this.log.Warning($"Evaluating '{key}'. UserObject missing! You should pass a UserObject to GetValue() or GetValueAsync(), in order to make targeting work properly. Read more: https://configcat.com/docs/advanced/user-object");
                }

                // regular evaluate

                result = new EvaluateResult(setting.RawValue, setting.VariationId);

                evaluateLog.ReturnValue = result.RawValue;
                evaluateLog.VariationId = result.VariationId;

                return result;
            }
            finally
            {
                this.log.Information(evaluateLog.ToString());
            }
        }

        private bool TryEvaluateVariations(ICollection<RolloutPercentageItem> rolloutPercentageItems, string key, User user, out EvaluateResult result)
        {
            result = new EvaluateResult();

            if (rolloutPercentageItems != null && rolloutPercentageItems.Count > 0)
            {
                var hashCandidate = key + user.Identifier;

                var hashValue = HashUtils.HashString(hashCandidate).Substring(0, 7);

                var hashScale = int.Parse(hashValue, NumberStyles.HexNumber) % 100;

                var bucket = 0;

                foreach (var variation in rolloutPercentageItems.OrderBy(o => o.Order))
                {
                    bucket += variation.Percentage;

                    if (hashScale < bucket)
                    {
                        result.RawValue = variation.RawValue;
                        result.VariationId = variation.VariationId;

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryEvaluateRules<T>(ICollection<RolloutRule> rules, User user, EvaluateLogger<T> logger, out EvaluateResult result)
        {
            result = new EvaluateResult();

            if (rules != null && rules.Count > 0)
            {
                foreach (var rule in rules.OrderBy(o => o.Order))
                {
                    result.RawValue = rule.RawValue;
                    result.VariationId = rule.VariationId;

                    if (!user.AllAttributes.ContainsKey(rule.ComparisonAttribute))
                    {
                        continue;
                    }

                    var comparisonAttributeValue = user.AllAttributes[rule.ComparisonAttribute];
                    if (string.IsNullOrEmpty(comparisonAttributeValue))
                    {
                        continue;
                    }

                    string l = $"evaluate rule: '{comparisonAttributeValue}' {EvaluateLogger<T>.FormatComparator(rule.Comparator)} '{rule.ComparisonValue}' => ";

                    switch (rule.Comparator)
                    {
                        case ComparatorEnum.In:

                            if (rule.ComparisonValue
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Contains(comparisonAttributeValue))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;

                        case ComparatorEnum.NotIn:

                            if (!rule.ComparisonValue
                               .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(t => t.Trim())
                               .Contains(comparisonAttributeValue))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;
                        case ComparatorEnum.Contains:

                            if (comparisonAttributeValue.Contains(rule.ComparisonValue))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;
                        case ComparatorEnum.NotContains:

                            if (!comparisonAttributeValue.Contains(rule.ComparisonValue))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;
                        case ComparatorEnum.SemVerIn:
                        case ComparatorEnum.SemVerNotIn:
                        case ComparatorEnum.SemVerLessThan:
                        case ComparatorEnum.SemVerLessThanEqual:
                        case ComparatorEnum.SemVerGreaterThan:
                        case ComparatorEnum.SemVerGreaterThanEqual:

                            if (EvaluateSemVer(comparisonAttributeValue, rule.ComparisonValue, rule.Comparator))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;

                        case ComparatorEnum.NumberEqual:
                        case ComparatorEnum.NumberNotEqual:
                        case ComparatorEnum.NumberLessThan:
                        case ComparatorEnum.NumberLessThanEqual:
                        case ComparatorEnum.NumberGreaterThan:
                        case ComparatorEnum.NumberGreaterThanEqual:

                            if (EvaluateNumber(comparisonAttributeValue, rule.ComparisonValue, rule.Comparator))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;
                        case ComparatorEnum.SensitiveOneOf:
                            if (rule.ComparisonValue
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Contains(HashUtils.HashString(comparisonAttributeValue)))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;
                        case ComparatorEnum.SensitiveNotOneOf:
                            if (!rule.ComparisonValue
                               .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(t => t.Trim())
                               .Contains(HashUtils.HashString(comparisonAttributeValue)))
                            {
                                logger.Log(l + "match");

                                return true;
                            }

                            logger.Log(l + "no match");

                            break;
                        default:
                            break;
                    }
                }
            }

            return false;
        }

        private static bool EvaluateNumber(string s1, string s2, ComparatorEnum comparator)
        {
            if (!double.TryParse(s1.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d1)
                || !double.TryParse(s2.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d2))
            {
                return false;
            }

            switch (comparator)
            {
                case ComparatorEnum.NumberEqual:

                    return d1 == d2;

                case ComparatorEnum.NumberNotEqual:

                    return d1 != d2;

                case ComparatorEnum.NumberLessThan:

                    return d1 < d2;

                case ComparatorEnum.NumberLessThanEqual:

                    return d1 <= d2;

                case ComparatorEnum.NumberGreaterThan:

                    return d1 > d2;

                case ComparatorEnum.NumberGreaterThanEqual:

                    return d1 >= d2;

                default:
                    break;
            }

            return false;
        }

        private static bool EvaluateSemVer(string s1, string s2, ComparatorEnum comparator)
        {
            if (SemVersion.TryParse(s1?.Trim(), out SemVersion v1, true))
            {
                s2 = string.IsNullOrWhiteSpace(s2) ? string.Empty : s2.Trim();

                switch (comparator)
                {
                    case ComparatorEnum.SemVerIn:

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

                        return !rsvi.Contains(null) && rsvi.Any(v => v.PrecedenceMatches(v1));

                    case ComparatorEnum.SemVerNotIn:

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

                        return !rsvni.Contains(null) && !rsvni.Any(v => v.PrecedenceMatches(v1));

                    case ComparatorEnum.SemVerLessThan:

                        if (SemVersion.TryParse(s2, out SemVersion v20, true))
                        {
                            return v1.CompareByPrecedence(v20) < 0;
                        }

                        break;
                    case ComparatorEnum.SemVerLessThanEqual:

                        if (SemVersion.TryParse(s2, out SemVersion v21, true))
                        {
                            return v1.CompareByPrecedence(v21) <= 0;
                        }

                        break;
                    case ComparatorEnum.SemVerGreaterThan:

                        if (SemVersion.TryParse(s2, out SemVersion v22, true))
                        {
                            return v1.CompareByPrecedence(v22) > 0;
                        }

                        break;
                    case ComparatorEnum.SemVerGreaterThanEqual:

                        if (SemVersion.TryParse(s2, out SemVersion v23, true))
                        {
                            return v1.CompareByPrecedence(v23) >= 0;
                        }

                        break;
                    default:
                        break;
                }
            }

            return false;
        }
    }
}