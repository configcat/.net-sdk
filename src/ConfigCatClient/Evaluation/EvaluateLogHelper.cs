using ConfigCat.Client.Utils;
using System;
using System.Globalization;

namespace ConfigCat.Client.Evaluation;

internal static class EvaluateLogHelper
{
    public const string InvalidItemPlaceholder = "<invalid item>";
    public const string InvalidNamePlaceholder = "<invalid name>";
    public const string InvalidOperatorPlaceholder = "<invalid operator>";
    public const string InvalidReferencePlaceholder = "<invalid reference>";
    public const string InvalidValuePlaceholder = "<invalid value>";

    internal const int StringListMaxLength = 10;

    public static IndentedTextBuilder AppendEvaluationResult(this IndentedTextBuilder builder, bool result)
    {
        return builder.Append(result ? "true" : "false");
    }

    private static IndentedTextBuilder AppendUserCondition(this IndentedTextBuilder builder, string? comparisonAttribute, UserComparator comparator, object? comparisonValue)
    {
        return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue ?? InvalidValuePlaceholder}'");
    }

    private static IndentedTextBuilder AppendUserCondition(this IndentedTextBuilder builder, string? comparisonAttribute, UserComparator comparator, string? comparisonValue, bool isSensitive = false)
    {
        return builder.AppendUserCondition(comparisonAttribute, comparator, !isSensitive ? (object?)comparisonValue : "<hashed value>");
    }

    private static IndentedTextBuilder AppendUserCondition(this IndentedTextBuilder builder, string? comparisonAttribute, UserComparator comparator, string[]? comparisonValue, bool isSensitive = false)
    {
        if (comparisonValue is null)
        {
            return builder.AppendUserCondition(comparisonAttribute, comparator, (object?)null);
        }

        const string valueText = "value", valuesText = "values";

        if (isSensitive)
        {
            return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} [<{comparisonValue.Length} hashed {(comparisonValue.Length == 1 ? valueText : valuesText)}>]");
        }
        else
        {
            var comparisonValueFormatter = new StringListFormatter(comparisonValue, StringListMaxLength, getOmittedItemsText: static count =>
                $", ... <{count.ToString(CultureInfo.InvariantCulture)} more {(count == 1 ? valueText : valuesText)}>");

            return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} [{comparisonValueFormatter}]");
        }
    }

    private static IndentedTextBuilder AppendUserCondition(this IndentedTextBuilder builder, string? comparisonAttribute, UserComparator comparator, double? comparisonValue, bool isDateTime = false)
    {
        if (comparisonValue is null)
        {
            return builder.AppendUserCondition(comparisonAttribute, comparator, (object?)null);
        }

        return isDateTime && DateTimeUtils.TryConvertFromUnixTimeSeconds(comparisonValue.Value, out var dateTime)
            ? builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue.Value}' ({dateTime:yyyy-MM-dd'T'HH:mm:ss.fffK} UTC)")
            : builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue.Value}'");
    }

    public static IndentedTextBuilder AppendUserCondition(this IndentedTextBuilder builder, UserCondition condition)
    {
        return condition.Comparator switch
        {
            UserComparator.Contains or
            UserComparator.NotContains or
            UserComparator.SemVerOneOf or
            UserComparator.SemVerNotOneOf =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringListValue),

            UserComparator.SemVerLessThan or
            UserComparator.SemVerLessThanEqual or
            UserComparator.SemVerGreaterThan or
            UserComparator.SemVerGreaterThanEqual =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringValue),

            UserComparator.NumberEqual or
            UserComparator.NumberNotEqual or
            UserComparator.NumberLessThan or
            UserComparator.NumberLessThanEqual or
            UserComparator.NumberGreaterThan or
            UserComparator.NumberGreaterThanEqual =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.DoubleValue),

            UserComparator.SensitiveOneOf or
            UserComparator.SensitiveNotOneOf or
            UserComparator.SensitiveTextStartsWith or
            UserComparator.SensitiveTextNotStartsWith or
            UserComparator.SensitiveTextEndsWith or
            UserComparator.SensitiveTextNotEndsWith or
            UserComparator.SensitiveArrayContains or
            UserComparator.SensitiveArrayNotContains =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringListValue, isSensitive: true),

            UserComparator.DateTimeBefore or
            UserComparator.DateTimeAfter =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.DoubleValue, isDateTime: true),

            UserComparator.SensitiveTextEquals or
            UserComparator.SensitiveTextNotEquals =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringValue, isSensitive: true),

            _ =>
                builder.AppendUserCondition(condition.ComparisonAttribute, condition.Comparator, condition.GetComparisonValue(throwIfInvalid: false)),
        };
    }

    public static IndentedTextBuilder AppendPrerequisiteFlagCondition(this IndentedTextBuilder builder, PrerequisiteFlagCondition condition)
    {
        var prerequisiteFlagKey = condition.PrerequisiteFlagKey;
        var comparator = condition.Comparator;
        var comparisonValue = condition.ComparisonValue.GetValue(throwIfInvalid: false);

        return builder.Append($"Flag '{prerequisiteFlagKey}' {comparator.ToDisplayText()} '{comparisonValue ?? InvalidValuePlaceholder}'");
    }

    public static IndentedTextBuilder AppendSegmentCondition(this IndentedTextBuilder builder, SegmentCondition condition)
    {
        var segment = condition.Segment;
        var comparator = condition.Comparator;

        var segmentName = segment?.Name ??
            (segment is null ? InvalidReferencePlaceholder : InvalidNamePlaceholder);

        return builder.Append($"User {comparator.ToDisplayText()} '{segmentName}'");
    }

    public static IndentedTextBuilder AppendConditionConsequence(this IndentedTextBuilder builder, bool result)
    {
        builder.Append(" => ").AppendEvaluationResult(result);
        return result ? builder : builder.Append(", skipping the remaining AND conditions");
    }

    private static IndentedTextBuilder AppendConditions<TCondition>(this IndentedTextBuilder builder, TCondition[] conditions)
        where TCondition : IConditionProvider
    {
        for (var i = 0; i < conditions.Length; i++)
        {
            builder.IncreaseIndent();

            if (i > 0)
            {
                builder.NewLine("AND ");
            }

            _ = conditions[i].GetCondition(throwIfInvalid: false) switch
            {
                UserCondition userCondition => builder.AppendUserCondition(userCondition),
                PrerequisiteFlagCondition prerequisiteFlagCondition => builder.AppendPrerequisiteFlagCondition(prerequisiteFlagCondition),
                SegmentCondition segmentCondition => builder.AppendSegmentCondition(segmentCondition),
                _ => builder.Append(InvalidItemPlaceholder),
            };

            builder.DecreaseIndent();
        }

        return builder;
    }

    public static IndentedTextBuilder AppendPercentageOption(this IndentedTextBuilder builder, PercentageOption percentageOptions, string? userAttributeName = null)
    {
        var percentage = percentageOptions.Percentage;
        var value = percentageOptions.Value;

        return userAttributeName switch
        {
            null => builder.Append($"{percentage}%: '{value}'"),
            nameof(User.Identifier) => builder.Append($"{percentage}% of users: '{value}'"),
            _ => builder.Append($"{percentage}% of all {userAttributeName} attributes: '{value}'")
        };
    }

    private static IndentedTextBuilder AppendPercentageOptions(this IndentedTextBuilder builder, PercentageOption?[] percentageOptions, string? percentageOptionsAttribute = null)
    {
        for (var i = 0; i < percentageOptions.Length; i++)
        {
            if (i > 0)
            {
                builder.NewLine();
            }

            _ = percentageOptions[i] is { } percentageOption
                ? builder.AppendPercentageOption(percentageOption, percentageOptionsAttribute)
                : builder.Append(InvalidItemPlaceholder);
        }

        return builder;
    }

    private static IndentedTextBuilder AppendTargetingRuleThenPart(this IndentedTextBuilder builder, TargetingRule targetingRule, bool newLine, bool appendPercentageOptions = false, string? percentageOptionsAttribute = null)
    {
        var percentageOptions = targetingRule.PercentageOptions;

        (newLine ? builder.NewLine() : builder.Append(" "))
            .Append("THEN");

        if (percentageOptions is not { Length: > 0 })
        {
            return builder.Append($" '{targetingRule.SimpleValue?.Value ?? default}'");
        }
        else if (!appendPercentageOptions)
        {
            return builder.Append(" % options");
        }
        else
        {
            builder.IncreaseIndent();
            builder.NewLine().AppendPercentageOptions(percentageOptions, percentageOptionsAttribute);
            return builder.DecreaseIndent();
        }
    }

    public static IndentedTextBuilder AppendTargetingRuleConsequence(this IndentedTextBuilder builder, TargetingRule targetingRule, string? error, bool isMatch, bool newLine)
    {
        builder.IncreaseIndent();

        builder.AppendTargetingRuleThenPart(targetingRule, newLine)
            .Append(" => ").Append(error ?? (isMatch ? "MATCH, applying rule" : "no match"));

        return builder.DecreaseIndent();
    }

    public static IndentedTextBuilder AppendTargetingRule(this IndentedTextBuilder builder, TargetingRule targetingRule, string? percentageOptionsAttribute = null)
    {
        var conditions = targetingRule.Conditions;

        return builder.Append("IF ")
            .AppendConditions(conditions)
            .AppendTargetingRuleThenPart(targetingRule, newLine: true, appendPercentageOptions: true, percentageOptionsAttribute);
    }

    private static IndentedTextBuilder AppendTargetingRules(this IndentedTextBuilder builder, TargetingRule[] targetingRules, string percentageOptionsAttribute)
    {
        for (var i = 0; i < targetingRules.Length; i++)
        {
            if (i > 0)
            {
                builder.NewLine("ELSE ");
            }

            _ = targetingRules[i] is { } targetingRule
                ? builder.AppendTargetingRule(targetingRule, percentageOptionsAttribute)
                : builder.Append(InvalidItemPlaceholder);
        }

        return builder;
    }

    public static IndentedTextBuilder AppendSetting(this IndentedTextBuilder builder, Setting setting)
    {
        var targetingRules = setting.TargetingRules;
        var percentageOptions = setting.PercentageOptions;
        var percentageOptionsAttribute = setting.PercentageOptionsAttribute ?? nameof(User.Identifier);
        var value = setting.Value;

        builder.AppendTargetingRules(targetingRules, percentageOptionsAttribute);

        if (percentageOptions.Length > 0)
        {
            if (targetingRules.Length > 0)
            {
                builder.NewLine("OTHERWISE");
                builder.IncreaseIndent();
                builder.NewLine().AppendPercentageOptions(percentageOptions, percentageOptionsAttribute);
                builder.DecreaseIndent();
            }
            else
            {
                builder.AppendPercentageOptions(percentageOptions, percentageOptionsAttribute);
            }

            return builder.NewLine().Append($"To unidentified: '{value}'");
        }
        else if (targetingRules.Length > 0)
        {
            return builder.NewLine().Append($"To all others: '{value}'");
        }
        else
        {
            return builder.Append($"To all users: '{value}'");
        }
    }

    public static IndentedTextBuilder AppendSegment(this IndentedTextBuilder builder, Segment segment)
    {
        return builder.AppendConditions(segment.Conditions);
    }

    public static string ToDisplayText(this UserComparator comparator)
    {
        return comparator switch
        {
            UserComparator.Contains => "CONTAINS ANY OF",
            UserComparator.NotContains => "NOT CONTAINS ANY OF",
            UserComparator.SemVerOneOf => "IS ONE OF",
            UserComparator.SemVerNotOneOf => "IS NOT ONE OF",
            UserComparator.SemVerLessThan => "<",
            UserComparator.SemVerLessThanEqual => "<=",
            UserComparator.SemVerGreaterThan => ">",
            UserComparator.SemVerGreaterThanEqual => ">=",
            UserComparator.NumberEqual => "=",
            UserComparator.NumberNotEqual => "!=",
            UserComparator.NumberLessThan => "<",
            UserComparator.NumberLessThanEqual => "<=",
            UserComparator.NumberGreaterThan => ">",
            UserComparator.NumberGreaterThanEqual => ">=",
            UserComparator.SensitiveOneOf => "IS ONE OF",
            UserComparator.SensitiveNotOneOf => "IS NOT ONE OF",
            UserComparator.DateTimeBefore => "BEFORE",
            UserComparator.DateTimeAfter => "AFTER",
            UserComparator.SensitiveTextEquals => "EQUALS",
            UserComparator.SensitiveTextNotEquals => "NOT EQUALS",
            UserComparator.SensitiveTextStartsWith => "STARTS WITH ANY OF",
            UserComparator.SensitiveTextNotStartsWith => "NOT STARTS WITH ANY OF",
            UserComparator.SensitiveTextEndsWith => "ENDS WITH ANY OF",
            UserComparator.SensitiveTextNotEndsWith => "NOT ENDS WITH ANY OF",
            UserComparator.SensitiveArrayContains => "ARRAY CONTAINS ANY OF",
            UserComparator.SensitiveArrayNotContains => "ARRAY NOT CONTAINS ANY OF",
            _ => InvalidOperatorPlaceholder
        };
    }

    public static string ToDisplayText(this PrerequisiteFlagComparator comparator)
    {
        return comparator switch
        {
            PrerequisiteFlagComparator.Equals => "EQUALS",
            PrerequisiteFlagComparator.NotEquals => "NOT EQUALS",
            _ => InvalidOperatorPlaceholder
        };
    }

    public static string ToDisplayText(this SegmentComparator comparator)
    {
        return comparator switch
        {
            SegmentComparator.IsIn => "IS IN SEGMENT",
            SegmentComparator.IsNotIn => "IS NOT IN SEGMENT",
            _ => InvalidOperatorPlaceholder
        };
    }
}
