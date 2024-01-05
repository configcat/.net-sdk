using System;
using ConfigCat.Client.ConfigService;

namespace ConfigCat.Client;

internal static partial class LoggerExtensions
{
    #region Common error messages (1000-1999)

    public static FormattableLogMessage ConfigJsonIsNotPresent(this LoggerWrapper logger, string defaultReturnValue) => logger.LogInterpolated(
        LogLevel.Error, 1000,
        $"Config JSON is not present. Returning {defaultReturnValue}.",
        "DEFAULT_RETURN_VALUE");

    public static FormattableLogMessage ConfigJsonIsNotPresent(this LoggerWrapper logger, string key, string defaultParamName, object? defaultParamValue) => logger.LogInterpolated(
        LogLevel.Error, 1000,
        $"Config JSON is not present when evaluating setting '{key}'. Returning the `{defaultParamName}` parameter that you specified in your application: '{defaultParamValue}'.",
        "KEY", "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE");

    public static FormattableLogMessage SettingEvaluationFailedDueToMissingKey(this LoggerWrapper logger, string key, string defaultParamName, object? defaultParamValue, string availableKeys) => logger.LogInterpolated(
        LogLevel.Error, 1001,
        $"Failed to evaluate setting '{key}' (the key was not found in config JSON). Returning the `{defaultParamName}` parameter that you specified in your application: '{defaultParamValue}'. Available keys: [{availableKeys}].",
        "KEY", "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE", "AVAILABLE_KEYS");

    public static FormattableLogMessage SettingEvaluationError(this LoggerWrapper logger, string methodName, string defaultReturnValue, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1002, ex,
        $"Error occurred in the `{methodName}` method. Returning {defaultReturnValue}.",
        "METHOD_NAME", "DEFAULT_RETURN_VALUE");

    public static FormattableLogMessage SettingEvaluationError(this LoggerWrapper logger, string methodName, string key, string defaultParamName, object? defaultParamValue, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1002, ex,
        $"Error occurred in the `{methodName}` method while evaluating setting '{key}'. Returning the `{defaultParamName}` parameter that you specified in your application: '{defaultParamValue}'.",
        "METHOD_NAME", "KEY", "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE");

    public static FormattableLogMessage ForceRefreshError(this LoggerWrapper logger, string methodName, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1003, ex,
        $"Error occurred in the `{methodName}` method.",
        "METHOD_NAME");

    public static FormattableLogMessage FetchFailedDueToInvalidSdkKey(this LoggerWrapper logger) => logger.Log(
        LogLevel.Error, 1100,
        "Your SDK Key seems to be wrong. You can find the valid SDK Key at https://app.configcat.com/sdkkey");

    public static FormattableLogMessage FetchFailedDueToUnexpectedHttpResponse(this LoggerWrapper logger, int statusCode, string? reasonPhrase) => logger.LogInterpolated(
        LogLevel.Error, 1101,
        $"Unexpected HTTP response was received while trying to fetch config JSON: {statusCode} {reasonPhrase}",
        "STATUS_CODE", "REASON_PHRASE");

    public static FormattableLogMessage FetchFailedDueToRequestTimeout(this LoggerWrapper logger, TimeSpan timeout, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1102, ex,
         $"Request timed out while trying to fetch config JSON. Timeout value: {timeout}",
        "TIMEOUT");

    public static FormattableLogMessage FetchFailedDueToUnexpectedError(this LoggerWrapper logger, Exception ex) => logger.Log(
        LogLevel.Error, 1103, ex,
        "Unexpected error occurred while trying to fetch config JSON.");

    public static FormattableLogMessage FetchFailedDueToRedirectLoop(this LoggerWrapper logger) => logger.Log(
        LogLevel.Error, 1104,
        "Redirection loop encountered while trying to fetch config JSON. Please contact us at https://configcat.com/support/");

    public static FormattableLogMessage FetchReceived200WithInvalidBody(this LoggerWrapper logger, Exception? ex) => logger.Log(
        LogLevel.Error, 1105, ex,
        "Fetching config JSON was successful but the HTTP response content was invalid.");

