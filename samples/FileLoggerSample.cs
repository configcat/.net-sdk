using System;
using ConfigCat.Client;

namespace SampleApplication
{
    class Program
    {
        class MyFileLogger : ILogger
        {
            private readonly string filePath;
            private static object lck = new object();

            public LogLevel LogLevel { get ; set ; }

            public MyFileLogger(string filePath, LogLevel logLevel)
            {
                this.filePath = filePath;
                this.LogLevel = logLevel;
            }

            private void LogMessage(string message)
            {
                lock (lck) // ensure thread safe
                {
                    System.IO.File.AppendAllText(this.filePath, message + Environment.NewLine);
                }
            }

            public void Debug(string message)
            {
                LogMessage("DEBUG - " + message);
            }

            public void Information(string message)
            {
                LogMessage("INFO - " + message);
            }

            public void Warning(string message)
            {
                LogMessage("WARN - " + message);
            }

            public void Error(string message)
            {
                LogMessage("ERROR - " + message);
            }
        }        

        static void Main(string[] args)
        {
            string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "configcat.log");
            LogLevel logLevel = LogLevel.Warning; // I would like to log only WARNING and higher entires (Warnings and Errors).

            var clientConfiguration = new AutoPollConfiguration
            {
                SdkKey = "YOUR-SDK-KEY",
                Logger = new MyFileLogger(filePath, logLevel),
                PollIntervalSeconds = 5
            };

            IConfigCatClient client = new ConfigCatClient(clientConfiguration);

            var feature = client.GetValue("keyNotExists", "N/A");

            Console.ReadKey();
        }
    }
}