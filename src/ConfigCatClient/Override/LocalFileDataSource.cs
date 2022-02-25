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
        private int isReading;
        private readonly ILogger logger;
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly TaskCompletionSource<bool> asyncInit = new();
        private readonly ManualResetEvent syncInit = new(false);

        private IDictionary<string, Setting> overrideValues;

        public LocalFileDataSource(string filePath, bool autoReload, ILogger logger)
        {
            if (autoReload)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    logger.Error($"Directory of {filePath} not found to watch.");
                }
                else
                {
                    this.fileSystemWatcher = new FileSystemWatcher(directory)
                    {
                        Filter = Path.GetFileName(filePath),
                        NotifyFilter = NotifyFilters.LastWrite,
                        EnableRaisingEvents = true
                    };
                    this.fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                    logger.Information($"Watching {filePath} for changes.");
                }
            }

            this.logger = logger;
            _ = this.ReadFileAsync(filePath);
        }

        private async void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
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
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
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
            }
            catch (Exception ex)
            {
                this.logger.Error($"Failed to read file {filePath}. {ex}");
            }
            finally
            {
                this.SetInitialized();
                Interlocked.Exchange(ref this.isReading, 0);
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
                this.fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
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
