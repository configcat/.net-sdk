namespace ConfigCat.Client.ConfigService
{
    internal static class ConfigServiceLoggerExtensions
    {
        public static void StatusChange(this ILogger logger, ConfigServiceBase.Status status)
        {
            logger.Debug($"Switched to {status.ToString().ToUpperInvariant()} mode.");
        }

        public static void OfflineModeWarning(this ILogger logger)
        {
            logger.Warning("Client is in offline mode, it can't initiate HTTP calls.");
        }

        public static void DisposedWarning(this ILogger logger, string methodName)
        {
            logger.Warning($"Client has already been disposed, thus {methodName}() has no effect.");
        }
    }
}