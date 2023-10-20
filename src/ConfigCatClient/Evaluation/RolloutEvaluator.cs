using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Versioning;

namespace ConfigCat.Client.Evaluation;

internal sealed class RolloutEvaluator : IRolloutEvaluator
{
    internal const string MissingUserObjectError = "cannot evaluate, User Object is missing";
    internal const string MissingUserAttributeError = "cannot evaluate, the User.{0} attribute is missing";
    internal const string InvalidUserAttributeError = "cannot evaluate, the User.{0} attribute is invalid ({1})";
    internal const string CircularDependencyError = "cannot evaluate, circular dependency detected";

    internal const string TargetingRuleIgnoredMessage = "The current targeting rule is ignored and the evaluation continues with the next rule.";

    private readonly LoggerWrapper logger;

    public RolloutEvaluator(LoggerWrapper logger)
    {
        this.logger = logger;
    }

    public EvaluateResult Evaluate(ref EvaluateContext context)
    {
        ref var logBuilder = ref context.LogBuilder;

        // Building the evaluation log is relatively expensive, so let's not do it if it wouldn't be logged anyway.
        if (this.logger.IsEnabled(LogLevel.Info))
        {
            logBuilder = new IndentedTextBuilder();

            logBuilder.Append($"Evaluating '{context.Key}'");

            if (context.User is not null)
            {
                logBuilder.Append($" for User '{context.UserAttributes.Serialize()}'");
            }

            logBuilder.IncreaseIndent();
        }

        object? returnValue = null;
        try
        {
            var result = EvaluateSetting(ref context);
            returnValue = result.Value.GetValue(context.Setting.SettingType, throwIfInvalid: false) ?? EvaluateLogHelper.InvalidValuePlaceholder;
            return result;
        }
        catch
        {
            returnValue = context.DefaultValue.GetValue(context.Setting.SettingType, throwIfInvalid: false);
            throw;
        }
        finally
        {
            if (logBuilder is not null)
            {
                logBuilder.NewLine().Append($"Returning '{returnValue}'.");

                logBuilder.DecreaseIndent();

                this.logger.SettingEvaluated(logBuilder.ToString());
            }
        }
    }

    private EvaluateResult EvaluateSetting(ref EvaluateContext context)
    {
        var targetingRules = context.Setting.TargetingRules;
        if (targetingRules.Length > 0 && TryEvaluateTargetingRules(targetingRules, ref context, out var evaluateResult))
        {
            return evaluateResult;
        }

        var percentageOptions = context.Setting.PercentageOptions;
        if (percentageOptions.Length > 0 && TryEvaluatePercentageOptions(percentageOptions, targetingRule: null, ref context, out evaluateResult))
        {
            return evaluateResult;
        }

        evaluateResult = new EvaluateResult(context.Setting);
        return evaluateResult;
    }

    private bool TryEvaluateTargetingRules(TargetingRule[] targetingRules, ref EvaluateContext context, out EvaluateResult result)
    {
        var logBuilder = context.LogBuilder;

        logBuilder?.NewLine("Evaluating targeting rules and applying the first match if any:");

        for (var i = 0; i < targetingRules.Length; i++)
        {
            var targetingRule = targetingRules[i];
            var conditions = targetingRule.Conditions;

            if (!TryEvaluateConditions(conditions, targetingRule, contextSalt: context.Key, ref context, out var isMatch))
            {
                logBuilder?
                    .IncreaseIndent()
                    .NewLine(TargetingRuleIgnoredMessage)
                    .DecreaseIndent();
                continue;
            }
            else if (!isMatch)
            {
                continue;
            }

            if (targetingRule.SimpleValue is { } simpleValue)
            {
                result = new EvaluateResult(simpleValue, matchedTargetingRule: targetingRule);
                return true;
            }

            var percentageOptions = targetingRule.PercentageOptions;
            if (percentageOptions is not { Length: > 0 })
            {
                throw new InvalidOperationException("Targeting rule THEN part is missing or invalid.");
            }

            logBuilder?.IncreaseIndent();

            if (TryEvaluatePercentageOptions(percentageOptions, targetingRule, ref context, out result))
            {
                logBuilder?.DecreaseIndent();
                return true;
            }
            else
            {
                logBuilder?
                    .NewLine(TargetingRuleIgnoredMessage)
                    .DecreaseIndent();
                continue;
            }
        }

        result = default;
        return false;
    }

