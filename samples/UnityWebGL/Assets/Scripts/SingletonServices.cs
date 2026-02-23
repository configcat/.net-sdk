using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client;
using ConfigCat.Client.Shims;
using UnityEngine;
using UnityEngine.Networking;

// Assign this script to the first scene, then you can access the provided services via SingletonServices.Instance in your scripts.
// See also:
// * https://gamedev.stackexchange.com/questions/116009/in-unity-how-do-i-correctly-implement-the-singleton-pattern
// * https://gamedevbeginner.com/singletons-in-unity-the-right-way/
public class SingletonServices : MonoBehaviour
{
    private static SingletonServices instance;
    public static SingletonServices Instance => instance;

    [field: NonSerialized]
    public IConfigCatClient ConfigCatClient { get; private set; }

    private void Awake()
    {
        if (Interlocked.CompareExchange(ref instance, this, null) is not null)
        {
            // If another instance has been created already, get rid of this one.
            Destroy(this);
            return;
        }

        var logger = new ConfigCatToUnityDebugLogAdapter(LogLevel.Debug);
        var taskShim = new UnityTaskShim(this);
        ConfigCat.Client.ConfigCatClient.PlatformCompatibilityOptions.EnableUnityWebGLCompatibility(taskShim, () => new UnityWebRequestConfigFetcher(taskShim, logger));

        ConfigCatClient = ConfigCat.Client.ConfigCatClient.Get("PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ", options =>
        {
            options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(10));
            options.Logger = logger;
        });

        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnDestroy()
    {
        this.ConfigCatClient?.Dispose();
        this.ConfigCatClient = null;
    }

    private sealed class UnityTaskShim : TaskShim
    {
        private readonly SingletonServices singletonServices;

        public UnityTaskShim(SingletonServices singletonServices)
        {
            this.singletonServices = singletonServices;
        }

        public override async Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            return await function();
        }

        public override async Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (delay == TimeSpan.Zero)
            {
                return;
            }

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            IEnumerator wait = null;
            var ctr = cancellationToken.CanBeCanceled
                ? cancellationToken.Register(() =>
                {
                    try
                    {
                        if (wait is not null)
                        {
                            this.singletonServices.StopCoroutine(wait);
                        }
                    }
                    catch { /* there's nothing to do if StopCoroutine fails */ }
                    finally { tcs.TrySetCanceled(cancellationToken); }
                }, useSynchronizationContext: true)
                : default;

            if (delay != Timeout.InfiniteTimeSpan)
            {
                wait = Wait(delay, tcs, ctr);

                static IEnumerator Wait(TimeSpan delay, TaskCompletionSource<object> tcs, CancellationTokenRegistration ctr)
                {
                    yield return new WaitForSecondsRealtime((float)delay.TotalSeconds);

                    try { ctr.Dispose(); }
                    catch { /* there's nothing to do if Dispose fails */ }
                    finally { tcs.TrySetResult(null); }
                }

                this.singletonServices.StartCoroutine(wait);
            }

