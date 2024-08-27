using System;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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

        ConfigCat.Client.ConfigCatClient.PlatformCompatibilityOptions.EnableUnityWebGLCompatibility(new UnityTaskShim(this), () => new UnityWebRequestConfigFetcher(this));

        ConfigCatClient = ConfigCat.Client.ConfigCatClient.Get("PKDVCLf-Hq-h-kCzMp-L7Q/HhOWfwVtZ0mb30i9wi17GQ", options =>
        {
            options.PollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromSeconds(10));
            options.Logger = new ConfigCatToUnityDebugLogAdapter(LogLevel.Debug);
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
        private readonly SingletonServices singletonServices;

        public UnityWebRequestConfigFetcher(SingletonServices singletonServices)
        {
            this.singletonServices = singletonServices;
        }

        public async Task<FetchResponse> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = request.Uri;

            // NOTE: It's intentional that we don't specify the If-None-Match header.
            // The browser should automatically handle it, adding it manually would cause an unnecessary CORS OPTIONS request.
            // For the case where the browser doesn't handle it, we also send the etag in the ccetag query parameter.

            if (request.LastETag is not null)
            {
                var query = HttpUtility.ParseQueryString(uri.Query);
                query["ccetag"] = request.LastETag;
                var uriBuilder = new UriBuilder(uri);
                uriBuilder.Query = query.ToString();
                uri = uriBuilder.Uri;
            }
            
            Debug.Log($"Fetching config at {uri}...");

            using var webRequest = UnityWebRequest.Get(uri);

            for (int i = 0, n = request.Headers.Count; i < n; i++)
            {
                var header = request.Headers[i];
                webRequest.SetRequestHeader(header.Key, header.Value);
            }

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
                Debug.Log($"Fetching config finished with status code {statusCode}.");
                return new FetchResponse(statusCode, reasonPhrase: null, webRequest.GetResponseHeaders(), statusCode == HttpStatusCode.OK ? webRequest.downloadHandler.text : null);
            }
            else if (webRequest.result == UnityWebRequest.Result.ConnectionError && webRequest.error == "Request timeout")
            {
                Debug.Log($"Fetching config timed out.");
                throw FetchErrorException.Timeout(TimeSpan.FromSeconds(webRequest.timeout));
            }
            else 
            {
                Debug.Log($"Fetching config failed due to {webRequest.result}: {webRequest.error}");
                throw FetchErrorException.Failure(null, new Exception($"Web request failed due to {webRequest.result}: {webRequest.error}"));
            }
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

        public void Log(LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception exception = null)
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
    }
}