    private bool TryEvaluatePercentageOptions(PercentageOption[] percentageOptions, TargetingRule? targetingRule, ref EvaluateContext context, out EvaluateResult result)
    {
        var logBuilder = context.LogBuilder;

        if (context.User is null)
        {
            logBuilder?.NewLine("Skipping % options because the User Object is missing.");

            if (!context.IsMissingUserObjectLogged)
            {
                this.logger.UserObjectIsMissing(context.Key);
                context.IsMissingUserObjectLogged = true;
            }

            result = default;
            return false;
        }

        string? percentageOptionsAttributeValue;
        var percentageOptionsAttributeName = context.Setting.PercentageOptionsAttribute;
        if (percentageOptionsAttributeName is null)
        {
            percentageOptionsAttributeName = nameof(User.Identifier);
            percentageOptionsAttributeValue = context.User.Identifier;
        }
        else if (!context.UserAttributes!.TryGetValue(percentageOptionsAttributeName, out percentageOptionsAttributeValue))
        {
            logBuilder?.NewLine().Append($"Skipping % options because the User.{percentageOptionsAttributeName} attribute is missing.");

            if (!context.IsMissingUserObjectAttributeLogged)
            {
                this.logger.UserObjectAttributeIsMissing(context.Key, percentageOptionsAttributeName);
                context.IsMissingUserObjectAttributeLogged = true;
            }

            result = default;
            return false;
        }

        logBuilder?.NewLine().Append($"Evaluating % options based on the User.{percentageOptionsAttributeName} attribute:");

        var sha1 = (context.Key + percentageOptionsAttributeValue).Sha1();

        // NOTE: this is equivalent to hashValue = int.Parse(sha1.ToHexString().Substring(0, 7), NumberStyles.HexNumber) % 100;
        var hashValue =
            ((sha1[0] << 20)
            | (sha1[1] << 12)
            | (sha1[2] << 4)
            | (sha1[3] >> 4)) % 100;

        logBuilder?.NewLine().Append($"- Computing hash in the [0..99] range from User.{percentageOptionsAttributeName} => {hashValue} (this value is sticky and consistent across all SDKs)");

        var bucket = 0;

        for (var i = 0; i < percentageOptions.Length; i++)
        {
            var percentageOption = percentageOptions[i];

            bucket += percentageOption.Percentage;

            if (hashValue >= bucket)
            {
                continue;
            }

            var percentageOptionValue = percentageOption.Value.GetValue(context.Setting.SettingType, throwIfInvalid: false);
            logBuilder?.NewLine().Append($"- Hash value {hashValue} selects % option {i + 1} ({percentageOption.Percentage}%), '{percentageOptionValue ?? EvaluateLogHelper.InvalidValuePlaceholder}'.");

            result = new EvaluateResult(percentageOption, matchedTargetingRule: targetingRule, matchedPercentageOption: percentageOption);
            return true;
        }

        throw new InvalidOperationException("Sum of percentage option percentages are less than 100).");
    }

