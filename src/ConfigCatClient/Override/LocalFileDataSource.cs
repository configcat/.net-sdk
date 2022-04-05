using ConfigCat.Client.Evaluate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Override
{
    internal sealed class LocalFileDataSource : IOverrideDataSource
    {
        const int WAIT_TIME_FOR_UNLOCK = 200; // ms
        const int MAX_WAIT_ITERATIONS = 50; // ms
        const int FILE_POLL_INTERVAL = 1000; // ms

        private DateTime fileLastWriteTime;
        private readonly string fullPath;
        private readonly ILogger logger;
        private readonly TaskCompletionSource<bool> asyncInit = new();
        private readonly ManualResetEvent syncInit = new(false);
        private readonly CancellationTokenSource cancellationTokenSource = new();

        private volatile IDictionary<string, Setting> overrideValues;

        public LocalFileDataSource(string filePath, bool autoReload, ILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger.Error($"File {filePath} does not exist.");
                return;
            }

            this.fullPath = Path.GetFullPath(filePath);
            this.logger = logger;

            this.ReloadFile();

            if (autoReload)
            {
                this.StartWatch();
            }
        }

        public IDictionary<string, Setting> GetOverrides() => this.overrideValues ?? new Dictionary<string, Setting>();

        public Task<IDictionary<string, Setting>> GetOverridesAsync() => Task.FromResult(this.overrideValues ?? new Dictionary<string, Setting>());


        private void StartWatch()
        {
            Task.Run(async () =>
            {
                this.logger.Information($"Watching {this.fullPath} for changes.");
                while (!this.cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        try
                        {
                            var lastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
                            if (lastWriteTime > this.fileLastWriteTime)
                            {
                                this.logger.Information($"Reload file {this.fullPath}.");
                                this.ReloadFile();
                            }
                        }

                        finally
                        {
                            await Task.Delay(FILE_POLL_INTERVAL, this.cancellationTokenSource.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore exceptions from cancellation.
                    }
                }
            });
        }

        private void ReloadFile()
        {
            try
            {
                for (int i = 1; i <= MAX_WAIT_ITERATIONS; i++)
                {
                    try
                    {
                        var content = File.ReadAllText(this.fullPath);
                        var simplified = content.DeserializeOrDefault<SimplifiedConfig>();
                        if (simplified?.Entries != null)
                        {
                            this.overrideValues = simplified.Entries.ToDictionary(kv => kv.Key, kv => kv.Value.ToSetting());
                            return;
                        }

                        var deserialized = content.Deserialize<SettingsWithPreferences>();
                        this.overrideValues = deserialized.Settings;
                        break;
                    }
                    // this logic ensures that we keep trying to open the file for max 10s
                    // when it's locked by another process.
                    catch (IOException e) when (e.HResult == -2147024864) // ERROR_SHARING_VIOLATION
                    {
                        if (i >= MAX_WAIT_ITERATIONS)
                            throw;

                        Thread.Sleep(WAIT_TIME_FOR_UNLOCK);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error($"Failed to read file {this.fullPath}. {ex}");
            }
            finally
            {
                this.fileLastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
            }
        }

        public void Dispose()
        {
            this.cancellationTokenSource.Cancel();
        }

        private sealed class SimplifiedConfig
        {
#if USE_NEWTONSOFT_JSON
            [Newtonsoft.Json.JsonProperty(PropertyName = "flags")]
#else
            [System.Text.Json.Serialization.JsonPropertyName("flags")]
#endif
            public IDictionary<string, object> Entries { get; set; }
        }
    }
}
