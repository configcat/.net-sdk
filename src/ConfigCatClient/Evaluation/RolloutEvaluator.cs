using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using ConfigCat.Client.Utils;
using ConfigCat.Client.Versioning;

namespace ConfigCat.Client.Evaluation;

internal sealed class RolloutEvaluator : IRolloutEvaluator
{
    private const string MissingUserObjectError = "cannot evaluate, User Object is missing";

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
            returnValue = result.SelectedValue.Value.GetValue(context.Setting.SettingType, throwIfInvalid: false) ?? EvaluateLogHelper.InvalidValuePlaceholder;
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
            var targetingRule = targetingRules[i]; // TODO: error handling - what to do when item is null?

            var conditions = targetingRule.Conditions;

            const string targetingRuleIgnoredMessage = "The current targeting rule is ignored and the evaluation continues with the next rule.";

            // TODO: error handling - condition.GetCondition() - what to do when the condition is invalid (not available/multiple values specified)?
            if (!TryEvaluateConditions(conditions, static condition => condition.GetCondition()!, targetingRule, contextSalt: context.Key, ref context, out var isMatch))
            {
                logBuilder?
                    .IncreaseIndent()
                    .NewLine(targetingRuleIgnoredMessage)
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
                // TODO: error handling - percentage options are expected but the list of percentage options are missing or both of simple value and percentage options are specified
                throw new InvalidOperationException();
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
                    .NewLine(targetingRuleIgnoredMessage)
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
            // TODO: error handling - how to handle when percentageOptionsAttributeName is empty?

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

        if (percentageOptions.Sum(option => option.Percentage) != 100)
        {
            // TODO: error handling - sum of percentage options is not 100
            throw new InvalidOperationException();
        }

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
            var percentageOption = percentageOptions[i]; // TODO: error handling - what to do when item is null?

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

        throw new InvalidOperationException();  // execution should never get here
    }

