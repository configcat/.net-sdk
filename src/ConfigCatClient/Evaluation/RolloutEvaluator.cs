using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Versioning;

namespace ConfigCat.Client.Evaluation;

internal sealed class RolloutEvaluator : IRolloutEvaluator
{
    internal const string MissingUserObjectError = "cannot evaluate, User Object is missing";
    internal const string MissingUserAttributeError = "cannot evaluate, the User.{0} attribute is missing";
    internal const string InvalidUserAttributeError = "cannot evaluate, the User.{0} attribute is invalid ({1})";

    internal const string TargetingRuleIgnoredMessage = "The current targeting rule is ignored and the evaluation continues with the next rule.";

    private readonly LoggerWrapper logger;

    public RolloutEvaluator(LoggerWrapper logger)
    {
        this.logger = logger;
    }

    public EvaluateResult Evaluate<T>(T defaultValue, ref EvaluateContext context, [NotNull] out T returnValue)
    {
        ref var logBuilder = ref context.LogBuilder;

        // Building the evaluation log is expensive, so let's not do it if it wouldn't be logged anyway.
        if (this.logger.IsEnabled(LogLevel.Info))
        {
            logBuilder = new IndentedTextBuilder();

            logBuilder.Append($"Evaluating '{context.Key}'");

            if (context.User is not null)
            {
                logBuilder.Append($" for User '{SerializationHelper.SerializeUser(context.User)}'");
            }

            logBuilder.IncreaseIndent();
        }

        returnValue = default!;
        try
        {
            EvaluateResult result;

            if (typeof(T) != typeof(object))
            {
                var expectedSettingType = typeof(T).ToSettingType();

                // NOTE: We've already checked earlier in the call chain that T is an allowed type (see also TypeExtensions.EnsureSupportedSettingClrType).
                Debug.Assert(expectedSettingType != Setting.UnknownType, "Type is not supported.");

                // context.Setting.SettingType can be unknown in two cases:
                // 1. when the setting type is missing from the config JSON (which should occur in the case of a full config JSON flag override only) or
                // 2. when the setting comes from a non-full config JSON flag override and has an unsupported value (see also ObjectExtensions.ToSetting).
                // The latter case is handled by SettingValue.GetValue<T> below.
                if (context.Setting.settingType != Setting.UnknownType && context.Setting.settingType != expectedSettingType)
                {
                    throw new EvaluationErrorException(EvaluationErrorCode.SettingValueTypeMismatch,
                        "The type of a setting must match the type of the specified default value. "
                        + $"Setting's type was {context.Setting.settingType} but the default value's type was {typeof(T)}. "
                        + $"Please use a default value which corresponds to the setting type {context.Setting.settingType}. "
                        + "Learn more: https://configcat.com/docs/sdk-reference/dotnet/#setting-type-mapping");
                }

                result = EvaluateSetting(ref context);

                returnValue = result.Value.GetValue<T>(expectedSettingType)!;
            }
            else
            {
                result = EvaluateSetting(ref context);

                returnValue = (T)(context.Setting.settingType != Setting.UnknownType
                    ? result.Value.GetValue(context.Setting.settingType)!
                    : result.Value.GetValue()!);
            }

            return result;
        }
        catch
        {
            logBuilder?.ResetIndent().IncreaseIndent();

            returnValue = defaultValue;
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
        var targetingRules = context.Setting.TargetingRulesOrEmpty;
        if (targetingRules.Length > 0 && TryEvaluateTargetingRules(targetingRules, ref context, out var evaluateResult))
        {
            return evaluateResult;
        }

        var percentageOptions = context.Setting.PercentageOptionsOrEmpty;
        if (percentageOptions.Length > 0 && TryEvaluatePercentageOptions(percentageOptions, matchedTargetingRule: null, ref context, out evaluateResult))
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
            var conditions = targetingRule.ConditionsOrEmpty;

            var isMatch = EvaluateConditions(conditions, targetingRule, contextSalt: context.Key, ref context, out var error);
            if (!isMatch)
            {
                if (error is not null)
                {
                    logBuilder?
                        .IncreaseIndent()
                        .NewLine(TargetingRuleIgnoredMessage)
                        .DecreaseIndent();
                }
                continue;
            }

            if (targetingRule.SimpleValueOrNull is { } simpleValue)
            {
                result = new EvaluateResult(simpleValue, matchedTargetingRule: targetingRule);
                return true;
            }

            var percentageOptions = targetingRule.PercentageOptionsOrNull;
            if (percentageOptions is not { Length: > 0 })
            {
                throw new InvalidConfigModelException("Targeting rule THEN part is missing or invalid.");
            }

            logBuilder?.IncreaseIndent();

            if (TryEvaluatePercentageOptions(percentageOptions, targetingRule, ref context, out result))
            {
                logBuilder?.DecreaseIndent();
                return true;
            }

            logBuilder?
                .NewLine(TargetingRuleIgnoredMessage)
                .DecreaseIndent();
        }

        result = default;
        return false;
    }

