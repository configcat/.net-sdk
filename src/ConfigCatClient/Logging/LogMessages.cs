using ConfigCat.Client.ConfigService;

namespace ConfigCat.Client;

internal static partial class LoggerExtensions
{
    #region Common error messages (1000-1999)

    #endregion

    #region SDK-specific error messages (2000-2999)

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
