using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace ConfigCat.Client.Evaluate
{
    internal class RolloutEvaluator : IRolloutEvaluator
    {
        private ILogger log;

        public RolloutEvaluator(ILogger logger)
        {
            this.log = logger;
        }

        public T Evaluate<T>(ProjectConfig projectConfig, string key, T defaultValue, User user = null)
        {
            if (projectConfig.JsonString == null)
            {
                this.log.Warning("ConfigJson is not present, returning defaultValue");

                return defaultValue;
            }

            var json = (JObject)JsonConvert.DeserializeObject(projectConfig.JsonString);

            var setting = (Setting)json.GetValue(key)?.ToObject(typeof(Setting));

            if (setting == null)
            {
                this.log.Warning($"Unknown key: '{key}'");

                return defaultValue;
            }

            if (user != null)
            {
                // evaluate rules

                T result;

                if (TryEvaluateRules(setting.RolloutRules, user, out result))
                {
                    return result;
                }

                // evaluate variations

                if (TryEvaluateVariations(setting.RolloutPercentageItems, key, user, out result))
                {
                    return result;
                }
            }

            // regular evaluate

            return new JValue(setting.Value).Value<T>();
        }

        private bool TryEvaluateVariations<T>(ICollection<RolloutPercentageItem> rolloutPercentageItems, string key, User user, out T result)
        {
            result = default(T);

            if (rolloutPercentageItems != null && rolloutPercentageItems.Count > 0)
            {
                var hashCandidate = key + user.Identifier;

                var hashValue = HashString(hashCandidate).Substring(0, 7);

                var hashScale = int.Parse(hashValue, NumberStyles.HexNumber) % 100;

                var bucket = 0;

                foreach (var variation in rolloutPercentageItems.OrderBy(o => o.Order))
                {
                    bucket += variation.Percentage;                    

                    if (hashScale < bucket)
                    {
                        result = new JValue(variation.RawValue).Value<T>();

                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryEvaluateRules<T>(ICollection<RolloutRule> rules, User user, out T result)
        {
            result = default(T);

            if (rules != null && rules.Count > 0)
            {
                foreach (var rule in rules.OrderBy(o => o.Order))
                {
                    result = new JValue(rule.RawValue).Value<T>();

                    if (!user.AllAttributes.ContainsKey(rule.ComparisonAttribute.ToLowerInvariant()))
                    {
                        continue;
                    }

                    var comparisonAttributeValue = user.AllAttributes[rule.ComparisonAttribute.ToLowerInvariant()];
                    if (string.IsNullOrEmpty(comparisonAttributeValue))
                    {
                        continue;
                    }

                    switch (rule.Comparator)
                    {
                        case ComparatorEnum.In:

                            if (rule.ComparisonValue
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .Contains(comparisonAttributeValue))
                            {
                                return true;
                            }

                            break;

                        case ComparatorEnum.NotIn:

                            if (!rule.ComparisonValue
                               .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(t => t.Trim())
                               .Contains(comparisonAttributeValue))
                            {
                                return true;
                            }

                            break;
                        case ComparatorEnum.Contains:

                            if (comparisonAttributeValue.Contains(rule.ComparisonValue))
                            {
                                return true;
                            }

                            break;
                        case ComparatorEnum.NotContains:

                            if (!comparisonAttributeValue.Contains(rule.ComparisonValue))
                            {
                                return true;
                            }

                            break;
                        default:
                            break;
                    }
                }
            }

            return false;
        }

        private static string HashString(string s)
        {
            using (var hash = SHA1.Create())
            {
                var hashedBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(s));

                var result = new StringBuilder();

                foreach (byte t in hashedBytes)
                {
                    result.Append(t.ToString("x2"));
                }

                return result.ToString();
            }
        }
    }
}