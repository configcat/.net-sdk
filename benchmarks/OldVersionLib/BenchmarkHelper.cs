using System.Reflection;
using System.Text.Json;
using ConfigCat.Client.Tests.Helpers;

namespace ConfigCat.Client.Benchmarks.Old;

public static partial class BenchmarkHelper
{
    public class BasicMatrixTestsDescriptor : IMatrixTestDescriptor
    {
        public ConfigLocation ConfigLocation => new ConfigLocation.LocalFile("data", "sample_v5_old.json");
        public string MatrixResultFileName => "testmatrix.csv";
    }

    private static readonly SettingsWithPreferences Config = new SettingsWithPreferences
    {
        Settings =
        {
            ["basicFlag"] = CreateSetting(SettingType.Boolean, JsonDocument.Parse("true").RootElement),
            ["complexFlag"] = CreateSetting(SettingType.String,  JsonDocument.Parse("\"fallback\"").RootElement,
                new[]
                {
                    new RolloutRule
                    {
                        Order = 0,
                        ComparisonAttribute = nameof(User.Identifier),
                        Comparator = Comparator.SensitiveOneOf,
                        ComparisonValue = "68d93aa74a0aa1664f65ad6c0515f24769b15c84,8409e4e5d27a1465165012b03b2606f0e5b08250",
                        Value = JsonDocument.Parse("\"a\"").RootElement,
                    },
                    new RolloutRule
                    {
                        Order = 1,
                        ComparisonAttribute = nameof(User.Email),
                        Comparator = Comparator.Contains,
                        ComparisonValue = "@example.com",
                        Value = JsonDocument.Parse("\"b\"").RootElement,
                    },
                    new RolloutRule
                    {
                        Order = 2,
                        ComparisonAttribute = "Version",
                        Comparator = Comparator.SemVerIn,
                        ComparisonValue = "1.0.0, 2.0.0",
                        Value = JsonDocument.Parse("\"c\"").RootElement,
                    },
                    new RolloutRule
                    {
                        Order = 3,
                        ComparisonAttribute = "Version",
                        Comparator = Comparator.SemVerGreaterThan,
                        ComparisonValue = "3.0.0",
                        Value = JsonDocument.Parse("\"d\"").RootElement,
                    },
                    new RolloutRule
                    {
                        Order = 4,
                        ComparisonAttribute = "Number",
                        Comparator = Comparator.NumberGreaterThan,
                        ComparisonValue = "3.14",
                        Value = JsonDocument.Parse("\"e\"").RootElement,
                    },
                },
                new[]
                {
                    new RolloutPercentageItem
                    {
                        Percentage = 20,
                        Value = JsonDocument.Parse("\"p20\"").RootElement
                    },
                    new RolloutPercentageItem
                    {
                        Percentage = 30,
                        Value = JsonDocument.Parse("\"p30\"").RootElement
                    },
                    new RolloutPercentageItem
                    {
                        Percentage = 50,
                        Value = JsonDocument.Parse("\"p50\"").RootElement
                    },
                })
        }
    };

    private static Setting CreateSetting(SettingType settingType, JsonElement value, RolloutRule[]? targetingRules = null, RolloutPercentageItem[]? percentageOptions = null)
    {
        var setting = new Setting
        {
            SettingType = settingType,
            Value = value,
        };

        SetPrivatePropertyValue(setting, nameof(setting.RolloutRules), targetingRules);
        SetPrivatePropertyValue(setting, nameof(setting.RolloutPercentageItems), percentageOptions);

        return setting;
    }

    private static void SetPrivatePropertyValue(object obj, string propertyName, object? value)
    {
        var type = obj.GetType();
        type.InvokeMember(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, obj, new[] { value });
    }
}
