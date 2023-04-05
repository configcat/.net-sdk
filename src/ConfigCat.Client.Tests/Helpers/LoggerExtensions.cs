namespace ConfigCat.Client;

internal static class LoggerExtensions
{
    public static LoggerWrapper AsWrapper(this IConfigCatLogger logger, Hooks hooks = null)
    {
        return new LoggerWrapper(logger, hooks);
    }
}