            await tcs.Task;
        }
    }

    private sealed class UnityWebRequestConfigFetcher : IConfigCatConfigFetcher
    {
        private readonly TaskShim taskShim;
        private readonly ConfigCatToUnityDebugLogAdapter logger;

        public UnityWebRequestConfigFetcher(TaskShim taskShim, ConfigCatToUnityDebugLogAdapter logger)
        {
            this.taskShim = taskShim;
            this.logger = logger;
        }

        public async Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // NOTE: We shouldn't specify additional request headers in Unity WebGL because
            // that would cause an unnecessary CORS OPTIONS request in browsers.
            // We send the necessary information in the query string instead.

            var uri = GetAdjustedUri(request);

            logger.LogDebug($"Fetching config at '{uri}'...");

            const int retryLimit = 1;

            for (var retryCount = 0; ; retryCount++)
            {
                using var webRequest = UnityWebRequest.Get(uri);

                webRequest.timeout = (int)request.Timeout.TotalSeconds;

                var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                var ctr = cancellationToken.CanBeCanceled
                    ? cancellationToken.Register(() =>
                    {
                        try { webRequest.Abort(); }
                        catch { /* there's nothing to do if Abort fails */ }
                        finally { tcs.TrySetCanceled(cancellationToken); }
                    }, useSynchronizationContext: true)
                    : default;

                webRequest.SendWebRequest().completed += (_) =>
                {
                    try { ctr.Dispose(); }
                    catch { /* there's nothing to do if Dispose fails */ }
                    finally { tcs.TrySetResult(null); }
                };

                await tcs.Task;

                if (webRequest.result is UnityWebRequest.Result.Success or UnityWebRequest.Result.ProtocolError)
                {
                    var statusCode = (HttpStatusCode)webRequest.responseCode;
                    logger.LogDebug($"Fetching config finished with status code {(int)statusCode} {statusCode}.");
                    var response = new FetchResponse(statusCode, reasonPhrase: null, webRequest.GetResponseHeaders(), statusCode == HttpStatusCode.OK ? webRequest.downloadHandler.text : null);
                    if (response.IsExpected || retryCount >= retryLimit)
                    {
                        return response;
                    }
                }
                else if (webRequest.result == UnityWebRequest.Result.ConnectionError && webRequest.error == "Request timeout")
                {
                    logger.LogDebug($"Fetching config timed out.");
                    if (retryCount >= retryLimit)
                    {
                        throw FetchErrorException.Timeout(TimeSpan.FromSeconds(webRequest.timeout));
                    }
                }
                else
                {
                    logger.LogDebug($"Fetching config failed due to {webRequest.result}: {webRequest.error}");
                    if (retryCount >= retryLimit)
                    {
                        throw FetchErrorException.Failure(null, new Exception($"Web request failed due to {webRequest.result}: {webRequest.error}"));
                    }
                }

                // Wait a little before trying again.
                await TaskShim.Current.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(true);

                logger.LogDebug($"Trying again to fetch config at '{uri}'...");
            }
        }

        private static Uri GetAdjustedUri(in FetchRequest request)
        {
            var userAgentHeader = request.Headers.FirstOrDefault(kvp =>
                "User-Agent".Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)
                || "X-ConfigCat-UserAgent".Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

            var uriBuilder = new UriBuilder(request.Uri);

            var separator = uriBuilder.Query.Length == 0 ? "?" : "&";

            const string sdkQueryParamName = "sdk=";
            var sdkQueryParamValue = Uri.EscapeDataString(userAgentHeader.Value ?? string.Empty);

            if (request.LastETag is not null)
            {
                uriBuilder.Query += separator + sdkQueryParamName + sdkQueryParamValue
                    + "&ccetag=" + Uri.EscapeDataString(request.LastETag);
            }
            else
            {
                uriBuilder.Query += separator + sdkQueryParamName + sdkQueryParamValue;
            }

            return uriBuilder.Uri;
        }

        public void Dispose() { }
    }

    private sealed class ConfigCatToUnityDebugLogAdapter : IConfigCatLogger
    {
        private readonly LogLevel logLevel;

        public ConfigCatToUnityDebugLogAdapter(LogLevel logLevel)
        {
            this.logLevel = logLevel;
        }

        public LogLevel LogLevel
        {
            get => this.logLevel;
            set { throw new NotSupportedException(); }
        }

        public bool IsEnabled(LogLevel level)
        {
            return (byte)level <= (byte)LogLevel;
        }

        void IConfigCatLogger.Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception)
        {
            var eventIdString = eventId.Id.ToString(CultureInfo.InvariantCulture);
            var exceptionString = exception is null ? string.Empty : Environment.NewLine + exception;
            var logMessage = $"ConfigCat[{eventIdString}] {message.InvariantFormattedMessage}{exceptionString}";
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Info:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Debug:
                    Debug.Log(logMessage);
                    break;
            }
        }

        public void LogDebug(string message, Exception exception = null)
        {
            if (IsEnabled(LogLevel.Debug))
            {
                var formattableMessage = new FormattableLogMessage(message);
                ((IConfigCatLogger)this).Log(LogLevel.Debug, 0, ref formattableMessage, exception);
            }
        }
    }
}