    private bool TryEvaluateConditions<TCondition>(TCondition[] conditions, Func<TCondition, ICondition> getCondition, TargetingRule? targetingRule,
        string contextSalt, ref EvaluateContext context, out bool result)
    {
        result = true;

        var logBuilder = context.LogBuilder;
        string? error = null;
        var newLineBeforeThen = false;

        logBuilder?.NewLine("- ");

        for (var i = 0; i < conditions.Length; i++)
        {
            var condition = getCondition(conditions[i]); // TODO: error handling - what to do when item is null?

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
                case ComparisonCondition comparisonCondition:
                    conditionResult = EvaluateComparisonCondition(comparisonCondition, contextSalt, ref context, out error);
                    newLineBeforeThen = conditions.Length > 1;
                    break;

                case PrerequisiteFlagCondition:
                    throw new NotImplementedException(); // TODO

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

    private bool EvaluateComparisonCondition(ComparisonCondition condition, string contextSalt, ref EvaluateContext context, out string? error)
    {
        error = null;
        bool canEvaluate;

        var logBuilder = context.LogBuilder;

        var userAttributeName = condition.ComparisonAttribute;
        userAttributeName = userAttributeName is { Length: > 0 } ? userAttributeName : null;
        string? userAttributeValue = null;

        if (context.User is null)
        {
            if (!context.IsMissingUserObjectLogged)
            {
                this.logger.UserObjectIsMissing(context.Key);
                context.IsMissingUserObjectLogged = true;
            }

            error = MissingUserObjectError;
            canEvaluate = false;
        }
        else if (userAttributeName is null)
        {
            // TODO: error handling - comparison attribute is not specified
            canEvaluate = false;
        }
        else
        {
            canEvaluate = context.UserAttributes!.TryGetValue(userAttributeName, out userAttributeValue) && userAttributeValue.Length > 0;
        }

        // TODO: revise when to trim userAttributeValue/comparisonValue

        var comparator = condition.Comparator;
        switch (comparator)
        {
            case Comparator.SensitiveOneOf:
            case Comparator.SensitiveNotOneOf:
                logBuilder?.AppendComparisonCondition(userAttributeName, comparator, condition.StringListValue, isSensitive: true);
                // TODO: error handling - missing configJsonSalt
                return canEvaluate
                    && EvaluateSensitiveOneOf(userAttributeValue!, condition.StringListValue, context.Setting.ConfigJsonSalt!, contextSalt, negate: comparator == Comparator.SensitiveNotOneOf);

            case Comparator.Contains:
            case Comparator.NotContains:
                logBuilder?.AppendComparisonCondition(userAttributeName, comparator, condition.StringListValue);
                return canEvaluate
                    && EvaluateContains(userAttributeValue!, condition.StringListValue, negate: comparator == Comparator.NotContains);

            case Comparator.SemVerOneOf:
            case Comparator.SemVerNotOneOf:
                logBuilder?.AppendComparisonCondition(userAttributeName, comparator, condition.StringListValue);
                return canEvaluate
                    && SemVersion.TryParse(userAttributeValue!.Trim(), out var version, strict: true)
                    && EvaluateSemVerOneOf(version, condition.StringListValue, negate: comparator == Comparator.SemVerNotOneOf);

            case Comparator.SemVerLessThan:
            case Comparator.SemVerLessThanEqual:
            case Comparator.SemVerGreaterThan:
            case Comparator.SemVerGreaterThanEqual:
                logBuilder?.AppendComparisonCondition(userAttributeName, comparator, condition.StringValue);
                return canEvaluate
                    && SemVersion.TryParse(userAttributeValue!.Trim(), out version, strict: true)
                    && EvaluateSemVerRelation(version, comparator, condition.StringValue);

            case Comparator.NumberEqual:
            case Comparator.NumberNotEqual:
            case Comparator.NumberLessThan:
            case Comparator.NumberLessThanEqual:
            case Comparator.NumberGreaterThan:
            case Comparator.NumberGreaterThanEqual:
                logBuilder?.AppendComparisonCondition(userAttributeName, comparator, condition.DoubleValue);
                return canEvaluate
                    && double.TryParse(userAttributeValue!.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    && EvaluateNumberRelation(number, condition.Comparator, condition.DoubleValue);

            case Comparator.DateTimeBefore:
            case Comparator.DateTimeAfter:
            case Comparator.SensitiveTextEquals:
            case Comparator.SensitiveTextNotEquals:
            case Comparator.SensitiveTextStartsWith:
            case Comparator.SensitiveTextEndsWith:
            case Comparator.SensitiveArrayContains:
            case Comparator.SensitiveArrayNotContains:
                throw new NotImplementedException(); // TODO

            default:
                logBuilder?.AppendComparisonCondition(userAttributeName, comparator, condition.GetComparisonValue(throwIfInvalid: false));
                // TODO: error handling - comparator was not set
                throw new InvalidOperationException();
        }
    }

    private static bool EvaluateSensitiveOneOf(string text, string[]? comparisonValues, string configJsonSalt, string contextSalt, bool negate)
    {
        if (comparisonValues is null)
        {
            // TODO: error handling - what to do when comparison value is invalid (not available/multiple values specified)?
            return false;
        }

        var hash = HashComparisonValue(text, configJsonSalt, contextSalt);

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            if (hash.Equals(hexString: comparisonValues[i].AsSpan().Trim()))  // TODO: error handling - what to do when item is null?
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateContains(string text, string[]? comparisonValues, bool negate)
    {
        if (comparisonValues is null)
        {
            // TODO: error handling - what to do when comparison value is invalid (not available/multiple values specified)?
            return false;
        }

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            if (text.Contains(comparisonValues[i]))  // TODO: error handling - what to do when item is null?
            {
                return !negate;
            }
        }

        return negate;
    }

    private static bool EvaluateSemVerOneOf(SemVersion version, string[]? comparisonValues, bool negate)
    {
        if (comparisonValues is null)
        {
            // TODO: error handling - what to do when comparison value is invalid (not available/multiple values specified)?
            return false;
        }

        var result = false;

        for (var i = 0; i < comparisonValues.Length; i++)
        {
            var item = comparisonValues[i]; // TODO: error handling - what to do when item is null?

            // NOTE: Previous versions of the evaluation algorithm ignore empty comparison values
            // so we keep this behavior for backward compatibility.
            if (item.Length == 0)
            {
                continue;
            }

            // TODO: error handling - what to do when item is invalid?
            if (!SemVersion.TryParse(item, out var version2, strict: true))
            {
                return false;
            }

            if (!result && version.PrecedenceMatches(version2))
            {
                // NOTE: Previous versions of the evaluation algorithm require that
                // all the comparison values are empty or valid, that is, we can't stop when finding a match,
                // so we keep this behavior for backward compatibility.
                result = true;
            }
        }

        return result ^ negate;
    }

    private static bool EvaluateSemVerRelation(SemVersion version, Comparator comparator, string? comparisonValue)
    {
        if (comparisonValue is null)
        {
            // TODO: error handling - what to do when comparison value is invalid (not available/multiple values specified)?
            return false;
        }

        // TODO: should we trim comparisonValue?
        if (!SemVersion.TryParse(comparisonValue.Trim(), out var version2, strict: true)) // TODO: error handling - what to do when item is invalid?
        {
            return false;
        }

        var comparisonResult = version.CompareByPrecedence(version2);

        return comparator switch
        {
            Comparator.SemVerLessThan => comparisonResult < 0,
            Comparator.SemVerLessThanEqual => comparisonResult <= 0,
            Comparator.SemVerGreaterThan => comparisonResult > 0,
            Comparator.SemVerGreaterThanEqual => comparisonResult >= 0,
            _ => throw new ArgumentOutOfRangeException(nameof(comparator), comparator, null)
        };
    }

    private static bool EvaluateNumberRelation(double number, Comparator comparator, double? comparisonValue)
    {
        if (comparisonValue is not { } number2)
        {
            // TODO: error handling - what to do when comparison value is invalid (not available/multiple values specified)?
            return false;
        }

        return comparator switch
        {
            Comparator.NumberEqual => number == number2,
            Comparator.NumberNotEqual => number != number2,
            Comparator.NumberLessThan => number < number2,
            Comparator.NumberLessThanEqual => number <= number2,
            Comparator.NumberGreaterThan => number > number2,
            Comparator.NumberGreaterThanEqual => number >= number2,
            _ => throw new ArgumentOutOfRangeException(nameof(comparator), comparator, null)
        };
    }

    private bool EvaluateSegmentCondition(SegmentCondition condition, ref EvaluateContext context, out string? error)
    {
        error = null;

        var logBuilder = context.LogBuilder;

        var comparator = condition.Comparator;
        var segment = condition.Segment;

        logBuilder?.AppendSegmentCondition(comparator, segment);

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
        else if (segment is null)
        {
            // TODO: error handling - segment reference is invalid
            return false;
        }
        else if (segment.Name is not { Length: > 0 })
        {
            // TODO: error handling - segment name is not specified
            return false;
        }

        logBuilder?
            .NewLine("(")
            .IncreaseIndent()
            .NewLine().Append($"Evaluating segment '{segment.Name}':");

        var success = TryEvaluateConditions(segment.Conditions, static condition => condition, targetingRule: null, contextSalt: segment.Name, ref context, out var segmentResult);
        Debug.Assert(success, "Unexpected failure when evaluating segment conditions.");

        var result = comparator switch
        {
            SegmentComparator.IsIn => segmentResult,
            SegmentComparator.IsNotIn => !segmentResult,
            _ => throw new InvalidOperationException(), // TODO: error handling - comparator was not set
        };

        logBuilder?
            .NewLine().Append($"Segment evaluation result: User {(segmentResult ? SegmentComparator.IsIn : SegmentComparator.IsNotIn).ToDisplayText()}.")
            .NewLine("Condition (")
                .AppendSegmentCondition(comparator, segment)
                .Append(") evaluates to ").AppendEvaluationResult(result).Append(".")
            .DecreaseIndent()
            .NewLine(")");

        return result;
    }

    private static byte[] HashComparisonValue(string value, string configJsonSalt, string contextSalt)
    {
        return (value + configJsonSalt + contextSalt).Sha256();
    }
}
