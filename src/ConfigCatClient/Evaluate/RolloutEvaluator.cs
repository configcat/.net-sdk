using ConfigCat.Client.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace ConfigCat.Client.Evaluate
{
    internal class RolloutEvaluator : IRolloutEvaluator
    {
        private readonly ILogger log;

        public RolloutEvaluator(ILogger logger)
        {
            this.log = logger;
        }

        public T Evaluate<T>(IDictionary<string, Setting> settings, string key, T defaultValue, User user = null)
        {
            if (settings.Count == 0)
            {
                this.log.Error($"Config JSON is not present. Returning the defaultValue defined in the app source code: '{defaultValue}'.");
                return defaultValue;
            }

            var result = EvaluateLogic(settings, key, defaultValue?.ToString(), null, user);
#if USE_NEWTONSOFT_JSON
            return result == null ? defaultValue : result.Value.Value<T>();
#else
            return result == null ? defaultValue : result.Value.Deserialize<T>();
#endif
        }

        public object Evaluate(IDictionary<string, Setting> settings, string key, object defaultValue, User user = null)
        {
            if (settings.Count == 0)
            {
                this.log.Error($"Config JSON is not present. Returning the defaultValue defined in the app source code: '{defaultValue}'.");
                return defaultValue;
            }

            var result = EvaluateLogic(settings, key, defaultValue?.ToString(), null, user);
#if USE_NEWTONSOFT_JSON
            return result == null ? defaultValue : result.Value.ToObject(DetermineTypeFromSettingType(result.SettingType));
#else
            return result == null ? defaultValue : result.Value.Deserialize(DetermineTypeFromSettingType(result.SettingType));
#endif
        }

        public string EvaluateVariationId(IDictionary<string, Setting> settings, string key, string defaultVariationId, User user = null)
        {
            if (settings.Count == 0)
            {
                this.log.Error($"Config JSON is not present. Returning defaultVariationId: '{defaultVariationId}'.");
                return defaultVariationId;
            }

            var result = EvaluateLogic(settings, key, null, defaultVariationId, user);
            return result == null ? defaultVariationId : result.VariationId;
        }

        private EvaluateResult EvaluateLogic(IDictionary<string, Setting> settings, string key, string logDefaultValue, string logDefaultVariationId, User user = null)
        {
            if (!settings.TryGetValue(key, out var setting))
            {
                var keys = string.Join(",", settings.Keys.Select(s => $"'{s}'").ToArray());
                this.log.Error($"Evaluating '{key}' failed (key not found in ConfigCat). Returning the defaultValue defined in the app source code: '{logDefaultValue}'. Here are the available keys: {keys}.");
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
                EvaluateResult result;

                if (user != null)
                {
                    // evaluate rules

                    if (TryEvaluateRules(setting.RolloutRules, user, evaluateLog, out result))
                    {
                        evaluateLog.ReturnValue = result.Value.ToString();
                        evaluateLog.VariationId = result.VariationId;

                        result.SettingType = setting.SettingType;
                        return result;
                    }

                    // evaluate variations

                    if (TryEvaluateVariations(setting.RolloutPercentageItems, key, user, evaluateLog, out result))
                    {
                        evaluateLog.ReturnValue = result.Value.ToString();
                        evaluateLog.VariationId = result.VariationId;
                        result.SettingType = setting.SettingType;
                        return result;
                    }
                }
                else if (setting.RolloutRules.Any() || setting.RolloutPercentageItems.Any())
                {
                    this.log.Warning($"Cannot evaluate targeting rules and % options for '{key}' (UserObject mising). You should pass a UserObject to GetValue() or GetValueAsync(), in order to make targeting work properly. Read more: https://configcat.com/docs/advanced/user-object");
                }

                // regular evaluate

                result = new EvaluateResult
                {
                    Value = setting.Value,
                    VariationId = setting.VariationId,
                    SettingType = setting.SettingType,
                };

                evaluateLog.ReturnValue = result.Value.ToString();
                evaluateLog.VariationId = result.VariationId;

                return result;
            }
            finally
            {
                this.log.Information($"{evaluateLog}");
            }
        }

        private static bool TryEvaluateVariations<T>(ICollection<RolloutPercentageItem> rolloutPercentageItems, string key, User user, 
            EvaluateLogger<T> evaluateLog, out EvaluateResult result)
        {
            result = new EvaluateResult();

            if (rolloutPercentageItems is { Count: > 0 })
            {
                var hashCandidate = key + user.Identifier;

                var hashValue = hashCandidate.Hash().Substring(0, 7);

                var hashScale = int.Parse(hashValue, NumberStyles.HexNumber) % 100;
                evaluateLog.Log($"Applying the % option that matches the User's pseudo-random '{hashScale}' (this value is sticky and consitent across all SDKs):");

                var bucket = 0;

                foreach (var variation in rolloutPercentageItems.OrderBy(o => o.Order))
                {
                    bucket += variation.Percentage;

                    if (hashScale >= bucket)
                    {
                        evaluateLog.Log($"- % option: [IF {bucket} >= {hashScale} THEN '{variation.Value}'] => no match");
                        continue;
                    }
                    result.Value = variation.Value;
                    result.VariationId = variation.VariationId;
                    evaluateLog.Log($"- % option: [IF {bucket} >= {hashScale} THEN '{variation.Value}'] => MATCH, applying % option");
                    return true;
                }
            }

            return false;
        }

        private static bool TryEvaluateRules<T>(ICollection<RolloutRule> rules, User user, EvaluateLogger<T> logger, out EvaluateResult result)
        {
            result = new EvaluateResult();

            if (rules is { Count: > 0 })
            {
                logger.Log($"Applying the first targeting rule that matches the User '{user.Serialize()}':");
                foreach (var rule in rules.OrderBy(o => o.Order))
                {
                    result.Value = rule.Value;
                    result.VariationId = rule.VariationId;

                    string l = $"- rule: [IF User.{rule.ComparisonAttribute} {EvaluateLogger<T>.FormatComparator(rule.Comparator)} '{rule.ComparisonValue}' THEN {rule.Value}] => ";
                    if (!user.AllAttributes.ContainsKey(rule.ComparisonAttribute))
                    {
                        logger.Log(l + "no match");
                        continue;
                    }

                    var comparisonAttributeValue = user.AllAttributes[rule.ComparisonAttribute];
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

            return false;
        }

        private static bool EvaluateNumber(string s1, string s2, Comparator comparator)
        {
            if (!double.TryParse(s1.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d1)
                || !double.TryParse(s2.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double d2))
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

                    return !rsvi.Contains(null) && rsvi.Any(v => v.PrecedenceMatches(v1));

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

                    return !rsvni.Contains(null) && !rsvni.Any(v => v.PrecedenceMatches(v1));

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

        private static Type DetermineTypeFromSettingType(SettingType settingType) =>
            settingType switch
            {
                SettingType.Boolean => typeof(bool),
                SettingType.String => typeof(string),
                SettingType.Int => typeof(int),
                SettingType.Double => typeof(double),
                _ => throw new ArgumentOutOfRangeException(nameof(settingType), settingType, null)
            };
    }
}