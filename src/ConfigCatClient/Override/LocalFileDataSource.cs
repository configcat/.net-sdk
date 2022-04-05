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
        private readonly CancellationTokenSource pollerCancellationTokenSource = new();

        private volatile IDictionary<string, Setting> overrideValues;

        public LocalFileDataSource(string filePath, bool autoReload, ILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger.Error($"File {filePath} does not exist.");
                this.SetInitialized();
                return;
            }

            this.fullPath = Path.GetFullPath(filePath);
            this.logger = logger;

            this.StartFileReading(autoReload);
        }

        public IDictionary<string, Setting> GetOverrides()
        {
            if (this.overrideValues != null) return this.overrideValues;
            this.syncInit.WaitOne();
            return this.overrideValues ?? new Dictionary<string, Setting>();
        }

        public async Task<IDictionary<string, Setting>> GetOverridesAsync()
        {
            if (this.overrideValues != null) return this.overrideValues;
            await this.asyncInit.Task.ConfigureAwait(false);
            return this.overrideValues ?? new Dictionary<string, Setting>();
        }

        private void StartFileReading(bool autoReload)
        {
            Task.Run(async () =>
            {
                try
                {
                    await ReadFileAsync();

                    if (autoReload)
                    {
                        await this.StartWatchAsync();
                    }
                }
                finally
                {
                    this.SetInitialized();
                }
            });
        }

        private async Task StartWatchAsync()
        {
            this.logger.Information($"Watching {this.fullPath} for changes.");
            while (!this.pollerCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        var lastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
                        if (lastWriteTime > this.fileLastWriteTime)
                        {
                            this.logger.Information($"Reload file {this.fullPath}.");
                            await ReadFileAsync();
                        }
                    }

                    finally
                    {
                        await Task.Delay(FILE_POLL_INTERVAL, this.pollerCancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore exceptions from cancellation.
                }
            }
        }

        private async Task ReadFileAsync()
        {
            try
            {
                for (int i = 1; i <= MAX_WAIT_ITERATIONS; i++)
                {
                    try
                    {
                        using var stream = new FileStream(this.fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var reader = new StreamReader(stream);
                        var content = await reader.ReadToEndAsync();
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

                        await Task.Delay(WAIT_TIME_FOR_UNLOCK);
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
                this.SetInitialized();
            }
        }

        private void SetInitialized()
        {
            this.syncInit.Set();
            this.asyncInit.TrySetResult(true);
        }

        public void Dispose()
        {
            this.pollerCancellationTokenSource.Cancel();
            this.syncInit.Dispose();
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
