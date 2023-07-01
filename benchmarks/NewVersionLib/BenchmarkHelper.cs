using System;

namespace ConfigCat.Client.Benchmarks.New;

public static partial class BenchmarkHelper
{
    public class BasicMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        public string SampleJsonFileName => "sample_v5.json";
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
                                new ConditionWrapper
                                {
                                    ComparisonCondition =  new ComparisonCondition()
                                    {
                                        ComparisonAttribute = nameof(User.Identifier),
                                        Comparator = Comparator.SensitiveOneOf,
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
                                new ConditionWrapper
                                {
                                    ComparisonCondition =  new ComparisonCondition()
                                    {
                                        ComparisonAttribute = nameof(User.Email),
                                        Comparator = Comparator.Contains,
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
                                new ConditionWrapper
                                {
                                    ComparisonCondition =  new ComparisonCondition()
                                    {
                                        ComparisonAttribute = "Version",
                                        Comparator = Comparator.SemVerOneOf,
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
                                new ConditionWrapper
                                {
                                    ComparisonCondition =  new ComparisonCondition()
                                    {
                                        ComparisonAttribute = "Version",
                                        Comparator = Comparator.SemVerGreaterThan,
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
                                new ConditionWrapper
                                {
                                    ComparisonCondition =  new ComparisonCondition()
                                    {
                                        ComparisonAttribute = "Number",
                                        Comparator = Comparator.NumberGreaterThan,
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
