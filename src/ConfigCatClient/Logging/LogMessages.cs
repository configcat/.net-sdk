using System;
using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluation;

namespace ConfigCat.Client;

internal static partial class LoggerExtensions
{
    #region Common error messages (1000-1999)

    public static FormattableLogMessage ConfigJsonIsNotPresent(this IConfigCatLogger logger) => logger.Log(
        LogLevel.Error, 1000,
        "Config JSON is not present.");

    public static FormattableLogMessage ConfigJsonIsNotPresent(this IConfigCatLogger logger, string defaultParamName, object defaultParamValue) => logger.LogInterpolated(
        LogLevel.Error, 1000,
        $"Config JSON is not present. Returning the `{defaultParamName}` parameter that you specified in your application: '{defaultParamValue}'.",
        "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE");

    public static FormattableLogMessage SettingEvaluationFailedDueToMissingKey(this IConfigCatLogger logger, string key, string defaultParamName, object defaultParamValue, string availableKeys) => logger.LogInterpolated(
        LogLevel.Error, 1001,
        $"Failed to evaluate setting '{key}' (the key was not found in config JSON). Returning the `{defaultParamName}` parameter that you specified in your application: '{defaultParamValue}'. Available keys: {availableKeys}.",
        "KEY", "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE", "AVAILABLE_KEYS");

    public static FormattableLogMessage SettingEvaluationError(this IConfigCatLogger logger, string methodName, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1002, ex,
        $"Error occurred in the `{methodName}` method.",
        "METHOD_NAME");

    public static FormattableLogMessage ForceRefreshError(this IConfigCatLogger logger, string methodName, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1003, ex,
        $"Error occurred in the `{methodName}` method.",
        "METHOD_NAME");

    public static FormattableLogMessage FetchFailedDueToInvalidSdkKey(this IConfigCatLogger logger) => logger.Log(
        LogLevel.Error, 1100,
        "Your SDK Key seems to be wrong. You can find the valid SDK Key at https://app.configcat.com/sdkkey");

    public static FormattableLogMessage FetchFailedDueToUnexpectedHttpResponse(this IConfigCatLogger logger, int statusCode, string reasonPhrase) => logger.LogInterpolated(
        LogLevel.Error, 1101,
        $"Unexpected HTTP response was received while trying to fetch config JSON: {statusCode} {reasonPhrase}",
        "STATUS_CODE", "REASON_PHRASE");

    public static FormattableLogMessage FetchFailedDueToRequestTimeout(this IConfigCatLogger logger, TimeSpan timeout, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1102, ex,
         $"Request timed out while trying to fetch config JSON. Timeout value: {timeout}",
        "TIMEOUT");

    public static FormattableLogMessage FetchFailedDueToUnexpectedError(this IConfigCatLogger logger, Exception ex) => logger.Log(
        LogLevel.Error, 1103, ex,
        "Unexpected error occurred while trying to fetch config JSON.");

    public static FormattableLogMessage FetchFailedDueToRedirectLoop(this IConfigCatLogger logger) => logger.Log(
        LogLevel.Error, 1104,
        "Redirection loop encountered while trying to fetch config JSON. Please contact us at https://configcat.com/support/");

    public static FormattableLogMessage AutoPollConfigServiceErrorDuringPolling(this IConfigCatLogger logger, Exception ex) => logger.Log(
        LogLevel.Error, 1200, ex,
        "Error occurred during auto polling.");