    private bool TryEvaluateConditions<TCondition>(TCondition[] conditions, TargetingRule? targetingRule, string contextSalt, ref EvaluateContext context, out bool result)
        where TCondition : IConditionProvider
    {
        result = true;

        var logBuilder = context.LogBuilder;
        string? error = null;
        var newLineBeforeThen = false;

        logBuilder?.NewLine("- ");

        for (var i = 0; i < conditions.Length; i++)
        {
            var condition = conditions[i].GetCondition();

            if (logBuilder is not null)
            {
                if (i == 0)
                {
                    logBuilder
                        .Append("IF ")
                        .IncreaseIndent();
                }
                else
                {
                    logBuilder
                        .IncreaseIndent()
                        .NewLine("AND ");
                }
            }

            bool conditionResult;

            switch (condition)
            {
                case UserCondition userCondition:
                    conditionResult = EvaluateUserCondition(userCondition, contextSalt, ref context, out error);
                    newLineBeforeThen = conditions.Length > 1;
                    break;

                case PrerequisiteFlagCondition prerequisiteFlagCondition:
                    conditionResult = EvaluatePrerequisiteFlagCondition(prerequisiteFlagCondition, ref context, out error);
                    newLineBeforeThen = error is null || conditions.Length > 1;
                    break;

                case SegmentCondition segmentCondition:
                    conditionResult = EvaluateSegmentCondition(segmentCondition, ref context, out error);
                    newLineBeforeThen = error is null || conditions.Length > 1;
                    break;

                default:
                    throw new InvalidOperationException(); // execution should never get here
            }

            if (targetingRule is null || conditions.Length > 1)
            {
                logBuilder?.AppendConditionConsequence(conditionResult);
            }

            logBuilder?.DecreaseIndent();

            if (!conditionResult)
            {
                result = false;
                break;
            }
            else
            {
                Debug.Assert(error is null, "Unexpected error reported by condition evaluation.");
            }
        }

        if (targetingRule is not null)
        {
            logBuilder?.AppendTargetingRuleConsequence(targetingRule, error, result, newLineBeforeThen);
        }

        return error is null;
    }

    private bool EvaluateUserCondition(UserCondition condition, string contextSalt, ref EvaluateContext context, out string? error)
    {
        error = null;

        var logBuilder = context.LogBuilder;
        logBuilder?.AppendUserCondition(condition);

        if (context.User is null)
        {
            if (!context.IsMissingUserObjectLogged)
            {
                this.logger.UserObjectIsMissing(context.Key);
                context.IsMissingUserObjectLogged = true;
            }

            error = MissingUserObjectError;
            return false;
        }

        var userAttributeName = condition.ComparisonAttribute ?? throw new InvalidOperationException("Comparison attribute name is missing.");

        if (!(context.UserAttributes!.TryGetValue(userAttributeName, out var userAttributeValue) && userAttributeValue.Length > 0))
        {
            this.logger.UserObjectAttributeIsMissing(condition.ToString(), context.Key, userAttributeName);
            error = string.Format(CultureInfo.InvariantCulture, MissingUserAttributeError, userAttributeName);
            return false;
        }

        var comparator = condition.Comparator;
        switch (comparator)
        {
            case UserComparator.TextEquals:
            case UserComparator.TextNotEquals:
                return EvaluateTextEquals(userAttributeValue!, condition.StringValue, negate: comparator == UserComparator.TextNotEquals);

            case UserComparator.SensitiveTextEquals:
            case UserComparator.SensitiveTextNotEquals:
                return EvaluateSensitiveTextEquals(userAttributeValue!, condition.StringValue,
                    EnsureConfigJsonSalt(context.Setting.ConfigJsonSalt), contextSalt, negate: comparator == UserComparator.SensitiveTextNotEquals);

            case UserComparator.IsOneOf:
            case UserComparator.IsNotOneOf:
                return EvaluateIsOneOf(userAttributeValue!, condition.StringListValue, negate: comparator == UserComparator.IsNotOneOf);

            case UserComparator.SensitiveIsOneOf:
            case UserComparator.SensitiveIsNotOneOf:
                return EvaluateSensitiveIsOneOf(userAttributeValue!, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.ConfigJsonSalt), contextSalt, negate: comparator == UserComparator.SensitiveIsNotOneOf);

            case UserComparator.TextStartsWithAnyOf:
            case UserComparator.TextNotStartsWithAnyOf:
                return EvaluateTextSliceEqualsAnyOf(userAttributeValue!, condition.StringListValue, startsWith: true, negate: comparator == UserComparator.TextNotStartsWithAnyOf);

            case UserComparator.SensitiveTextStartsWithAnyOf:
            case UserComparator.SensitiveTextNotStartsWithAnyOf:
                return EvaluateSensitiveTextSliceEqualsAnyOf(userAttributeValue!, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.ConfigJsonSalt), contextSalt, startsWith: true, negate: comparator == UserComparator.SensitiveTextNotStartsWithAnyOf);

