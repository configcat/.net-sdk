using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Shims;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client.Override;

internal sealed class LocalFileDataSource : IOverrideDataSource, IDisposable
{
    private const int WAIT_TIME_FOR_UNLOCK = 200; // ms
    private const int MAX_WAIT_ITERATIONS = 50; // ms
    private const int FILE_POLL_INTERVAL = 1000; // ms

    private DateTime fileLastWriteTime;
    private readonly string fullPath;
    private readonly LoggerWrapper logger;
    private readonly CancellationTokenSource cancellationTokenSource = new();

    private volatile Dictionary<string, Setting>? overrideValues;

    public LocalFileDataSource(string filePath, bool autoReload, LoggerWrapper logger)
    {
        this.logger = logger;

        if (!File.Exists(filePath))
        {
            this.fullPath = string.Empty;
            logger.LocalFileDataSourceDoesNotExist(filePath);
            return;
        }

        this.fullPath = Path.GetFullPath(filePath);

        // method executes synchronously, GetAwaiter().GetResult() is just for preventing compiler warnings
        var reloadFileTask = ReloadFileAsync(isAsync: false);
        Debug.Assert(reloadFileTask.IsCompleted);
        reloadFileTask.GetAwaiter().GetResult();

        if (autoReload)
        {
            StartWatch();
        }
    }

    public Dictionary<string, Setting> GetOverrides() => this.overrideValues ?? new Dictionary<string, Setting>();

    public Task<Dictionary<string, Setting>> GetOverridesAsync(CancellationToken cancellationToken = default) => Task.FromResult(this.overrideValues ?? new Dictionary<string, Setting>());

    private void StartWatch()
    {
        // It's better to acquire a CancellationToken here because the getter might throw if CTS got disposed.
        var cancellationToken = this.cancellationTokenSource.Token;

        TaskShim.Current.Run(async () =>
        {
            this.logger.LocalFileDataSourceStartsWatchingFile(this.fullPath);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        await WatchCoreAsync(cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        this.logger.LocalFileDataSourceErrorDuringWatching(this.fullPath, ex);
                    }

                    await TaskShim.Current.Delay(TimeSpan.FromMilliseconds(FILE_POLL_INTERVAL), cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
                }
                catch (OperationCanceledException)
                {
                    // ignore exceptions from cancellation.
                }
                catch (Exception ex)
                {
                    this.logger.LocalFileDataSourceErrorDuringWatching(this.fullPath, ex);
                }
            }

            return default(object);
        });
    }

    private async ValueTask WatchCoreAsync(CancellationToken cancellationToken)
    {
        var lastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
        if (lastWriteTime > this.fileLastWriteTime)
        {
            this.logger.LocalFileDataSourceReloadsFile(this.fullPath);
            await ReloadFileAsync(isAsync: true, cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
        }
    }

    private async Task ReloadFileAsync(bool isAsync, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var i = 1; i <= MAX_WAIT_ITERATIONS; i++)
            {
                try
                {
                    var content = File.ReadAllText(this.fullPath);
                    var simplified = SerializationHelper.DeserializeSimplifiedConfig(content.AsSpan(), tolerant: true, throwOnError: false);
                    if (simplified?.Entries is not null)
                    {
                        this.overrideValues = simplified.Entries.ToDictionary(kv => kv.Key, kv => Setting.FromValue(kv.Value));
                        break;
                    }

                    var deserialized = Config.Deserialize(content.AsSpan(), tolerant: true);
                    this.overrideValues = deserialized.SettingsOrEmpty;
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
                        await TaskShim.Current.Delay(TimeSpan.FromMilliseconds(WAIT_TIME_FOR_UNLOCK), cancellationToken).ConfigureAwait(TaskShim.ContinueOnCapturedContext);
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
            this.logger.LocalFileDataSourceFailedToReadFile(this.fullPath, ex);
        }

        this.fileLastWriteTime = File.GetLastWriteTimeUtc(this.fullPath);
    }

    public void Dispose()
    {
        // Background work should stop under all circumstances
        if (!this.cancellationTokenSource.IsCancellationRequested)
        {
            try { this.cancellationTokenSource.Cancel(); }
            catch (ObjectDisposedException) { /* intentional no-op */ }
        }
        this.cancellationTokenSource.Dispose();
    }

    internal sealed class SimplifiedConfig
    {
        [JsonPropertyName("flags")]
        public Dictionary<string, JsonElement>? Entries { get; set; }
    }
}