    private bool TryEvaluatePercentageOptions(PercentageOption[] percentageOptions, TargetingRule? matchedTargetingRule, ref EvaluateContext context, out EvaluateResult result)
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

        var percentageOptionsAttributeName = context.Setting.percentageOptionsAttribute;
        object? percentageOptionsAttributeValue;

        if (percentageOptionsAttributeName is null)
        {
            percentageOptionsAttributeName = nameof(User.Identifier);
            percentageOptionsAttributeValue = context.User.Identifier;
        }
        else
        {
            percentageOptionsAttributeValue = context.User.GetAttribute(percentageOptionsAttributeName);
        }

        if (percentageOptionsAttributeValue is null)
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

        var sha1 = HashPercentageOptionsAttribute(context.Key, UserAttributeValueToString(percentageOptionsAttributeValue));

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

            bucket += percentageOption.percentage;

            if (hashValue >= bucket)
            {
                continue;
            }

            if (logBuilder is not null)
            {
                var percentageOptionValue = percentageOption.value.GetValue(throwIfInvalid: false) ?? EvaluateLogHelper.InvalidValuePlaceholder;
                logBuilder.NewLine().Append($"- Hash value {hashValue} selects % option {i + 1} ({percentageOption.percentage}%), '{percentageOptionValue}'.");
            }

