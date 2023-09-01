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

    private const int StringListMaxLength = 10;

    public static IndentedTextBuilder AppendEvaluationResult(this IndentedTextBuilder builder, bool result)
    {
        return builder.Append(result ? "true" : "false");
    }

    private static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, object? comparisonValue)
    {
        return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue ?? InvalidValuePlaceholder}'");
    }

    private static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, string? comparisonValue, bool isSensitive = false)
    {
        return builder.AppendComparisonCondition(comparisonAttribute, comparator, !isSensitive ? (object?)comparisonValue : "<hashed value>");
    }

    private static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, string[]? comparisonValue, bool isSensitive = false)
    {
        if (comparisonValue is null)
        {
            return builder.AppendComparisonCondition(comparisonAttribute, comparator, (object?)null);
        }

        const string valueText = "value", valuesText = "values";

        if (isSensitive)
        {
            return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} [<{comparisonValue.Length} hashed {(comparisonValue.Length == 1 ? valueText : valuesText)}>]");
        }
        else
        {
            var comparisonValueFormatter = new StringListFormatter(comparisonValue, StringListMaxLength, getOmittedItemsText: static count =>
                string.Format(CultureInfo.InvariantCulture, " ... <{0} more {1}>", count, count == 1 ? valueText : valuesText));

            return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} [{comparisonValueFormatter}]");
        }
    }

    private static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, double? comparisonValue, bool isDateTime = false)
    {
        if (comparisonValue is null)
        {
            return builder.AppendComparisonCondition(comparisonAttribute, comparator, (object?)null);
        }

        return isDateTime && DateTimeUtils.TryConvertFromUnixTimeSeconds(comparisonValue.Value, out var dateTime)
            ? builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue.Value}' ({dateTime:yyyy-MM-dd'T'HH:mm:ss.fffK} UTC)")
            : builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue.Value}'");
    }

    public static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, ComparisonCondition condition)
    {
        return condition.Comparator switch
        {
            Comparator.Contains or
            Comparator.NotContains or
            Comparator.SemVerOneOf or
            Comparator.SemVerNotOneOf =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringListValue),

            Comparator.SemVerLessThan or
            Comparator.SemVerLessThanEqual or
            Comparator.SemVerGreaterThan or
            Comparator.SemVerGreaterThanEqual =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringValue),

            Comparator.NumberEqual or
            Comparator.NumberNotEqual or
            Comparator.NumberLessThan or
            Comparator.NumberLessThanEqual or
            Comparator.NumberGreaterThan or
            Comparator.NumberGreaterThanEqual =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.DoubleValue),

            Comparator.SensitiveOneOf or
            Comparator.SensitiveNotOneOf or
            Comparator.SensitiveTextStartsWith or
            Comparator.SensitiveTextNotStartsWith or
            Comparator.SensitiveTextEndsWith or
            Comparator.SensitiveTextNotEndsWith =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringListValue, isSensitive: true),

            Comparator.DateTimeBefore or
            Comparator.DateTimeAfter =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.DoubleValue, isDateTime: true),

            Comparator.SensitiveTextEquals or
            Comparator.SensitiveTextNotEquals or
            Comparator.SensitiveArrayContains or
            Comparator.SensitiveArrayNotContains =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.StringValue, isSensitive: true),

            _ =>
                builder.AppendComparisonCondition(condition.ComparisonAttribute, condition.Comparator, condition.GetComparisonValue(throwIfInvalid: false)),
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

    private static IndentedTextBuilder AppendConditions<TCondition>(this IndentedTextBuilder builder, TCondition[] conditions, Func<TCondition, ICondition?> getCondition)
    {
        for (var i = 0; i < conditions.Length; i++)
        {
            builder.IncreaseIndent();

            if (i > 0)
            {
                builder.NewLine("AND ");
            }

            _ = getCondition(conditions[i]) switch
            {
                ComparisonCondition comparisonCondition => builder.AppendComparisonCondition(comparisonCondition),
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
            .AppendConditions(conditions, static condition => condition.GetCondition(throwIfInvalid: false))
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
        return builder.AppendConditions(segment.Conditions, static condition => condition);
    }

    public static string ToDisplayText(this Comparator comparator)
    {
        return comparator switch
        {
            Comparator.Contains => "CONTAINS ANY OF",
            Comparator.NotContains => "NOT CONTAINS ANY OF",
            Comparator.SemVerOneOf => "IS ONE OF",
            Comparator.SemVerNotOneOf => "IS NOT ONE OF",
            Comparator.SemVerLessThan => "<",
            Comparator.SemVerLessThanEqual => "<=",
            Comparator.SemVerGreaterThan => ">",
            Comparator.SemVerGreaterThanEqual => ">=",
            Comparator.NumberEqual => "=",
            Comparator.NumberNotEqual => "!=",
            Comparator.NumberLessThan => "<",
            Comparator.NumberLessThanEqual => "<=",
            Comparator.NumberGreaterThan => ">",
            Comparator.NumberGreaterThanEqual => ">=",
            Comparator.SensitiveOneOf => "IS ONE OF",
            Comparator.SensitiveNotOneOf => "IS NOT ONE OF",
            Comparator.DateTimeBefore => "BEFORE",
            Comparator.DateTimeAfter => "AFTER",
            Comparator.SensitiveTextEquals => "EQUALS",
            Comparator.SensitiveTextNotEquals => "NOT EQUALS",
            Comparator.SensitiveTextStartsWith => "STARTS WITH ANY OF",
            Comparator.SensitiveTextNotStartsWith => "NOT STARTS WITH ANY OF",
            Comparator.SensitiveTextEndsWith => "ENDS WITH ANY OF",
            Comparator.SensitiveTextNotEndsWith => "NOT ENDS WITH ANY OF",
            Comparator.SensitiveArrayContains => "ARRAY CONTAINS",
            Comparator.SensitiveArrayNotContains => "ARRAY NOT CONTAINS",
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
