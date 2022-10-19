﻿using ConfigCat.Client.Evaluation;
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
        private readonly LoggerWrapper logger;
        private readonly ConfigCatClientContext clientContext;
        private readonly CancellationTokenSource cancellationTokenSource = new();

        private volatile IDictionary<string, Setting> overrideValues;

        public LocalFileDataSource(string filePath, bool autoReload, LoggerWrapper logger, ConfigCatClientContext clientContext = null)
        {
            if (!File.Exists(filePath))
            {
                logger.Error($"File {filePath} does not exist.");
                return;
            }

            this.fullPath = Path.GetFullPath(filePath);
            this.logger = logger;
            this.clientContext = clientContext ?? ConfigCatClientContext.None;

            // method executes synchronously, GetAwaiter().GetResult() is just for preventing compiler warnings
            this.ReloadFileAsync(isAsync: false).GetAwaiter().GetResult();

            if (autoReload)
            {
                this.StartWatch();
            }
        }

        public IDictionary<string, Setting> GetOverrides() => this.overrideValues ?? new Dictionary<string, Setting>();

        public Task<IDictionary<string, Setting>> GetOverridesAsync() => Task.FromResult(this.overrideValues ?? new Dictionary<string, Setting>());

        private void StartWatch()
        {
            // It's better to acquire a CancellationToken here because the getter might throw if CTS got disposed of.
            var cancellationToken = this.cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                this.logger.Information($"Watching {this.fullPath} for changes.");

                while (!this.clientContext.ClientIsGone && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        try
                        {
                            await WatchCoreAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            this.logger.Error($"Error occured during watching {this.fullPath} for changes.", ex);
                        }

                        if (!this.clientContext.ClientIsGone)
                        {
                            await Task.Delay(FILE_POLL_INTERVAL, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore exceptions from cancellation.
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error($"Error occured during watching {this.fullPath} for changes.", ex);
                    }
                }
            });
        }

        private async Task WatchCoreAsync(CancellationToken cancellationToken)
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
            if (lastWriteTime > this.fileLastWriteTime)
            {
                this.logger.Information($"Reload file {this.fullPath}.");
                await this.ReloadFileAsync(isAsync: true, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReloadFileAsync(bool isAsync, CancellationToken cancellationToken = default)
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

                        if (isAsync)
                        {
                            await Task.Delay(WAIT_TIME_FOR_UNLOCK, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            Thread.Sleep(WAIT_TIME_FOR_UNLOCK);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                this.logger.Error($"Failed to read file {this.fullPath}.", ex);
            }

            this.fileLastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
        }

        private void Dispose(bool disposing)
        {
            // Background work should stop under all circumstances
            this.cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
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
