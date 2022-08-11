namespace ConfigCat.Client
{
    internal static class LoggerExtensions
    {
        public static LoggerWrapper AsWrapper(this ILogger logger)
        {
            return new LoggerWrapper(logger);
        }
    }
}
