using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ConfigCat.Client.Tests.Helpers;

namespace ConfigCat.Client.Benchmarks.New;

public static partial class BenchmarkHelper
{
    public class BasicMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        public ConfigLocation ConfigLocation => new ConfigLocation.LocalFile("data", "sample_v5.json");
        public string MatrixResultFileName => "testmatrix.csv";
    }

    private static readonly Config Config = new Func<Config>(() =>
    {
        var config = new Config
        {
            preferences = new Preferences
            {
                Salt = "LKQu1a62agfNnWuGwA8cZglf4x0yZSbY2En7WQn5dWw"
            },
            settings = new Dictionary<string, Setting>()
            {
                ["basicFlag"] = new Setting
                {
                    settingType = SettingType.Boolean,
                    value = new SettingValue { BoolValue = true },
                },
                ["complexFlag"] = new Setting
                {
                    settingType = SettingType.String,
                    targetingRules = new[]
                    {
                        new TargetingRule
                        {
                            conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        comparisonAttribute = nameof(User.Identifier),
                                        comparator = UserComparator.SensitiveTextIsOneOf,
                                        StringListValue = new[]
                                        {
                                            "61418c941ecda8031d08ab86ec821e676fde7b6a59cd16b1e7191503c2f8297d",
                                            "2ebea0310612c4c40d183b0c123d9bd425cf54f1e101f42858e701b5077cba01"
                                        }
                                    }
                                },
                            },
                            SimpleValueOrNull = new SimpleSettingValue { value = new SettingValue { StringValue =  "a" } },
                        },
                        new TargetingRule
                        {
                            conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        comparisonAttribute = nameof(User.Email),
                                        comparator = UserComparator.TextContainsAnyOf,
                                        StringListValue = new[] { "@example.com" }
                                    }
                                },
                            },
                            SimpleValueOrNull = new SimpleSettingValue { value = new SettingValue { StringValue =  "b" } },
                        },
                        new TargetingRule
                        {
                            conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        comparisonAttribute = "Version",
                                        comparator = UserComparator.SemVerIsOneOf,
                                        StringListValue = new[] { "1.0.0", "2.0.0" }
                                    }
                                },
                            },
                            SimpleValueOrNull = new SimpleSettingValue { value = new SettingValue { StringValue =  "c" } },
                        },
                        new TargetingRule
                        {
                            conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        comparisonAttribute = "Version",
                                        comparator = UserComparator.SemVerGreater,
                                        StringValue = "3.0.0"
                                    }
                                },
                            },
                            SimpleValueOrNull = new SimpleSettingValue { value = new SettingValue { StringValue =  "d" } },
                        },
                        new TargetingRule
                        {
                            conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        comparisonAttribute = "Number",
                                        comparator = UserComparator.NumberGreater,
                                        DoubleValue = 3.14
                                    }
                                },
                            },
                            SimpleValueOrNull = new SimpleSettingValue { value = new SettingValue { StringValue =  "e" } },
                        },
                        new TargetingRule
                        {
                            PercentageOptionsOrNull = new[]
                            {
                                new PercentageOption
                                {
                                    percentage = 20,
                                    value = new SettingValue { StringValue =  "p20" }
                                },
                                new PercentageOption
                                {
                                    percentage = 30,
                                    value = new SettingValue { StringValue =  "p30" }
                                },
                                new PercentageOption
                                {
                                    percentage = 50,
                                    value = new SettingValue { StringValue =  "p50" }
                                },
                            }
                        },
                    },
                    value = new SettingValue { StringValue = "fallback" }
                }
            }
        };

        ((IJsonOnDeserialized)config).OnDeserialized();
        return config;
    })();
}