            result = new EvaluateResult(percentageOption, matchedTargetingRule, matchedPercentageOption: percentageOption);
            return true;
        }

        throw new InvalidConfigModelException("Sum of percentage option percentages is less than 100.");
    }

    private bool EvaluateConditions<TCondition>(TCondition[] conditions, TargetingRule? targetingRule, string contextSalt, ref EvaluateContext context, out string? error)
        where TCondition : IConditionProvider
    {
        error = null;
        var result = true;

        var logBuilder = context.LogBuilder;
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
                    conditionResult = EvaluatePrerequisiteFlagCondition(prerequisiteFlagCondition, ref context);
                    newLineBeforeThen = true;
                    break;

                case SegmentCondition segmentCondition:
                    conditionResult = EvaluateSegmentCondition(segmentCondition, ref context, out error);
                    newLineBeforeThen = error is null || error != MissingUserObjectError || conditions.Length > 1;
                    break;

                default:
                    throw new InvalidOperationException(); // execution should never get here
            }

            if (logBuilder is not null)
            {
                if (targetingRule is null || conditions.Length > 1)
                {
                    logBuilder.AppendConditionConsequence(conditionResult);
                }

                logBuilder.DecreaseIndent();
            }

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

        return result;
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

        var userAttributeName = condition.comparisonAttribute ?? throw new InvalidConfigModelException("Comparison attribute name is missing.");
        var userAttributeValue = context.User.GetAttribute(userAttributeName);

        if (userAttributeValue is null or string { Length: 0 })
        {
            this.logger.UserObjectAttributeIsMissing(condition, context.Key, userAttributeName);
            error = string.Format(CultureInfo.InvariantCulture, MissingUserAttributeError, userAttributeName);
            return false;
        }

        var comparator = condition.comparator;
        switch (comparator)
        {
            case UserComparator.TextEquals:
            case UserComparator.TextNotEquals:
                var text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateTextEquals(text, condition.StringValue, negate: comparator == UserComparator.TextNotEquals);

            case UserComparator.SensitiveTextEquals:
            case UserComparator.SensitiveTextNotEquals:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateSensitiveTextEquals(text, condition.StringValue,
                    EnsureConfigJsonSalt(context.Setting.configJsonSalt), contextSalt, negate: comparator == UserComparator.SensitiveTextNotEquals);

            case UserComparator.TextIsOneOf:
            case UserComparator.TextIsNotOneOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateTextIsOneOf(text, condition.StringListValue, negate: comparator == UserComparator.TextIsNotOneOf);

            case UserComparator.SensitiveTextIsOneOf:
            case UserComparator.SensitiveTextIsNotOneOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateSensitiveTextIsOneOf(text, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.configJsonSalt), contextSalt, negate: comparator == UserComparator.SensitiveTextIsNotOneOf);

            case UserComparator.TextStartsWithAnyOf:
            case UserComparator.TextNotStartsWithAnyOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateTextSliceEqualsAnyOf(text, condition.StringListValue, startsWith: true, negate: comparator == UserComparator.TextNotStartsWithAnyOf);

            case UserComparator.SensitiveTextStartsWithAnyOf:
            case UserComparator.SensitiveTextNotStartsWithAnyOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateSensitiveTextSliceEqualsAnyOf(text, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.configJsonSalt), contextSalt, startsWith: true, negate: comparator == UserComparator.SensitiveTextNotStartsWithAnyOf);

            case UserComparator.TextEndsWithAnyOf:
            case UserComparator.TextNotEndsWithAnyOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateTextSliceEqualsAnyOf(text, condition.StringListValue, startsWith: false, negate: comparator == UserComparator.TextNotEndsWithAnyOf);

            case UserComparator.SensitiveTextEndsWithAnyOf:
            case UserComparator.SensitiveTextNotEndsWithAnyOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateSensitiveTextSliceEqualsAnyOf(text, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.configJsonSalt), contextSalt, startsWith: false, negate: comparator == UserComparator.SensitiveTextNotEndsWithAnyOf);

            case UserComparator.TextContainsAnyOf:
            case UserComparator.TextNotContainsAnyOf:
                text = GetUserAttributeValueAsText(userAttributeName, userAttributeValue, condition, context.Key);
                return EvaluateTextContainsAnyOf(text, condition.StringListValue, negate: comparator == UserComparator.TextNotContainsAnyOf);

            case UserComparator.SemVerIsOneOf:
            case UserComparator.SemVerIsNotOneOf:
                var version = GetUserAttributeValueAsSemVer(userAttributeName, userAttributeValue, condition, context.Key, out error);
                return error is null && EvaluateSemVerIsOneOf(version!, condition.StringListValue, condition.SemVerListValue, negate: comparator == UserComparator.SemVerIsNotOneOf);

            case UserComparator.SemVerLess:
            case UserComparator.SemVerLessOrEquals:
            case UserComparator.SemVerGreater:
            case UserComparator.SemVerGreaterOrEquals:
                version = GetUserAttributeValueAsSemVer(userAttributeName, userAttributeValue, condition, context.Key, out error);
                return error is null && EvaluateSemVerRelation(version!, comparator, condition.StringValue, condition.SemVerValue);

            case UserComparator.NumberEquals:
            case UserComparator.NumberNotEquals:
            case UserComparator.NumberLess:
            case UserComparator.NumberLessOrEquals:
            case UserComparator.NumberGreater:
            case UserComparator.NumberGreaterOrEquals:
                var number = GetUserAttributeValueAsNumber(userAttributeName, userAttributeValue, condition, context.Key, out error);
                return error is null && EvaluateNumberRelation(number, comparator, condition.DoubleValue);

            case UserComparator.DateTimeBefore:
            case UserComparator.DateTimeAfter:
                number = GetUserAttributeValueAsUnixTimeSeconds(userAttributeName, userAttributeValue, condition, context.Key, out error);
                return error is null && EvaluateDateTimeRelation(number, condition.DoubleValue, before: comparator == UserComparator.DateTimeBefore);

            case UserComparator.ArrayContainsAnyOf:
            case UserComparator.ArrayNotContainsAnyOf:
                var stringArray = GetUserAttributeValueAsStringArray(userAttributeName, userAttributeValue, condition, context.Key, out error);
                return error is null && EvaluateArrayContainsAnyOf(stringArray!, condition.StringListValue, negate: comparator == UserComparator.ArrayNotContainsAnyOf);

            case UserComparator.SensitiveArrayContainsAnyOf:
            case UserComparator.SensitiveArrayNotContainsAnyOf:
                stringArray = GetUserAttributeValueAsStringArray(userAttributeName, userAttributeValue, condition, context.Key, out error);
                return error is null && EvaluateSensitiveArrayContainsAnyOf(stringArray!, condition.StringListValue,
                    EnsureConfigJsonSalt(context.Setting.configJsonSalt), contextSalt, negate: comparator == UserComparator.SensitiveArrayNotContainsAnyOf);

            default:
                throw new InvalidConfigModelException("Comparison operator is invalid.");
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

        var hash = HashComparisonValue(text, configJsonSalt, contextSalt);

        return hash.Equals(hexString: comparisonValue.AsSpan()) ^ negate;
    }

    private static bool EvaluateTextIsOneOf(string text, string[]? comparisonValues, bool negate)
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

    private static bool EvaluateSensitiveTextIsOneOf(string text, string[]? comparisonValues, string configJsonSalt, string contextSalt, bool negate)
    {
        EnsureComparisonValue(comparisonValues);

        var hash = HashComparisonValue(text, configJsonSalt, contextSalt);

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

        var textUtf8 = Encoding.UTF8.GetBytes(text);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            var item = EnsureComparisonValue(comparisonValues[i]);

            ReadOnlySpan<char> hash2;

            var index = item.IndexOf('_');
            if (index < 0
                || !int.TryParse(item.AsSpan(0, index).ToParsable(), NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var sliceLength)
                || (hash2 = item.AsSpan(index + 1)).IsEmpty)
            {
                EnsureComparisonValue<string>(null);
                break; // execution should never get here (this is just for keeping the compiler happy)
            }

            if (textUtf8.Length < sliceLength)
            {
                continue;
            }

            var slice = startsWith ? textUtf8.AsSpan(0, sliceLength) : textUtf8.AsSpan(textUtf8.Length - sliceLength);

            var hash = HashComparisonValue(slice, configJsonSalt, contextSalt);
            if (hash.Equals(hexString: hash2))
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateTextContainsAnyOf(string text, string[]? comparisonValues, bool negate)
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

    private static bool EvaluateSemVerIsOneOf(SemVersion version, string[]? comparisonValues, SemVersion?[]? parsedComparisonValues, bool negate)
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

            var version2 = parsedComparisonValues![i];
            if (version2 is null)
            {
                // NOTE: Previous versions of the evaluation algorithm ignored invalid comparison values.
                // We keep this behavior for backward compatibility.
                return false;
            }

            if (!result && version.PrecedenceMatches(version2))
            {
                // NOTE: Previous versions of the evaluation algorithm require that
                // none of the comparison values are empty or invalid, that is, we can't stop when finding a match.
                // We keep this behavior for backward compatibility.
                result = true;
            }
        }

        return result ^ negate;
    }

    private static bool EvaluateSemVerRelation(SemVersion version, UserComparator comparator, string? comparisonValue, SemVersion? parsedComparisonValue)
    {
        EnsureComparisonValue(comparisonValue);

        var version2 = parsedComparisonValue!;
        if (version2 is null)
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
            var hash = HashComparisonValue(array[i], configJsonSalt, contextSalt);

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

    private bool EvaluatePrerequisiteFlagCondition(PrerequisiteFlagCondition condition, ref EvaluateContext context)
    {
        var logBuilder = context.LogBuilder;
        logBuilder?.AppendPrerequisiteFlagCondition(condition, context.Settings);

        Setting? prerequisiteFlag;
        var prerequisiteFlagKey = condition.prerequisiteFlagKey;
        if (prerequisiteFlagKey is null)
        {
            throw new InvalidConfigModelException("Prerequisite flag key is missing.");
        }
        else if (!context.Settings.TryGetValue(prerequisiteFlagKey, out prerequisiteFlag))
        {
            throw new InvalidConfigModelException("Prerequisite flag is missing.");
        }

        var comparisonValue = EnsureComparisonValue(condition.comparisonValue.GetValue(throwIfInvalid: false));

        var expectedSettingType = comparisonValue.GetType().ToSettingType();
        if (prerequisiteFlag.settingType != Setting.UnknownType && prerequisiteFlag.settingType != expectedSettingType)
        {
            throw new InvalidConfigModelException($"Type mismatch between comparison value '{comparisonValue}' and prerequisite flag '{prerequisiteFlagKey}'.");
        }

        context.VisitedFlags.Add(context.Key);
        if (context.VisitedFlags.Contains(prerequisiteFlagKey!))
        {
            context.VisitedFlags.Add(prerequisiteFlagKey!);
            var dependencyCycle = new StringListFormatter(context.VisitedFlags).ToString("a", CultureInfo.InvariantCulture);
            throw new InvalidConfigModelException($"Circular dependency detected between the following depending flags: {dependencyCycle}.");
        }

        var prerequisiteFlagContext = new EvaluateContext(prerequisiteFlagKey!, prerequisiteFlag!, context);

        logBuilder?
            .NewLine("(")
            .IncreaseIndent()
            .NewLine().Append($"Evaluating prerequisite flag '{prerequisiteFlagKey}':");

        var prerequisiteFlagEvaluateResult = EvaluateSetting(ref prerequisiteFlagContext);

        context.VisitedFlags.RemoveAt(context.VisitedFlags.Count - 1);

        var prerequisiteFlagValue = prerequisiteFlagEvaluateResult.Value.GetValue(expectedSettingType, throwIfInvalid: true)!;

        var comparator = condition.comparator;
        var result = comparator switch
        {
            PrerequisiteFlagComparator.Equals => prerequisiteFlagValue.Equals(comparisonValue),
            PrerequisiteFlagComparator.NotEquals => !prerequisiteFlagValue.Equals(comparisonValue),
            _ => throw new InvalidConfigModelException("Comparison operator is invalid.")
        };

        logBuilder?
            .NewLine().Append($"Prerequisite flag evaluation result: '{prerequisiteFlagValue}'.")
            .NewLine("Condition (")
                .AppendPrerequisiteFlagCondition(condition, context.Settings)
                .Append(") evaluates to ").AppendConditionResult(result).Append(".")
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

        var segment = condition.segment ?? throw new InvalidConfigModelException("Segment reference is invalid.");

        if (segment.name is not { Length: > 0 })
        {
            throw new InvalidConfigModelException("Segment name is missing.");
        }

        logBuilder?
            .NewLine("(")
            .IncreaseIndent()
            .NewLine().Append($"Evaluating segment '{segment.name}':");

        var segmentResult = EvaluateConditions(segment.ConditionsOrEmpty, targetingRule: null, contextSalt: segment.name, ref context, out error);

        var comparator = condition.comparator;
        var result = error is null && comparator switch
        {
            SegmentComparator.IsIn => segmentResult,
            SegmentComparator.IsNotIn => !segmentResult,
            _ => throw new InvalidConfigModelException("Comparison operator is invalid.")
        };

        if (logBuilder is not null)
        {
            logBuilder.NewLine("Segment evaluation result: ");
            (error is null
                ? logBuilder.Append($"User {(segmentResult ? SegmentComparator.IsIn : SegmentComparator.IsNotIn).ToDisplayText()}")
                : logBuilder.Append(error))
                .Append(".");

            logBuilder.NewLine("Condition (").AppendSegmentCondition(condition).Append(")");
            (error is null
                ? logBuilder.Append(" evaluates to ").AppendConditionResult(result)
                : logBuilder.Append(" failed to evaluate"))
                .Append(".");

            logBuilder
                .DecreaseIndent()
                .NewLine(")");
        }

        return result;
    }

    private static byte[] HashPercentageOptionsAttribute(string contextKey, string attributeValue)
    {
        var contextKeyByteCount = Encoding.UTF8.GetByteCount(contextKey);
        var attributeValueByteCount = Encoding.UTF8.GetByteCount(attributeValue);
        var totalByteCount = contextKeyByteCount + attributeValueByteCount;

        var bytes = ArrayPool<byte>.Shared.Rent(totalByteCount);
        try
        {
            Encoding.UTF8.GetBytes(contextKey, 0, contextKey.Length, bytes, 0);
            Encoding.UTF8.GetBytes(attributeValue, 0, attributeValue.Length, bytes, contextKeyByteCount);

            return new ArraySegment<byte>(bytes, 0, totalByteCount).Sha1();
        }
        finally { ArrayPool<byte>.Shared.Return(bytes); }
    }

    private static byte[] HashComparisonValue(string value, string configJsonSalt, string contextSalt)
    {
        var valueByteCount = Encoding.UTF8.GetByteCount(value);
        var configJsonSaltByteCount = Encoding.UTF8.GetByteCount(configJsonSalt);
        var contextSaltByteCount = Encoding.UTF8.GetByteCount(contextSalt);
        var totalByteCount = valueByteCount + configJsonSaltByteCount + contextSaltByteCount;

        var bytes = ArrayPool<byte>.Shared.Rent(totalByteCount);
        try
        {
            Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
            Encoding.UTF8.GetBytes(configJsonSalt, 0, configJsonSalt.Length, bytes, valueByteCount);
            Encoding.UTF8.GetBytes(contextSalt, 0, contextSalt.Length, bytes, valueByteCount + configJsonSaltByteCount);

            return new ArraySegment<byte>(bytes, 0, totalByteCount).Sha256();
        }
        finally { ArrayPool<byte>.Shared.Return(bytes); }
    }

    private static byte[] HashComparisonValue(ReadOnlySpan<byte> valueUtf8, string configJsonSalt, string contextSalt)
    {
        var valueByteCount = valueUtf8.Length;
        var configJsonSaltByteCount = Encoding.UTF8.GetByteCount(configJsonSalt);
        var contextSaltByteCount = Encoding.UTF8.GetByteCount(contextSalt);
        var totalByteCount = valueByteCount + configJsonSaltByteCount + contextSaltByteCount;

        var bytes = ArrayPool<byte>.Shared.Rent(totalByteCount);
        try
        {
            valueUtf8.CopyTo(bytes);
            Encoding.UTF8.GetBytes(configJsonSalt, 0, configJsonSalt.Length, bytes, valueByteCount);
            Encoding.UTF8.GetBytes(contextSalt, 0, contextSalt.Length, bytes, valueByteCount + configJsonSaltByteCount);

            return new ArraySegment<byte>(bytes, 0, totalByteCount).Sha256();
        }
        finally { ArrayPool<byte>.Shared.Return(bytes); }
    }

    private static string EnsureConfigJsonSalt([NotNull] string? value)
    {
        return value ?? throw new InvalidConfigModelException("Config JSON salt is missing.");
    }

    [return: NotNull]
    private static T EnsureComparisonValue<T>([NotNull] T? value)
    {
        return value ?? throw new InvalidConfigModelException("Comparison value is missing or invalid.");
    }

    private static string UserAttributeValueToString(object attributeValue)
    {
        if (attributeValue is string text)
        {
            return text;
        }
        else if (attributeValue is string[] stringArray)
        {
            return SerializationHelper.SerializeStringArray(stringArray, unescapeAstral: true);
        }
        else if (attributeValue.TryConvertNumericToDouble(out var number))
        {
            var format = Math.Abs(number) is >= 1e-6 and < 1e21
                ? "0.#################"
                : "0.#################e+0";
            return number.ToString(format, CultureInfo.InvariantCulture);
        }
        else if (attributeValue.TryConvertDateTimeToDateTimeOffset(out var dateTimeOffset))
        {
            var unixTimeSeconds = DateTimeUtils.ToUnixTimeMilliseconds(dateTimeOffset.UtcDateTime) / 1000.0;
            return unixTimeSeconds.ToString(CultureInfo.InvariantCulture);
        }

        return Convert.ToString(attributeValue, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private string GetUserAttributeValueAsText(string attributeName, object attributeValue, UserCondition condition, string key)
    {
        if (attributeValue is string text)
        {
            return text;
        }

        text = UserAttributeValueToString(attributeValue);
        this.logger.UserObjectAttributeIsAutoConverted(condition, key, attributeName, text);
        return text;
    }

    private SemVersion? GetUserAttributeValueAsSemVer(string attributeName, object attributeValue, UserCondition condition, string key, out string? error)
    {
        if (attributeValue is SemVersion version)
        {
            error = null;
            return version;
        }
        else if (attributeValue is Version clrVersion)
        {
            error = null;
            return new SemVersion(clrVersion);
        }
        else if (attributeValue is string text && SemVersion.TryParse(text.Trim(), out version!, strict: true))
        {
            error = null;
            return version;
        }

        error = HandleInvalidUserAttribute(condition, key, attributeName, $"'{attributeValue}' is not a valid semantic version");
        return default;
    }

    private double GetUserAttributeValueAsNumber(string attributeName, object attributeValue, UserCondition condition, string key, out string? error)
    {
        if (attributeValue.TryConvertNumericToDouble(out var number)
            || attributeValue is string text && double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            error = null;
            return number;
        }

        error = HandleInvalidUserAttribute(condition, key, attributeName, $"'{attributeValue}' is not a valid decimal number");
        return default;
    }

    private double GetUserAttributeValueAsUnixTimeSeconds(string attributeName, object attributeValue, UserCondition condition, string key, out string? error)
    {
        if (attributeValue.TryConvertDateTimeToDateTimeOffset(out var dateTimeOffset))
        {
            error = null;
            return DateTimeUtils.ToUnixTimeMilliseconds(dateTimeOffset.UtcDateTime) / 1000.0;
        }
        else if (attributeValue.TryConvertNumericToDouble(out var number)
            || attributeValue is string text && double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
        {
            error = null;
            return number;
        }

        error = HandleInvalidUserAttribute(condition, key, attributeName, $"'{attributeValue}' is not a valid Unix timestamp (number of seconds elapsed since Unix epoch)");
        return default;
    }

    private string[]? GetUserAttributeValueAsStringArray(string attributeName, object attributeValue, UserCondition condition, string key, out string? error)
    {
        if (attributeValue is string[] stringArray
            || attributeValue is string json && (stringArray = SerializationHelper.DeserializeStringArray(json.AsSpan(), throwOnError: false)!) is not null)
        {
            if (!Array.Exists(stringArray, item => item is null))
            {
                error = null;
                return stringArray;
            }
        }

        error = HandleInvalidUserAttribute(condition, key, attributeName, $"'{attributeValue}' is not a valid string array");
        return default;
    }

    private string HandleInvalidUserAttribute(UserCondition condition, string key, string attributeName, string reason)
    {
        this.logger.UserObjectAttributeIsInvalid(condition, key, reason, attributeName);
        return string.Format(CultureInfo.InvariantCulture, InvalidUserAttributeError, attributeName, reason);
    }
}