            case UserComparator.TextEndsWithAnyOf:
            case UserComparator.TextNotEndsWithAnyOf:
                return EvaluateTextSliceEqualsAnyOf(userAttributeValue!, condition.StringListValue, startsWith: false, negate: comparator == UserComparator.TextNotEndsWithAnyOf);

            case UserComparator.SensitiveTextEndsWithAnyOf:
            case UserComparator.SensitiveTextNotEndsWithAnyOf:
                return EvaluateSensitiveTextSliceEqualsAnyOf(userAttributeValue!, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.ConfigJsonSalt), contextSalt, startsWith: false, negate: comparator == UserComparator.SensitiveTextNotEndsWithAnyOf);

            case UserComparator.ContainsAnyOf:
            case UserComparator.NotContainsAnyOf:
                return EvaluateContainsAnyOf(userAttributeValue!, condition.StringListValue, negate: comparator == UserComparator.NotContainsAnyOf);

            case UserComparator.SemVerIsOneOf:
            case UserComparator.SemVerIsNotOneOf:
                if (!SemVersion.TryParse(userAttributeValue!.Trim(), out var version, strict: true))
                {
                    error = HandleInvalidUserAttribute(condition, context.Key, userAttributeName, $"'{userAttributeValue}' is not a valid semantic version");
                    return false;
                }
                return EvaluateSemVerIsOneOf(version, condition.StringListValue, negate: comparator == UserComparator.SemVerIsNotOneOf);

            case UserComparator.SemVerLess:
            case UserComparator.SemVerLessOrEquals:
            case UserComparator.SemVerGreater:
            case UserComparator.SemVerGreaterOrEquals:
                if (!SemVersion.TryParse(userAttributeValue!.Trim(), out version, strict: true))
                {
                    error = HandleInvalidUserAttribute(condition, context.Key, userAttributeName, $"'{userAttributeValue}' is not a valid semantic version");
                    return false;
                }
                return EvaluateSemVerRelation(version, comparator, condition.StringValue);

            case UserComparator.NumberEquals:
            case UserComparator.NumberNotEquals:
            case UserComparator.NumberLess:
            case UserComparator.NumberLessOrEquals:
            case UserComparator.NumberGreater:
            case UserComparator.NumberGreaterOrEquals:
                if (!double.TryParse(userAttributeValue!.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    error = HandleInvalidUserAttribute(condition, context.Key, userAttributeName, $"'{userAttributeValue}' is not a valid decimal number");
                    return false;
                }
                return EvaluateNumberRelation(number, condition.Comparator, condition.DoubleValue);

            case UserComparator.DateTimeBefore:
            case UserComparator.DateTimeAfter:
                if (!double.TryParse(userAttributeValue!.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    error = HandleInvalidUserAttribute(condition, context.Key, userAttributeName, $"'{userAttributeValue}' is not a valid Unix timestamp (number of seconds elapsed since Unix epoch)");
                    return false;
                }
                return EvaluateDateTimeRelation(number, condition.DoubleValue, before: comparator == UserComparator.DateTimeBefore);

            case UserComparator.ArrayContainsAnyOf:
            case UserComparator.ArrayNotContainsAnyOf:
                var array = userAttributeValue!.DeserializeOrDefault<string[]>();
                if (array is null)
                {
                    error = HandleInvalidUserAttribute(condition, context.Key, userAttributeName, $"'{userAttributeValue}' is not a valid JSON string array");
                    return false;
                }

                return EvaluateArrayContainsAnyOf(array, condition.StringListValue, negate: comparator == UserComparator.ArrayNotContainsAnyOf);

            case UserComparator.SensitiveArrayContainsAnyOf:
            case UserComparator.SensitiveArrayNotContainsAnyOf:
                array = userAttributeValue!.DeserializeOrDefault<string[]>();
                if (array is null)
                {
                    error = HandleInvalidUserAttribute(condition, context.Key, userAttributeName, $"'{userAttributeValue}' is not a valid JSON string array");
                    return false;
                }

                return EvaluateSensitiveArrayContainsAnyOf(array, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.ConfigJsonSalt), contextSalt, negate: comparator == UserComparator.SensitiveArrayNotContainsAnyOf);

            default:
                throw new InvalidOperationException("Comparison operator is invalid.");
        }
    }

    private static bool EvaluateTextEquals(string text, string? comparisonValue, bool negate)
    {
        EnsureComparisonValue(comparisonValue);

        return text.Equals(comparisonValue) ^ negate;
    }

    private static bool EvaluateSensitiveTextEquals(string text, string? comparisonValue, string configJsonSalt, string contextSalt, bool negate)
    {
        EnsureComparisonValue(comparisonValue);

        var hash = HashComparisonValue(text.AsSpan(), configJsonSalt, contextSalt);

        return hash.Equals(hexString: comparisonValue.AsSpan()) ^ negate;
    }

    private static bool EvaluateIsOneOf(string text, string[]? comparisonValues, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            if (text.Equals(EnsureComparisonValue(comparisonValues[i])))
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateSensitiveIsOneOf(string text, string[]? comparisonValues, string configJsonSalt, string contextSalt, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        var hash = HashComparisonValue(text.AsSpan(), configJsonSalt, contextSalt);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            if (hash.Equals(hexString: EnsureComparisonValue(comparisonValues[i]).AsSpan()))
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateTextSliceEqualsAnyOf(string text, string[]? comparisonValues, bool startsWith, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            var item = EnsureComparisonValue(comparisonValues[i]);

            if (text.Length < item.Length)
            {
                continue;
            }

            var slice = startsWith ? text.AsSpan(0, item.Length) : text.AsSpan(text.Length - item.Length);

            if (slice.SequenceEqual(item.AsSpan()))
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateSensitiveTextSliceEqualsAnyOf(string text, string[]? comparisonValues, string configJsonSalt, string contextSalt, bool startsWith, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            var item = EnsureComparisonValue(comparisonValues[i]);

            ReadOnlySpan<char> hash2;

            var index = item.IndexOf('_');
            if (index < 0
                || !int.TryParse(item.AsSpan(0, index).ToParsable(), NumberStyles.None, CultureInfo.InvariantCulture, out var sliceLength)
                || (hash2 = item.AsSpan(index + 1)).IsEmpty)
            {
                EnsureComparisonValue<string>(null);
                break; // execution should never get here (this is just for keeping the compiler happy)
            }

            if (text.Length < sliceLength)
            {
                continue;
            }

            var slice = startsWith ? text.AsSpan(0, sliceLength) : text.AsSpan(text.Length - sliceLength);

            var hash = HashComparisonValue(slice, configJsonSalt, contextSalt);
            if (hash.Equals(hexString: hash2))
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateContainsAnyOf(string text, string[]? comparisonValues, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            if (text.Contains(EnsureComparisonValue(comparisonValues[i])))
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateSemVerIsOneOf(SemVersion version, string[]? comparisonValues, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        var result = false;

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            var item = EnsureComparisonValue(comparisonValues[i]);

            // NOTE: Previous versions of the evaluation algorithm ignore empty comparison values.
            // We keep this behavior for backward compatibility.
            if (item.Length == 0)
            {
                continue;
            }

            if (!SemVersion.TryParse(item.Trim(), out var version2, strict: true))
            {
                // NOTE: Previous versions of the evaluation algorithm ignored invalid comparison values.
                // We keep this behavior for backward compatibility.
                return false;
            }

            if (!result && version.PrecedenceMatches(version2))
            {
                // NOTE: Previous versions of the evaluation algorithm require that
                // all the comparison values are empty or valid, that is, we can't stop when finding a match.
                // We keep this behavior for backward compatibility.
                result = true;
            }
        }

        return result ^ negate;
    }

    private static bool EvaluateSemVerRelation(SemVersion version, UserComparator comparator, string? comparisonValue)
    {
        EnsureComparisonValue(comparisonValue);

        if (!SemVersion.TryParse(comparisonValue.Trim(), out var version2, strict: true))
        {
            return false;
        }

        var comparisonResult = version.CompareByPrecedence(version2);

        return comparator switch
        {
            UserComparator.SemVerLess => comparisonResult < 0,
            UserComparator.SemVerLessOrEquals => comparisonResult <= 0,
            UserComparator.SemVerGreater => comparisonResult > 0,
            UserComparator.SemVerGreaterOrEquals => comparisonResult >= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(comparator), comparator, null)
        };
    }

    private static bool EvaluateNumberRelation(double number, UserComparator comparator, double? comparisonValue)
    {
        var number2 = EnsureComparisonValue(comparisonValue).Value;

        return comparator switch
        {
            UserComparator.NumberEquals => number == number2,
            UserComparator.NumberNotEquals => number != number2,
            UserComparator.NumberLess => number < number2,
            UserComparator.NumberLessOrEquals => number <= number2,
            UserComparator.NumberGreater => number > number2,
            UserComparator.NumberGreaterOrEquals => number >= number2,
            _ => throw new ArgumentOutOfRangeException(nameof(comparator), comparator, null)
        };
    }

    private static bool EvaluateDateTimeRelation(double number, double? comparisonValue, bool before)
    {
        var number2 = EnsureComparisonValue(comparisonValue).Value;

        return before ? number < number2 : number > number2;
    }

    private static bool EvaluateArrayContainsAnyOf(string[] array, string[]? comparisonValues, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        for (var i = 0; i < array.Length; i++)
        {
            var text = array[i];

            for (var j = 0; j < comparisonValues.Length; j++)
            {
                if (text.Equals(EnsureComparisonValue(comparisonValues[j])))
                {
                    return !negate;
                }
            }
        }

        return negate;
    }

    private static bool EvaluateSensitiveArrayContainsAnyOf(string[] array, string[]? comparisonValues, string configJsonSalt, string contextSalt, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        for (var i = 0; i < array.Length; i++)
        {
            var hash = HashComparisonValue(array[i].AsSpan(), configJsonSalt, contextSalt);

            for (var j = 0; j < comparisonValues.Length; j++)
            {
                if (hash.Equals(hexString: EnsureComparisonValue(comparisonValues[j]).AsSpan()))
                {
                    return !negate;
                }
            }
        }

        return negate;
    }

    private bool EvaluatePrerequisiteFlagCondition(PrerequisiteFlagCondition condition, ref EvaluateContext context, out string? error)
    {
        error = null;

        var logBuilder = context.LogBuilder;
        logBuilder?.AppendPrerequisiteFlagCondition(condition);

        var prerequisiteFlagKey = condition.PrerequisiteFlagKey;
        if (prerequisiteFlagKey is null || !context.Settings.TryGetValue(prerequisiteFlagKey, out var prerequisiteFlag))
        {
            throw new InvalidOperationException("Prerequisite flag key is missing or invalid.");
        }

        var comparisonValue = condition.ComparisonValue.GetValue(throwIfInvalid: false);
        if (comparisonValue is null || comparisonValue.GetType().ToSettingType() != prerequisiteFlag.SettingType)
        {
            EnsureComparisonValue<string>(null);
        }

        context.VisitedFlags.Add(context.Key);
        if (context.VisitedFlags.Contains(prerequisiteFlagKey!))
        {
            context.VisitedFlags.Add(prerequisiteFlagKey!);
            var dependencyCycle = new StringListFormatter(context.VisitedFlags).ToString("a", CultureInfo.InvariantCulture);
            this.logger.CircularDependencyDetected(condition.ToString(), context.Key, dependencyCycle);

            context.VisitedFlags.RemoveRange(context.VisitedFlags.Count - 2, 2);
            error = CircularDependencyError;
            return false;
        }

        var prerequisiteFlagContext = new EvaluateContext(prerequisiteFlagKey!, prerequisiteFlag!, ref context);

        logBuilder?
            .NewLine("(")
            .IncreaseIndent()
            .NewLine().Append($"Evaluating prerequisite flag '{prerequisiteFlagKey}':");

        var prerequisiteFlagEvaluateResult = EvaluateSetting(ref prerequisiteFlagContext);

        context.VisitedFlags.RemoveAt(context.VisitedFlags.Count - 1);

        var prerequisiteFlagValue = prerequisiteFlagEvaluateResult.Value.GetValue(prerequisiteFlag!.SettingType, throwIfInvalid: false);

        var comparator = condition.Comparator;
        var result = comparator switch
        {
            PrerequisiteFlagComparator.Equals => prerequisiteFlagValue is not null && prerequisiteFlagValue.Equals(comparisonValue),
            PrerequisiteFlagComparator.NotEquals => prerequisiteFlagValue is not null && !prerequisiteFlagValue.Equals(comparisonValue),
            _ => throw new InvalidOperationException("Comparison operator is invalid.")
        };

        logBuilder?
            .NewLine().Append($"Prerequisite flag evaluation result: '{prerequisiteFlagValue ?? EvaluateLogHelper.InvalidValuePlaceholder}'.")
            .NewLine("Condition (")
                .AppendPrerequisiteFlagCondition(condition)
                .Append(") evaluates to ").AppendEvaluationResult(result).Append(".")
            .DecreaseIndent()
            .NewLine(")");

        return result;
    }

    private bool EvaluateSegmentCondition(SegmentCondition condition, ref EvaluateContext context, out string? error)
    {
        error = null;

        var logBuilder = context.LogBuilder;
        logBuilder?.AppendSegmentCondition(condition);

        if (context.User is null)
        {
            if (!context.IsMissingUserObjectLogged)
            {
                this.logger.UserObjectIsMissing(context.Key);
                context.IsMissingUserObjectLogged = true;
            }

            error = MissingUserObjectError;
            return false;
        }

        var segment = condition.Segment ?? throw new InvalidOperationException("Segment reference is invalid.");

        if (segment.Name is not { Length: > 0 })
        {
            throw new InvalidOperationException("Segment name is missing.");
        }

        logBuilder?
            .NewLine("(")
            .IncreaseIndent()
            .NewLine().Append($"Evaluating segment '{segment.Name}':");

        TryEvaluateConditions(segment.Conditions, targetingRule: null, contextSalt: segment.Name, ref context, out var segmentResult);

        var comparator = condition.Comparator;
        var result = comparator switch
        {
            SegmentComparator.IsIn => segmentResult,
            SegmentComparator.IsNotIn => !segmentResult,
            _ => throw new InvalidOperationException("Comparison operator is invalid.")
        };

        logBuilder?
            .NewLine().Append($"Segment evaluation result: User {(segmentResult ? SegmentComparator.IsIn : SegmentComparator.IsNotIn).ToDisplayText()}.")
            .NewLine("Condition (")
                .AppendSegmentCondition(condition)
                .Append(") evaluates to ").AppendEvaluationResult(result).Append(".")
            .DecreaseIndent()
            .NewLine(")");

        return result;
    }

    private static byte[] HashComparisonValue(ReadOnlySpan<char> value, string configJsonSalt, string contextSalt)
    {
        return string.Concat(value.ToConcatenable(), configJsonSalt, contextSalt).Sha256();
    }

    private static string EnsureConfigJsonSalt([NotNull] string? value)
    {
        return value ?? throw new InvalidOperationException("Config JSON salt is missing.");
    }

    [return: NotNull]
    private static T EnsureComparisonValue<T>([NotNull] T? value)
    {
        return value ?? throw new InvalidOperationException("Comparison value is missing or invalid.");
    }

    private string HandleInvalidUserAttribute(UserCondition condition, string key, string userAttributeName, string reason)
    {
        this.logger.UserObjectAttributeIsInvalid(condition.ToString(), key, reason, userAttributeName);
        return string.Format(CultureInfo.InvariantCulture, InvalidUserAttributeError, userAttributeName, reason);
    }
}
