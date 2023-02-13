using System;
using ConfigCat.Client.ConfigService;

namespace ConfigCat.Client;

internal static partial class LoggerExtensions
{
    #region Common error messages (1000-1999)

    public static FormattableLogMessage ConfigJsonIsNotPresent(this ILogger logger) => logger.Log(
        LogLevel.Error, 1000,
        "Config JSON is not present.");

    public static FormattableLogMessage ConfigJsonIsNotPresent(this ILogger logger, string defaultParamName, object defaultParamValue) => logger.LogInterpolated(
        LogLevel.Error, 1000,
        $"Config JSON is not present. Returning the {defaultParamName} defined in the app source code: '{defaultParamValue}'.",
        "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE");

    public static FormattableLogMessage SettingEvaluationFailedDueToMissingKey(this ILogger logger, string key, string defaultParamName, object defaultParamValue, string availableKeys) => logger.LogInterpolated(
        LogLevel.Error, 1001,
        $"Evaluating '{key}' failed (key was not found in config JSON). Returning the {defaultParamName} that you specified in the source code: '{defaultParamValue}'. These are the available keys: {availableKeys}.",
        "KEY", "DEFAULT_PARAM_NAME", "DEFAULT_PARAM_VALUE", "AVAILABLE_KEYS");

    public static FormattableLogMessage SettingEvaluationError(this ILogger logger, string methodName, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1002, ex,
        $"Error occured in '{methodName}' method.",
        "METHOD_NAME");

    public static FormattableLogMessage ForceRefreshError(this ILogger logger, string methodName, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1003, ex,
        $"Error occured in '{methodName}' method.",
        "METHOD_NAME");

    public static FormattableLogMessage FetchFailedDueToInvalidSdkKey(this ILogger logger) => logger.Log(
        LogLevel.Error, 1100,
        "Double-check your SDK Key at https://app.configcat.com/sdkkey");

    public static FormattableLogMessage FetchFailedDueToUnexpectedHttpResponse(this ILogger logger, int statusCode, string reasonPhrase) => logger.LogInterpolated(
        LogLevel.Error, 1101,
        $"Unexpected HTTP response was received: {statusCode} {reasonPhrase}",
        "STATUS_CODE", "REASON_PHRASE");

    public static FormattableLogMessage FetchFailedDueToRequestTimeout(this ILogger logger, TimeSpan timeout, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1102, ex,
        $"Request timed out. Timeout value: {timeout}",
        "TIMEOUT");

    public static FormattableLogMessage FetchFailedDueToUnexpectedError(this ILogger logger, Exception ex) => logger.Log(
        LogLevel.Error, 1103, ex,
        "Unexpected error occurred during fetching.");

    public static FormattableLogMessage FetchFailedDueToRedirectLoop(this ILogger logger) => logger.Log(
        LogLevel.Error, 1104,
        "Redirect loop during config.json fetch. Please contact support@configcat.com.");

    public static FormattableLogMessage AutoPollConfigServiceErrorDuringPolling(this ILogger logger, Exception ex) => logger.Log(
        LogLevel.Error, 1200, ex,
        "Error occured during polling.");

    public static FormattableLogMessage LocalFileDataSourceDoesNotExist(this ILogger logger, string filePath) => logger.LogInterpolated(
        LogLevel.Error, 1300,
        $"File '{filePath}' does not exist.",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceErrorDuringWatching(this ILogger logger, string filePath, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1301, ex,
        $"Error occured during watching '{filePath}' for changes.",
        "FILE_PATH");

    public static FormattableLogMessage LocalFileDataSourceFailedToReadFile(this ILogger logger, string filePath, Exception ex) => logger.LogInterpolated(
        LogLevel.Error, 1302, ex,
        $"Failed to read file '{filePath}'.",
        "FILE_PATH");

    #endregion

    #region SDK-specific error messages (2000-2999)

    public static FormattableLogMessage EstablishingSecureConnectionFailed(this ILogger logger, Exception ex) => logger.Log(
        LogLevel.Error, 2100, ex,
        "Secure connection could not be established. Please make sure that your application is enabled to use TLS 1.2+. For more information see https://stackoverflow.com/a/58195987/8656352.");

    #endregion

    #region Common warning messages (3000-3999)

    public static FormattableLogMessage ConfigServiceCantInitiateHttpCalls(this ILogger logger) => logger.Log(
        LogLevel.Warning, 3000,
        "Client is in offline mode, it can't initiate HTTP calls.");

    public static FormattableLogMessage ConfigServiceMethodHasNoEffect(this ILogger logger, string methodName) => logger.LogInterpolated(
        LogLevel.Warning, 3001,
        $"Client has already been disposed, thus {methodName}() has no effect.",
        "METHOD_NAME");

    #endregion

    #region SDK-specific warning messages (4000-4999)

    #endregion

    #region Common info messages (5000-5999)

    #endregion

    #region SDK-specific info messages (6000-6999)

    #endregion

    #region Common debug messages (10000-)

    public static FormattableLogMessage ConfigServiceStatusChange(this ILogger logger, ConfigServiceBase.Status status) => logger.LogInterpolated(
        LogLevel.Debug, 10000,
        $"Switched to {status.ToString().ToUpperInvariant()} mode.",
        "MODE");

    #endregion
}
