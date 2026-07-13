using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ConfigCat.Extensions.Hosting.Tests.Fakes;

internal sealed class FakeMSLogger : ILogger
{
    public static Logger<T> Create<T>(out FakeMSLogger underlyingLogger, LogLevel minimumLogLevel = LogLevel.Trace)
    {
        underlyingLogger = new FakeMSLogger(string.Empty);
        return new Logger<T>(new Factory(underlyingLogger));
    }

    public FakeMSLogger(string categoryName, LogLevel minimumLogLevel = LogLevel.Trace)
    {
        CategoryName = categoryName;
        MinimumLogLevel = minimumLogLevel;
    }

    public string CategoryName { get; private set; }

    public LogLevel MinimumLogLevel { get; }

    public ConcurrentQueue<(LogLevel logLevel, EventId eventId, string message, Exception? exception)> LogEvents { get; } = new();

    public bool IsEnabled(LogLevel logLevel) => logLevel >= MinimumLogLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel))
        {
            var message = formatter(state, exception);
            LogEvents.Enqueue((logLevel, eventId, message, exception));
        }
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NullScope();

    private sealed class NullScope : IDisposable
    {
        public void Dispose() { /* intentional no-op */ }
    }

    private sealed class Factory : ILoggerFactory
    {
        private readonly FakeMSLogger logger;

        public Factory(FakeMSLogger logger)
        {
            this.logger = logger;
        }

        public void AddProvider(ILoggerProvider provider) => throw new NotImplementedException();

        public ILogger CreateLogger(string categoryName)
        {
            this.logger.CategoryName = categoryName;
            return this.logger;
        }

        public void Dispose() { /* intentional no-op */ }
    }
}
