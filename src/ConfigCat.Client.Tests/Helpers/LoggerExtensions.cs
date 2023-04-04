namespace ConfigCat.Client;

internal static class LoggerExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete
    public static LoggerWrapper AsWrapper(this ILogger logger, Hooks hooks = null)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        return new LoggerWrapper(logger, hooks);
    }
}
