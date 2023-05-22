using System;
using System.Globalization;
using System.IO;
using ConfigCat.Client;

namespace SampleApplication
{
    class Program
    {
        class MyFileLogger : IConfigCatLogger
        {
            private static readonly object SyncObj = new object();

            private readonly string filePath;

            private volatile LogLevel logLevel;

            public LogLevel LogLevel
            {
                get => this.logLevel;
                set => this.logLevel = value;
            }

            public MyFileLogger(string filePath, LogLevel logLevel)
            {
                this.filePath = filePath;
                LogLevel = logLevel;
            }

            private void AppendMessage(string message)
            {
                lock (SyncObj) // ensure thread safety
                {
                    File.AppendAllText(this.filePath, message + Environment.NewLine);
                }
            }

            public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
            {
                var levelString = level switch
                {
                    LogLevel.Debug   => "DEBUG",
                    LogLevel.Info    => "INFO ",
                    LogLevel.Warning => "WARN ",
                    LogLevel.Error   => "ERROR",
                    _ => level.ToString().ToUpperInvariant().PadRight(5)
                };

                var timeStampString = DateTime.UtcNow.ToString("O");

                var eventIdString = eventId.Id.ToString(CultureInfo.InvariantCulture);

                var exceptionString = exception is null ? string.Empty : Environment.NewLine + exception;

                AppendMessage($"ConfigCat.{levelString}@{timeStampString} [{eventIdString}] {message.InvariantFormattedMessage}{exceptionString}");
            }
        }

        static void Main(string[] args)
        {
            var filePath = Path.Combine(Environment.CurrentDirectory, "configcat.log");
            var logLevel = LogLevel.Warning; // Log only WARNING and higher entries (warnings and errors).

            var client = ConfigCatClient.Get("YOUR-SDK-KEY", options =>
            {
                options.Logger = new MyFileLogger(filePath, logLevel);
                options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(5));
            });

            var feature = client.GetValue("keyNotExists", "N/A");

            Console.ReadKey();
        }
    }
}