    public static FormattableLogMessage FetchReceived304WhenLocalCacheIsEmpty(this LoggerWrapper logger, int statusCode, string? reasonPhrase) => logger.LogInterpolated(
        LogLevel.Error, 1106,
        $"Unexpected HTTP response was received when no config JSON is cached locally: {statusCode} {reasonPhrase}",
        "STATUS_CODE", "REASON_PHRASE");

    public static FormattableLogMessage AutoPollConfigServiceErrorDuringPolling(this LoggerWrapper logger, Exception ex) => logger.Log(
        LogLevel.Error, 1200, ex,
        "Error occurred during auto polling.");

    public static FormattableLogMessage LocalFileDataSourceDoesNotExist(this LoggerWrapper logger, string filePath) => logger.LogInterpolated(
        LogLevel.Error, 1300,
        $"Cannot find the local config file '{filePath}'. This is a path that your application provided to the ConfigCat SDK by passing it to the `FlagOverrides.LocalFile()` method. Read more: https://configcat.com/docs/sdk-reference/dotnet/#json-file",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceErrorDuringWatching(this LoggerWrapper logger, string filePath, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1301, ex,
        $"Error occurred while watching the local config file '{filePath}' for changes.",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceFailedToReadFile(this LoggerWrapper logger, string filePath, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1302, ex,
        $"Failed to read the local config file '{filePath}'.",
        "FILE_PATH");

    #endregion

    #region SDK-specific error messages (2000-2999)

    public static FormattableLogMessage EstablishingSecureConnectionFailed(this LoggerWrapper logger, Exception ex) => logger.Log(
        LogLevel.Error, 2100, ex,
        "Secure connection could not be established. Please make sure that your application is enabled to use TLS 1.2+. For more information, see https://stackoverflow.com/a/58195987/8656352");

    public static FormattableLogMessage ConfigServiceCacheReadError(this LoggerWrapper logger, Exception ex) => logger.Log(
        LogLevel.Error, 2200, ex,
        "Error occurred while reading the cache.");

    public static FormattableLogMessage ConfigServiceCacheWriteError(this LoggerWrapper logger, Exception ex) => logger.Log(
        LogLevel.Error, 2201, ex,
        "Error occurred while writing the cache.");

    #endregion

    #region Common warning messages (3000-3999)

    public static FormattableLogMessage ClientIsAlreadyCreated(this LoggerWrapper logger, string sdkKey) => logger.LogInterpolated(
        LogLevel.Warning, 3000,
        $"There is an existing client instance for the specified SDK Key. No new client instance will be created and the specified configuration action is ignored. Returning the existing client instance. SDK Key: '{sdkKey}'.",
        "SDK_KEY");

    public static FormattableLogMessage UserObjectIsMissing(this LoggerWrapper logger, string key) => logger.LogInterpolated(
        LogLevel.Warning, 3001,
        $"Cannot evaluate targeting rules and % options for setting '{key}' (User Object is missing). You should pass a User Object to the evaluation methods like `GetValue()`/`GetValueAsync()` in order to make targeting work properly. Read more: https://configcat.com/docs/advanced/user-object/",
        "KEY");

    public static FormattableLogMessage DataGovernanceIsOutOfSync(this LoggerWrapper logger) => logger.Log(
        LogLevel.Warning, 3002,
        "The `dataGovernance` parameter specified at the client initialization is not in sync with the preferences on the ConfigCat Dashboard. Read more: https://configcat.com/docs/advanced/data-governance/");

    public static FormattableLogMessage UserObjectAttributeIsMissing(this LoggerWrapper logger, string key, string attributeName) => logger.LogInterpolated(
        LogLevel.Warning, 3003,
        $"Cannot evaluate % options for setting '{key}' (the User.{attributeName} attribute is missing). You should set the User.{attributeName} attribute in order to make targeting work properly. Read more: https://configcat.com/docs/advanced/user-object/",
        "KEY", "ATTRIBUTE_NAME", "ATTRIBUTE_NAME");

    public static FormattableLogMessage UserObjectAttributeIsMissing(this LoggerWrapper logger, string condition, string key, string attributeName) => logger.LogInterpolated(
        LogLevel.Warning, 3003,
        $"Cannot evaluate condition ({condition}) for setting '{key}' (the User.{attributeName} attribute is missing). You should set the User.{attributeName} attribute in order to make targeting work properly. Read more: https://configcat.com/docs/advanced/user-object/",
        "CONDITION", "KEY", "ATTRIBUTE_NAME", "ATTRIBUTE_NAME");

    public static FormattableLogMessage UserObjectAttributeIsInvalid(this LoggerWrapper logger, string condition, string key, string reason, string attributeName) => logger.LogInterpolated(
        LogLevel.Warning, 3004,
        $"Cannot evaluate condition ({condition}) for setting '{key}' ({reason}). Please check the User.{attributeName} attribute and make sure that its value corresponds to the comparison operator.",
        "CONDITION", "KEY", "REASON", "ATTRIBUTE_NAME");

    public static FormattableLogMessage UserObjectAttributeIsAutoConverted(this LoggerWrapper logger, string condition, string key, string attributeName, string attributeValue) => logger.LogInterpolated(
        LogLevel.Warning, 3005,
        $"Evaluation of condition ({condition}) for setting '{key}' may not produce the expected result (the User.{attributeName} attribute is not a string value, thus it was automatically converted to the string value '{attributeValue}'). Please make sure that using a non-string value was intended.",
        "CONDITION", "KEY", "ATTRIBUTE_NAME", "ATTRIBUTE_VALUE");

    public static FormattableLogMessage ConfigServiceCannotInitiateHttpCalls(this LoggerWrapper logger) => logger.Log(
        LogLevel.Warning, 3200,
        "Client is in offline mode, it cannot initiate HTTP calls.");

    public static FormattableLogMessage ConfigServiceMethodHasNoEffectDueToDisposedClient(this LoggerWrapper logger, string methodName) => logger.LogInterpolated(
        LogLevel.Warning, 3201,
        $"The client object is already disposed, thus `{methodName}()` has no effect.",
        "METHOD_NAME");

    public static FormattableLogMessage ConfigServiceMethodHasNoEffectDueToOverrideBehavior(this LoggerWrapper logger, string overrideBehavior, string methodName) => logger.LogInterpolated(
        LogLevel.Warning, 3202,
        $"Client is configured to use the `{overrideBehavior}` override behavior, thus `{methodName}()` has no effect.",
        "OVERRIDE_BEHAVIOR", "METHOD_NAME");

    #endregion

    #region SDK-specific warning messages (4000-4999)

    #endregion

    #region Common info messages (5000-5999)

    public static FormattableLogMessage SettingEvaluated(this LoggerWrapper logger, string evaluateLog) => logger.LogInterpolated(
        LogLevel.Info, 5000,
        $"{evaluateLog}",
        "EVALUATE_LOG");

    public static FormattableLogMessage ConfigServiceStatusChanged(this LoggerWrapper logger, ConfigServiceBase.Status status) => logger.LogInterpolated(
        LogLevel.Info, 5200,
        $"Switched to {status.ToString().ToUpperInvariant()} mode.",
        "MODE");

    public static FormattableLogMessage LocalFileDataSourceStartsWatchingFile(this LoggerWrapper logger, string filePath) => logger.LogInterpolated(
        LogLevel.Info, 5300,
        $"Started watching the local config file '{filePath}' for changes.",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceReloadsFile(this LoggerWrapper logger, string filePath) => logger.LogInterpolated(
        LogLevel.Info, 5301,
        $"Reloading the local config file '{filePath}'...",
        "FILE_PATH");

    #endregion

    #region SDK-specific info messages (6000-6999)

    #endregion
}
