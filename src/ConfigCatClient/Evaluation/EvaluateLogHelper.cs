using System.Globalization;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Evaluation;

internal static class EvaluateLogHelper
{
    public const string InvalidNamePlaceholder = "<invalid name>";
    public const string InvalidOperatorPlaceholder = "<invalid operator>";
    public const string InvalidReferencePlaceholder = "<invalid reference>";
    public const string InvalidValuePlaceholder = "<invalid value>";

    private const int StringListMaxLength = 10;

    public static IndentedTextBuilder AppendEvaluationResult(this IndentedTextBuilder builder, bool result)
    {
        return builder.Append(result ? "true" : "false");
    }

    public static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, object? comparisonValue)
    {
        return builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue ?? InvalidValuePlaceholder}'");
    }

    public static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, string? comparisonValue, bool isSensitive = false)
    {
        return builder.AppendComparisonCondition(comparisonAttribute, comparator, !isSensitive ? (object?)comparisonValue : "<hashed value>");
    }

    public static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, string[]? comparisonValue, bool isSensitive = false)
    {
        // TODO: error handling: what to do with null items?

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

    public static IndentedTextBuilder AppendComparisonCondition(this IndentedTextBuilder builder, string? comparisonAttribute, Comparator comparator, double? comparisonValue)
    {
        return comparisonValue is not null
            ? builder.Append($"User.{comparisonAttribute} {comparator.ToDisplayText()} '{comparisonValue.Value}'")
            : builder.AppendComparisonCondition(comparisonAttribute, comparator, (object?)null);
    }

    public static IndentedTextBuilder AppendSegmentCondition(this IndentedTextBuilder builder, SegmentComparator comparator, Segment? segment)
    {
        var segmentName = segment?.Name ??
            (segment is null ? InvalidReferencePlaceholder : InvalidNamePlaceholder);
        return builder.Append($"User {comparator.ToDisplayText()} '{segmentName}'");
    }

    public static IndentedTextBuilder AppendConditionConsequence(this IndentedTextBuilder builder, bool result)
    {
        builder.Append(" => ").AppendEvaluationResult(result);
        return result ? builder : builder.Append(", skipping the remaining AND conditions");
    }

    public static IndentedTextBuilder AppendTargetingRuleConsequence(this IndentedTextBuilder builder, TargetingRule targetingRule, string? error, bool isMatch, bool newLine)
    {
        builder.IncreaseIndent();

        if (newLine)
        {
            builder.NewLine();
        }
        else
        {
            builder.Append(" ");
        }

        builder.Append("THEN ");
        if (targetingRule.PercentageOptions is not { Length: > 0 })
        {
            builder.Append($"'{targetingRule.SimpleValue?.Value ?? default}'");
        }
        else
        {
            builder.Append("% options");
        }
        builder.Append(" => ").Append(error ?? (isMatch ? "MATCH, applying rule" : "no match"));

        return builder.DecreaseIndent();
    }

    public static string ToDisplayText(this Comparator comparator)
    {
        return comparator switch
        {
            Comparator.Contains => "CONTAINS ANY OF",
            Comparator.NotContains => "NOT CONTAINS ANY OF",
            Comparator.SemVerOneOf => "IS ONE OF (semver)",
            Comparator.SemVerNotOneOf => "IS NOT ONE OF (semver)",
            Comparator.SemVerLessThan => "< (semver)",
            Comparator.SemVerLessThanEqual => "<= (semver)",
            Comparator.SemVerGreaterThan => "> (semver)",
            Comparator.SemVerGreaterThanEqual => ">= (semver)",
            Comparator.NumberEqual => "= (number)",
            Comparator.NumberNotEqual => "!= (number)",
            Comparator.NumberLessThan => "< (number)",
            Comparator.NumberLessThanEqual => "<= (number)",
            Comparator.NumberGreaterThan => "> (number)",
            Comparator.NumberGreaterThanEqual => ">= (number)",
            Comparator.SensitiveOneOf => "IS ONE OF (hashed)",
            Comparator.SensitiveNotOneOf => "IS NOT ONE OF (hashed)",
            Comparator.DateTimeBefore => "BEFORE (UTC datetime)",
            Comparator.DateTimeAfter => "AFTER (UTC datetime)",
            Comparator.SensitiveTextEquals => "EQUALS (hashed)",
            Comparator.SensitiveTextNotEquals => "NOT EQUALS (hashed)",
            Comparator.SensitiveTextStartsWith => "STARTS WITH ANY OF (hashed)",
            Comparator.SensitiveTextNotStartsWith => "NOT STARTS WITH ANY OF (hashed)",
            Comparator.SensitiveTextEndsWith => "ENDS WITH ANY OF (hashed)",
            Comparator.SensitiveTextNotEndsWith => "NOT ENDS WITH ANY OF (hashed)",
            Comparator.SensitiveArrayContains => "ARRAY CONTAINS (hashed)",
            Comparator.SensitiveArrayNotContains => "ARRAY NOT CONTAINS (hashed)",
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
