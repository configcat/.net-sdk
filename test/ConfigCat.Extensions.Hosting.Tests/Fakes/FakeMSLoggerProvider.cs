using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ConfigCat.Extensions.Hosting.Tests.Fakes;

internal sealed class FakeMSLoggerProvider : ILoggerProvider
{
    public FakeMSLoggerProvider(LogLevel minimumLogLevel = LogLevel.Trace)
    {
        MinimumLogLevel = minimumLogLevel;
    }

    public LogLevel MinimumLogLevel { get; }

    public ConcurrentDictionary<string, FakeMSLogger> Loggers { get; } = new();

    public ILogger CreateLogger(string categoryName)
        => Loggers.GetOrAdd(categoryName, name => new FakeMSLogger(name, MinimumLogLevel));

    public void Dispose() => Loggers.Clear();
}
