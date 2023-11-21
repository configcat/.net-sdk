using System;

#if BENCHMARK_OLD
namespace ConfigCat.Client.Benchmarks.Old;
#else
namespace ConfigCat.Client.Benchmarks.New;
#endif

public class NullLogger : IConfigCatLogger
{
    public LogLevel LogLevel { get; set; }

    public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null) { }
}
