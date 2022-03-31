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
        const int MAX_WAIT_ITERATIONS = 50;

        private int isReading;
        private readonly string fullPath;
        private readonly ILogger logger;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly TaskCompletionSource<bool> asyncInit = new();
        private readonly ManualResetEvent syncInit = new(false);

        private volatile IDictionary<string, Setting> overrideValues;

        public LocalFileDataSource(string filePath, bool autoReload, ILogger logger)
        {
            this.fullPath = Path.GetFullPath(filePath);
            if (autoReload)
            {
                var directory = Path.GetDirectoryName(this.fullPath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    logger.Error($"Directory of {this.fullPath} not found to watch.");
                }
                else
                {
                    this.fileSystemWatcher = new FileSystemWatcher(directory);
                    this.fileSystemWatcher.Changed += OnChanged;
                    this.fileSystemWatcher.Created += OnChanged;
                    this.fileSystemWatcher.Renamed += OnChanged;
                    this.fileSystemWatcher.EnableRaisingEvents = true;
                    logger.Information($"Watching {this.fullPath} for changes.");
                }
            }

            this.logger = logger;
            _ = this.ReadFileAsync(this.fullPath);
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            // filter out events on temporary files
            if (e.FullPath != this.fullPath)
                return;

            this.logger.Information($"Reload file {e.FullPath}.");
            await this.ReadFileAsync(e.FullPath);
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

        private async Task ReadFileAsync(string filePath)
        {
            if (Interlocked.CompareExchange(ref this.isReading, 1, 0) != 0)
                return;

            try
            {
                for (int i = 1; i <= MAX_WAIT_ITERATIONS; i++)
                {
                    try
                    {
                        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
                this.logger.Error($"Failed to read file {filePath}. {ex}");
            }
            finally
            {
                Interlocked.Exchange(ref this.isReading, 0);
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
            if (this.fileSystemWatcher != null)
            {
                this.fileSystemWatcher.Changed -= OnChanged;
                this.fileSystemWatcher.Created -= OnChanged;
                this.fileSystemWatcher.Renamed -= OnChanged;
                this.fileSystemWatcher.Dispose();
            }
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