    public static FormattableLogMessage LocalFileDataSourceDoesNotExist(this IConfigCatLogger logger, string filePath) => logger.LogInterpolated(
        LogLevel.Error, 1300,
        $"Cannot find the local config file '{filePath}'. This is a path that your application provided to the ConfigCat SDK by passing it to the `FlagOverrides.LocalFile()` method. Read more: https://configcat.com/docs/sdk-reference/dotnet/#json-file",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceErrorDuringWatching(this IConfigCatLogger logger, string filePath, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1301, ex,
        $"Error occurred while watching the local config file '{filePath}' for changes.",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceFailedToReadFile(this IConfigCatLogger logger, string filePath, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1302, ex,
        $"Failed to read the local config file '{filePath}'.",
        "FILE_PATH");

    #endregion

    #region SDK-specific error messages (2000-2999)

    public static FormattableLogMessage EstablishingSecureConnectionFailed(this IConfigCatLogger logger, Exception ex) => logger.Log(
        LogLevel.Error, 2100, ex,
        "Secure connection could not be established. Please make sure that your application is enabled to use TLS 1.2+. For more information, see https://stackoverflow.com/a/58195987/8656352");

    #endregion

    #region Common warning messages (3000-3999)

    public static FormattableLogMessage ClientIsAlreadyCreated(this IConfigCatLogger logger, string sdkKey) => logger.LogInterpolated(
        LogLevel.Warning, 3000,
        $"There is an existing client instance for the specified SDK Key. No new client instance will be created and the specified configuration action is ignored. Returning the existing client instance. SDK Key: '{sdkKey}'.",
        "SDK_KEY");

    public static FormattableLogMessage TargetingIsNotPossible(this IConfigCatLogger logger, string key) => logger.LogInterpolated(
        LogLevel.Warning, 3001,
        $"Cannot evaluate targeting rules and % options for setting '{key}' (User Object is missing). You should pass a User Object to the evaluation methods like `GetValue()`/`GetValueAsync()` in order to make targeting work properly. Read more: https://configcat.com/docs/advanced/user-object/",
        "KEY");

    public static FormattableLogMessage DataGovernanceIsOutOfSync(this IConfigCatLogger logger) => logger.Log(
        LogLevel.Warning, 3002,
        "The `dataGovernance` parameter specified in the client initialization is not in sync with the preferences on the ConfigCat Dashboard. Read more: https://configcat.com/docs/advanced/data-governance/");

    public static FormattableLogMessage FetchReceived200WithInvalidBody(this IConfigCatLogger logger) => logger.Log(
        LogLevel.Warning, 3100,
        "Fetching config JSON was successful but the HTTP response content was invalid.");

    public static FormattableLogMessage FetchReceived304WhenLocalCacheIsEmpty(this IConfigCatLogger logger, int statusCode, string reasonPhrase) => logger.LogInterpolated(
        LogLevel.Warning, 3101,
        $"Unexpected HTTP response was received when no config JSON is cached locally: {statusCode} {reasonPhrase}",
        "STATUS_CODE", "REASON_PHRASE");

    public static FormattableLogMessage ConfigServiceCannotInitiateHttpCalls(this IConfigCatLogger logger) => logger.Log(
        LogLevel.Warning, 3200,
        "Client is in offline mode, it cannot initiate HTTP calls.");

    public static FormattableLogMessage ConfigServiceMethodHasNoEffectDueToDisposedClient(this IConfigCatLogger logger, string methodName) => logger.LogInterpolated(
        LogLevel.Warning, 3201,
        $"The client object is already disposed, thus `{methodName}()` has no effect.",
        "METHOD_NAME");

    public static FormattableLogMessage ConfigServiceMethodHasNoEffectDueToOverrideBehavior(this IConfigCatLogger logger, string overrideBehavior, string methodName) => logger.LogInterpolated(
        LogLevel.Warning, 3202,
        $"Client is configured to use the `{overrideBehavior}` override behavior, thus `{methodName}()` has no effect.",
        "OVERRIDE_BEHAVIOR", "METHOD_NAME");

    #endregion

    #region SDK-specific warning messages (4000-4999)

    #endregion

    #region Common info messages (5000-5999)

    public static FormattableLogMessage SettingEvaluated(this IConfigCatLogger logger, EvaluateLogger<string> evaluateLog) => logger.LogInterpolated(
        LogLevel.Info, 5000,
        $"{evaluateLog}",
        "EVALUATE_LOG");

    public static FormattableLogMessage ConfigServiceStatusChanged(this IConfigCatLogger logger, ConfigServiceBase.Status status) => logger.LogInterpolated(
        LogLevel.Info, 5200,
        $"Switched to {status.ToString().ToUpperInvariant()} mode.",
        "MODE");

    public static FormattableLogMessage LocalFileDataSourceStartsWatchingFile(this IConfigCatLogger logger, string filePath) => logger.LogInterpolated(
        LogLevel.Info, 5300,
        $"Started watching the local config file '{filePath}' for changes.",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceReloadsFile(this IConfigCatLogger logger, string filePath) => logger.LogInterpolated(
        LogLevel.Info, 5301,
        $"Reloading the local config file '{filePath}'...",
        "FILE_PATH");

    #endregion

    #region SDK-specific info messages (6000-6999)

    #endregion
}
