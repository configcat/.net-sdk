using System;
using System.IO;
using ConfigCat.Client;

namespace SampleApplication
{
    class Program
    {
        class MyFileLogger : IConfigCatLogger
        {
            private readonly string filePath;
            private static readonly object SyncObj = new object();

            public LogLevel LogLevel { get; set; }

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

            #region Deprecated methods

            void ILogger.Debug(string message) => throw new NotSupportedException();

            void ILogger.Information(string message) => throw new NotSupportedException();

            void ILogger.Warning(string message) => throw new NotSupportedException();

            void ILogger.Error(string message) => throw new NotSupportedException();

            #endregion

            public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception = null)
            {
                var levelString = level switch
                {
                    LogLevel.Debug => "DEBUG",
                    LogLevel.Info => "INFO",
                    LogLevel.Warning => "WARNING",
                    LogLevel.Error => "ERROR",
                    _ => level.ToString().ToUpper()
                };

                AppendMessage(levelString + " - " + message.InvariantFormattedMessage);
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
