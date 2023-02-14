namespace ConfigCat.Client;

internal static class LoggerExtensions
{
    public static LoggerWrapper AsWrapper(this IConfigCatLogger logger)
    {
        return new LoggerWrapper(logger);
    }
}
