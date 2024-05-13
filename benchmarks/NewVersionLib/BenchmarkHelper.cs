using System;
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
            Preferences = new Preferences
            {
                Salt = "LKQu1a62agfNnWuGwA8cZglf4x0yZSbY2En7WQn5dWw"
            },
            Settings =
            {
                ["basicFlag"] = new Setting
                {
                    SettingType = SettingType.Boolean,
                    Value = new SettingValue { BoolValue = true },
                },
                ["complexFlag"] = new Setting
                {
                    SettingType = SettingType.String,
                    TargetingRules = new[]
                    {
                        new TargetingRule
                        {
                            Conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        ComparisonAttribute = nameof(User.Identifier),
                                        Comparator = UserComparator.SensitiveTextIsOneOf,
                                        StringListValue = new[]
                                        {
                                            "61418c941ecda8031d08ab86ec821e676fde7b6a59cd16b1e7191503c2f8297d",
                                            "2ebea0310612c4c40d183b0c123d9bd425cf54f1e101f42858e701b5077cba01"
                                        }
                                    }
                                },
                            },
                            SimpleValue = new SimpleSettingValue { Value = new SettingValue { StringValue =  "a" } },
                        },
                        new TargetingRule
                        {
                            Conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        ComparisonAttribute = nameof(User.Email),
                                        Comparator = UserComparator.TextContainsAnyOf,
                                        StringListValue = new[] { "@example.com" }
                                    }
                                },
                            },
                            SimpleValue = new SimpleSettingValue { Value = new SettingValue { StringValue =  "b" } },
                        },
                        new TargetingRule
                        {
                            Conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        ComparisonAttribute = "Version",
                                        Comparator = UserComparator.SemVerIsOneOf,
                                        StringListValue = new[] { "1.0.0", "2.0.0" }
                                    }
                                },
                            },
                            SimpleValue = new SimpleSettingValue { Value = new SettingValue { StringValue =  "c" } },
                        },
                        new TargetingRule
                        {
                            Conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        ComparisonAttribute = "Version",
                                        Comparator = UserComparator.SemVerGreater,
                                        StringValue = "3.0.0"
                                    }
                                },
                            },
                            SimpleValue = new SimpleSettingValue { Value = new SettingValue { StringValue =  "d" } },
                        },
                        new TargetingRule
                        {
                            Conditions = new[]
                            {
                                new ConditionContainer
                                {
                                    UserCondition =  new UserCondition()
                                    {
                                        ComparisonAttribute = "Number",
                                        Comparator = UserComparator.NumberGreater,
                                        DoubleValue = 3.14
                                    }
                                },
                            },
                            SimpleValue = new SimpleSettingValue { Value = new SettingValue { StringValue =  "e" } },
                        },
                        new TargetingRule
                        {
                            PercentageOptions = new[]
                            {
                                new PercentageOption
                                {
                                    Percentage = 20,
                                    Value = new SettingValue { StringValue =  "p20" }
                                },
                                new PercentageOption
                                {
                                    Percentage = 30,
                                    Value = new SettingValue { StringValue =  "p30" }
                                },
                                new PercentageOption
                                {
                                    Percentage = 50,
                                    Value = new SettingValue { StringValue =  "p50" }
                                },
                            }
                        },
                    },
                    Value = new SettingValue { StringValue =  "fallback" }
                }
            }
        };

        config.OnDeserialized();
        return config;
    })();
}
