using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Tests.Helpers;
using ConfigCat.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class HttpConfigFetcherTests
{
    private const string TestSdkKey = "configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g";
    private const string TestETag = "W/\"123\"";
    private const string TestProxyUrl = "https://127.0.0.1:3128";

    [DataTestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    public async Task HttpClientConfigFetcher_InternalHandler_ShouldReuseHandlerWhenResponseIsExpected(bool useProxy, bool runParallel)
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(15);
        var proxy = new WebProxy(TestProxyUrl);

        var logEvents = new List<LogEvent>();
        var fakeLogger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Debug).AsWrapper();

        var capturedParams = new List<(HttpClient Client, HttpMessageHandler Handler)>();

        var fakeHandler = new FakeHttpClientHandler();

        using var configFetcher = useProxy
            ? new TestHttpClientConfigFetcher(proxy)
            : new TestHttpClientConfigFetcher();

        configFetcher.OnCreateHttpClient = (handler, timeout, createHttpClient) =>
        {
            var httpClient = createHttpClient(fakeHandler, timeout);
            lock (capturedParams) { capturedParams.Add((httpClient, handler)); }
            return httpClient;
        };

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        // Act

        if (runParallel)
        {
            await Task.WhenAll(
                configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default),
                configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default));
        }
        else
        {
            await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default);
            await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default);
        }

        // Assert

        Assert.AreEqual(2, capturedParams.Count);

        HttpClient client1 = capturedParams[0].Client, client2 = capturedParams[1].Client;
        Assert.AreNotSame(client1, client2);
        Assert.AreEqual(timeout, client1.Timeout);
        Assert.AreEqual(timeout, client2.Timeout);

        HttpMessageHandler handler1 = capturedParams[0].Handler, handler2 = capturedParams[1].Handler;
        Assert.AreSame(handler1, handler2);
        Assert.IsInstanceOfType(handler1, typeof(HttpClientHandler));
        Assert.AreSame(useProxy ? proxy : null, ((HttpClientHandler)handler1).Proxy);

        Assert.AreEqual(2, fakeHandler.SendInvokeCount);
    }

    [DataTestMethod]
    [DataRow("408", false)]
    [DataRow("timeout", false)]
    [DataRow("error", true)]
    public async Task HttpClientConfigFetcher_InternalHandler_ShouldRenewHandlerAndRetryOnUnexpectedResponseOrFailure(string @case, bool useProxy)
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(1);
        var proxy = new WebProxy(TestProxyUrl);

        var logEvents = new List<LogEvent>();
        var fakeLogger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Debug).AsWrapper();

        var capturedParams = new List<(HttpClient Client, HttpMessageHandler Handler)>();

        HttpMessageHandler fakeHandler = @case switch
        {
            "408" => new FakeHttpClientHandler(HttpStatusCode.RequestTimeout),
            "timeout" => new FakeHttpClientHandler(HttpStatusCode.OK, delay: timeout + timeout),
            "error" => new ExceptionThrowerHttpClientHandler(new HttpRequestException("Connection reset by peer")),
            _ => throw new NotImplementedException()
        };

        using var configFetcher = useProxy
            ? new TestHttpClientConfigFetcher(proxy)
            : new TestHttpClientConfigFetcher();

        configFetcher.OnCreateHttpClient = (handler, timeout, createHttpClient) =>
        {
            var httpClient = createHttpClient(fakeHandler, timeout);
            lock (capturedParams) { capturedParams.Add((httpClient, handler)); }
            return httpClient;
        };

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        // Act

        FetchResponse fetchResponse = default;
        FetchErrorException? fetchErrorException = null;
        try { fetchResponse = await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default); }
        catch (FetchErrorException ex) { fetchErrorException = ex; }

        switch (@case)
        {
            case "408":
                Assert.AreEqual(HttpStatusCode.RequestTimeout, fetchResponse.StatusCode);
                break;
            case "timeout":
                Assert.IsInstanceOfType(fetchErrorException, typeof(FetchErrorException.Timeout_));
                break;
            case "error":
                Assert.IsInstanceOfType(fetchErrorException, typeof(FetchErrorException.Failure_));
                break;
        }

        // Assert

        Assert.AreEqual(2, capturedParams.Count);

        HttpClient client1 = capturedParams[0].Client, client2 = capturedParams[1].Client;
        Assert.AreNotSame(client1, client2);
        Assert.AreEqual(timeout, client1.Timeout);
        Assert.AreEqual(timeout, client2.Timeout);

        HttpMessageHandler handler1 = capturedParams[0].Handler, handler2 = capturedParams[1].Handler;
        Assert.AreNotSame(handler1, handler2);
        Assert.IsInstanceOfType(handler1, typeof(HttpClientHandler));
        Assert.IsInstanceOfType(handler2, typeof(HttpClientHandler));
        Assert.AreSame(useProxy ? proxy : null, ((HttpClientHandler)handler1).Proxy);
        Assert.AreSame(useProxy ? proxy : null, ((HttpClientHandler)handler2).Proxy);

        var sendInvokeCount = fakeHandler is ExceptionThrowerHttpClientHandler exceptionThrowerHandler
            ? exceptionThrowerHandler.SendInvokeCount
            : ((FakeHttpClientHandler)fakeHandler).SendInvokeCount;
        Assert.AreEqual(2, sendInvokeCount);
    }

    [TestMethod]
    public async Task HttpClientConfigFetcher_InternalHandler_ShouldNotRenewHandlerAfterRenewWithinThreshold()
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(1);
        var handlerRenewalThreshold = TimeSpan.FromSeconds(1);
        var requestRetryDelay = TimeSpan.FromMilliseconds(10);

        var logEvents = new List<LogEvent>();
        var fakeLogger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Debug).AsWrapper();

        var capturedParams = new List<(HttpClient Client, HttpMessageHandler Handler)>();

        var fakeHandler = new ExceptionThrowerHttpClientHandler(new HttpRequestException("Connection reset by peer"));

        using var configFetcher = new TestHttpClientConfigFetcher();

        configFetcher.HandlerRenewalThreshold = handlerRenewalThreshold;
        configFetcher.RequestRetryDelay = requestRetryDelay;

        configFetcher.OnCreateHttpClient = (handler, timeout, createHttpClient) =>
        {
            var httpClient = createHttpClient(fakeHandler, timeout);
            lock (capturedParams) { capturedParams.Add((httpClient, handler)); }
            return httpClient;
        };

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        // Act

        await Assert.ThrowsExceptionAsync<FetchErrorException.Failure_>(() => configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default));

        Debug.WriteLine(capturedParams.Count);

        await Assert.ThrowsExceptionAsync<FetchErrorException.Failure_>(() => configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default));

        Debug.WriteLine(capturedParams.Count);

        await Task.Delay(new TimeSpan(handlerRenewalThreshold.Ticks * 3 / 2));

        await Assert.ThrowsExceptionAsync<FetchErrorException.Failure_>(() => configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default));

        // Assert

        Assert.AreEqual(5, capturedParams.Count);

        HttpMessageHandler
            handler1 = capturedParams[0].Handler, handler2 = capturedParams[1].Handler, // 1st call to FetchAsync
            handler3 = capturedParams[2].Handler, // 2nd call to FetchAsync
            handler4 = capturedParams[3].Handler, handler5 = capturedParams[4].Handler; // 3rd call to FetchAsync

        Assert.AreNotSame(handler1, handler2);
        Assert.AreSame(handler2, handler3);
        Assert.AreSame(handler3, handler4);
        Assert.AreNotSame(handler4, handler5);
        Assert.AreNotSame(handler1, handler5);

        Assert.AreEqual(6, fakeHandler.SendInvokeCount);
    }

    [TestMethod]
    public async Task HttpClientConfigFetcher_ExternalHandler_ShouldUsePassedHandler()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler();

        var instance = new DefaultConfigFetcher("x", new Uri("http://example.com"), "1.0",
            new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), true, false,
            TimeSpan.FromSeconds(30));

        // Act

        await instance.FetchAsync(ProjectConfig.Empty);

        // Assert

        Assert.AreEqual(1, myHandler.SendInvokeCount);
    }

    [TestMethod]
    public async Task HttpClientConfigFetcher_ExternalHandler_ShouldContinueUsingHandlerOnFailure()
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(1);

        var logEvents = new List<LogEvent>();
        var fakeLogger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Debug).AsWrapper();

        var capturedParams = new List<(HttpClient Client, HttpMessageHandler Handler)>();

        var fakeHandler = new ExceptionThrowerHttpClientHandler(new HttpRequestException("Connection reset by peer"));

        using var configFetcher = new TestHttpClientConfigFetcher(fakeHandler);

        configFetcher.OnCreateHttpClient = (handler, timeout, createHttpClient) =>
        {
            var httpClient = createHttpClient(handler, timeout);
            lock (capturedParams) { capturedParams.Add((httpClient, handler)); }
            return httpClient;
        };

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        // Act

        FetchErrorException? fetchErrorException = null;
        try { await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default); }
        catch (FetchErrorException ex) { fetchErrorException = ex; }

        Assert.IsInstanceOfType(fetchErrorException, typeof(FetchErrorException.Failure_));

        // Assert

        Assert.AreEqual(1, capturedParams.Count);

        HttpClient client1 = capturedParams[0].Client;
        Assert.AreEqual(timeout, client1.Timeout);

        HttpMessageHandler handler1 = capturedParams[0].Handler;
        Assert.AreSame(fakeHandler, handler1);
        Assert.IsInstanceOfType(handler1, typeof(ExceptionThrowerHttpClientHandler));

        Assert.AreEqual(2, fakeHandler.SendInvokeCount);
    }

    [TestMethod]
    public void HttpClientConfigFetcher_ExternalHandler_HandlersDisposeShouldNotInvoke()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler();

        var instance = new DefaultConfigFetcher("x", new Uri("http://example.com"), "1.0",
            new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), true, false,
            TimeSpan.FromSeconds(30));

        // Act

        instance.Dispose();

        // Assert

        Assert.IsFalse(myHandler.Disposed);
    }

    [TestMethod]
    public async Task HttpClientConfigFetcher_ExternalClient_ShouldUseExternallyCreatedClient()
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(15);

        var logEvents = new List<LogEvent>();
        var fakeLogger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Debug).AsWrapper();

        var capturedParams = new List<(FetchRequest Request, bool IsRetry, HttpClient CreatedClient)>();

        var fakeHandler = new FakeHttpClientHandler();

        using var configFetcher = new TestHttpClientConfigFetcher((request, isRetry) =>
        {
            var httpClient = new HttpClient(fakeHandler, disposeHandler: false);
            lock (capturedParams) capturedParams.Add((request, isRetry, httpClient));
            return httpClient;
        });

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        // Act

        await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default);
        await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default);

        // Assert

        Assert.AreEqual(2, capturedParams.Count);

        FetchRequest request1 = capturedParams[0].Request, request2 = capturedParams[1].Request;
        Assert.AreEqual(request1, request2);

        HttpClient? createdClient1 = capturedParams[0].CreatedClient, createdClient2 = capturedParams[1].CreatedClient;
        Assert.AreNotSame(createdClient1, createdClient2);

        bool isRetry1 = capturedParams[0].IsRetry, isRetry2 = capturedParams[1].IsRetry;
        Assert.IsFalse(isRetry1);
        Assert.IsFalse(isRetry2);

        Assert.AreEqual(2, fakeHandler.SendInvokeCount);
    }

    [DataTestMethod]
    [DataRow("408")]
    [DataRow("timeout")]
    [DataRow("error")]
    public async Task HttpClientConfigFetcher_ExternalClient_ShouldObtainNewClientOnUnexpectedResponseOrFailure(string @case)
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(1);

        var logEvents = new List<LogEvent>();
        var fakeLogger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Debug).AsWrapper();

        var capturedParams = new List<(FetchRequest Request, bool IsRetry, HttpClient CreatedClient)>();

        HttpMessageHandler fakeHandler = @case switch
        {
            "408" => new FakeHttpClientHandler(HttpStatusCode.RequestTimeout),
            "timeout" => new FakeHttpClientHandler(HttpStatusCode.OK, delay: timeout + timeout),
            "error" => new ExceptionThrowerHttpClientHandler(new HttpRequestException("Connection reset by peer")),
            _ => throw new NotImplementedException()
        };

        using var configFetcher = new TestHttpClientConfigFetcher((request, isRetry) =>
        {
            var httpClient = new HttpClient(fakeHandler, disposeHandler: false) { Timeout = request.Timeout };
            lock (capturedParams) capturedParams.Add((request, isRetry, httpClient));
            return httpClient;
        });

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        // Act

        FetchResponse fetchResponse = default;
        FetchErrorException? fetchErrorException = null;
        try { fetchResponse = await configFetcher.FetchAsync(fetchRequest, fakeLogger, cancellationToken: default); }
        catch (FetchErrorException ex) { fetchErrorException = ex; }

        switch (@case)
        {
            case "408":
                Assert.AreEqual(HttpStatusCode.RequestTimeout, fetchResponse.StatusCode);
                break;
            case "timeout":
                Assert.IsInstanceOfType(fetchErrorException, typeof(FetchErrorException.Timeout_));
                break;
            case "error":
                Assert.IsInstanceOfType(fetchErrorException, typeof(FetchErrorException.Failure_));
                break;
        }

        // Assert

        Assert.AreEqual(2, capturedParams.Count);

        FetchRequest request1 = capturedParams[0].Request, request2 = capturedParams[1].Request;
        Assert.AreEqual(request1, request2);

        HttpClient? createdClient1 = capturedParams[0].CreatedClient, createdClient2 = capturedParams[1].CreatedClient;
        Assert.AreNotSame(createdClient1, createdClient2);

        bool isRetry1 = capturedParams[0].IsRetry, isRetry2 = capturedParams[1].IsRetry;
        Assert.IsFalse(isRetry1);
        Assert.IsTrue(isRetry2);

        var sendInvokeCount = fakeHandler is ExceptionThrowerHttpClientHandler exceptionThrowerHandler
            ? exceptionThrowerHandler.SendInvokeCount
            : ((FakeHttpClientHandler)fakeHandler).SendInvokeCount;
        Assert.AreEqual(2, sendInvokeCount);
    }

    [DataTestMethod]
    [DataRow(null, false, false)]
    [DataRow(null, false, true)]
    [DataRow(null, true, false)]
    [DataRow(null, true, true)]
    [DataRow(TestETag, false, false)]
    [DataRow(TestETag, false, true)]
    [DataRow(TestETag, true, false)]
    [DataRow(TestETag, true, true)]
    public async Task HttpClientConfigFetcher_ShouldSendUserAgentHeaders_WhenNotRunningInBrowser(string? etag, bool useCustomUri, bool addCustomHeaders)
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(15);

        var fakeHandler = new FakeHttpClientHandler();

        var extraHeaders = addCustomHeaders
            ? new KeyValuePair<string, string>[]
            {
                new("X-Custom", "1"),
                new(DefaultConfigFetcher.ConfigCatUserAgentHeaderName, "x"),
            }
            : null;

        using var configFetcher = new TestHttpClientConfigFetcher(delegate { return new HttpClient(fakeHandler, disposeHandler: false); })
        {
            IsRunningInBrowser = false,
            ExtraHeaders = extraHeaders
        };

        var requestUri = !useCustomUri
            ? ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey)
            : new Uri("https://example.com/configcat?x#y");
        var requestHeaders = DefaultConfigFetcher.GetRequestHeaders(ConfigCatClient.GetProductVersion(PollingModes.ManualPoll));
        var fetchRequest = new FetchRequest(requestUri, etag, requestHeaders, timeout);

        // Act

        await configFetcher.FetchAsync(fetchRequest, cancellationToken: default);

        // Assert

        Assert.AreEqual(1, fakeHandler.Requests.Count);

        var capturedRequest = fakeHandler.Requests[0];
        Assert.AreEqual(requestUri, capturedRequest.RequestUri);
        if (etag is not null)
        {
            Assert.AreEqual(etag, capturedRequest.Headers.IfNoneMatch.ToString());
        }
        else
        {
            Assert.AreEqual(0, capturedRequest.Headers.IfNoneMatch.Count);
        }

        var expectedHeaders = (useCustomUri && addCustomHeaders
            ? requestHeaders.Concat(extraHeaders!)
            : requestHeaders)
            .GroupBy(kvp => kvp.Key)
            .SelectMany(g => g.Select(kvp => new KeyValuePair<string, string>(g.Key, kvp.Value)))
            .ToArray();

        var actualHeaders = (etag is not null
            ? capturedRequest.Headers.Where(kvp => kvp.Key != "If-None-Match")
            : capturedRequest.Headers)
            .SelectMany(kvp => kvp.Value.Select(value => new KeyValuePair<string, string>(kvp.Key, value)))
            .ToArray();

        CollectionAssert.AreEqual(expectedHeaders, actualHeaders);

        Assert.AreEqual(1, fakeHandler.SendInvokeCount);
    }

    [DataTestMethod]
    [DataRow(null, false, false)]
    [DataRow(null, false, true)]
    [DataRow(null, true, false)]
    [DataRow(null, true, true)]
    [DataRow(TestETag, false, false)]
    [DataRow(TestETag, false, true)]
    [DataRow(TestETag, true, false)]
    [DataRow(TestETag, true, true)]
    public async Task HttpClientConfigFetcher_ShouldNotSendUserAgentHeaders_WhenRunningInBrowser(string? etag, bool useCustomUri, bool addCustomHeaders)
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(15);

        var fakeHandler = new FakeHttpClientHandler();

        var extraHeaders = addCustomHeaders
            ? new KeyValuePair<string, string>[]
            {
                new("X-Custom", "1"),
                new(DefaultConfigFetcher.ConfigCatUserAgentHeaderName, "x"),
            }
            : null;

        using var configFetcher = new TestHttpClientConfigFetcher(delegate { return new HttpClient(fakeHandler, disposeHandler: false); })
        {
            IsRunningInBrowser = true,
            ExtraHeaders = extraHeaders
        };

        var requestUri = !useCustomUri
            ? ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey)
            : new Uri("https://example.com/configcat");
        var requestHeaders = DefaultConfigFetcher.GetRequestHeaders(ConfigCatClient.GetProductVersion(PollingModes.ManualPoll));
        var fetchRequest = new FetchRequest(requestUri, etag, requestHeaders, timeout);

        // Act

        await configFetcher.FetchAsync(fetchRequest, cancellationToken: default);

        // Assert

        Assert.AreEqual(1, fakeHandler.Requests.Count);

        var capturedRequest = fakeHandler.Requests[0];
        Assert.IsNotNull(capturedRequest.RequestUri);
        Assert.AreEqual(fetchRequest.Uri.GetLeftPart(UriPartial.Path), capturedRequest.RequestUri.GetLeftPart(UriPartial.Path));

        var parsedQuery = HttpUtility.ParseQueryString(capturedRequest.RequestUri.Query);
        var expectedUserAgentHeaderValue = fetchRequest.Headers.First(kvp => kvp.Key == DefaultConfigFetcher.UserAgentHeaderName).Value;
        CollectionAssert.AreEqual(new[] { expectedUserAgentHeaderValue }, parsedQuery.GetValues(DefaultConfigFetcher.SdkQueryParamName));
        CollectionAssert.AreEqual(etag is not null ? new[] { etag } : null, parsedQuery.GetValues(DefaultConfigFetcher.ETagQueryParamName));

        Assert.AreEqual(0, capturedRequest.Headers.IfNoneMatch.Count);

        var expectedHeaders = (useCustomUri && addCustomHeaders
            ? extraHeaders!
            : Enumerable.Empty<KeyValuePair<string, string>>())
            .GroupBy(kvp => kvp.Key)
            .SelectMany(g => g.Select(kvp => new KeyValuePair<string, string>(g.Key, kvp.Value)))
            .ToArray();

        var actualHeaders = (etag is not null
            ? capturedRequest.Headers.Where(kvp => kvp.Key != "If-None-Match")
            : capturedRequest.Headers)
            .SelectMany(kvp => kvp.Value.Select(value => new KeyValuePair<string, string>(kvp.Key, value)))
            .ToArray();

        CollectionAssert.AreEqual(expectedHeaders, actualHeaders);

        Assert.AreEqual(1, fakeHandler.SendInvokeCount);
    }

    [DataTestMethod]
    [DataRow(HttpStatusCode.Forbidden, RefreshErrorCode.InvalidSdkKey)]
    [DataRow(HttpStatusCode.NotFound, RefreshErrorCode.InvalidSdkKey)]
    [DataRow(HttpStatusCode.Unauthorized, RefreshErrorCode.UnexpectedHttpResponse)]
    [DataRow(HttpStatusCode.BadGateway, RefreshErrorCode.UnexpectedHttpResponse)]
    public async Task HttpClientConfigFetcher_NonSuccessStatusCode_ShouldReturnPassedConfig(HttpStatusCode statusCode, RefreshErrorCode expectedErrorCode)
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler(statusCode);

        using var instance = new DefaultConfigFetcher("x", new Uri("http://example.com"), "1.0",
            new CounterLogger().AsWrapper(), ConfigFetcherHelper.CreateFetcherWithCustomHandler(myHandler), true, false,
            TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
        Assert.AreEqual(expectedErrorCode, actual.ErrorCode);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        if (expectedErrorCode == RefreshErrorCode.InvalidSdkKey)
        {
            Assert.AreNotSame(lastConfig, actual.Config);
        }
        else
        {
            Assert.AreSame(lastConfig, actual.Config);
        }
        Assert.AreSame(lastConfig.Config, actual.Config.Config);
        Assert.AreSame(lastConfig.HttpETag, actual.Config.HttpETag);
        Assert.IsTrue(lastConfig.TimeStamp <= actual.Config.TimeStamp);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HttpClientConfigFetcher_ThrowAnException_ShouldReturnPassedConfig(bool throwHttpRequestException)
    {
        // Arrange

        Exception exception = throwHttpRequestException
            ? new HttpRequestException("Connection reset by peer")
            : new Exception();
        var myHandler = new ExceptionThrowerHttpClientHandler(exception);

        var instance = new DefaultConfigFetcher("x", new Uri("http://example.com"), "1.0",
            new CounterLogger().AsWrapper(), ConfigFetcherHelper.CreateFetcherWithCustomHandler(myHandler), true, false,
            TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
        Assert.AreEqual(RefreshErrorCode.HttpRequestFailure, actual.ErrorCode);
        Assert.IsNotNull(actual.ErrorMessage);
        if (throwHttpRequestException)
        {
            Assert.IsInstanceOfType(actual.ErrorException, typeof(FetchErrorException.Failure_));
            Assert.AreSame(exception, ((FetchErrorException.Failure_)actual.ErrorException!).InnerException);
        }
        else
        {
            Assert.AreSame(exception, actual.ErrorException);
        }
        Assert.AreEqual(lastConfig, actual.Config);
    }

    [TestMethod]
    public async Task HttpClientConfigFetcher_ShouldUseProxySpecifiedViaOptions()
    {
        // Arrange

        var proxy = new WebProxy(TestProxyUrl);

        // Act

        var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            PollingMode = PollingModes.ManualPoll,
            Proxy = proxy
        });

        IWebProxy? actualProxy = null;

        using (client)
        {
            var configFetcher = (HttpClientConfigFetcher)GetConfigFetcherFrom(client, out _);
            actualProxy = (configFetcher.CurrentHandler as HttpClientHandler)?.Proxy;
        }

        // Assert

        Assert.AreEqual(proxy, actualProxy);
    }

    [TestMethod]
    public async Task HttpClientConfigFetcher_ShouldUseHttpTimeoutSpecifiedViaOptions()
    {
        // Arrange

        var timeout = Timeout.InfiniteTimeSpan;

        var capturedParams = new List<(HttpClient Client, HttpMessageHandler Handler)>();

        using var testConfigFetcher = new TestHttpClientConfigFetcher();

        testConfigFetcher.OnCreateHttpClient = (handler, timeout, createHttpClient) =>
        {
            var httpClient = createHttpClient(handler, timeout);
            lock (capturedParams) { capturedParams.Add((httpClient, handler)); }
            return httpClient;
        };

        // Act

        var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            PollingMode = PollingModes.ManualPoll,
            HttpTimeout = timeout
        });

        using (client)
        {
            GetConfigFetcherFrom(client, out var configFetcher);
            configFetcher.GetType().GetField("configFetcher", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(configFetcher, testConfigFetcher);
            await client.ForceRefreshAsync();
        }

        // Assert

        Assert.AreEqual(1, capturedParams.Count);
        Assert.AreEqual(timeout, capturedParams[0].Client.Timeout);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task HttpClientConfigFetcher_ShouldBeDisposedWhenNotOwned(bool ownConfigFetcher)
    {
        // Arrange

        var timeout = TimeSpan.FromSeconds(15);

        var requestUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        var fetchRequest = new FetchRequest(requestUri, TestETag, ArrayUtils.EmptyArray<KeyValuePair<string, string>>(), timeout);

        var configFetcher = ownConfigFetcher ? new TestHttpClientConfigFetcher() : null;

        var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            PollingMode = PollingModes.ManualPoll,
            ConfigFetcher = configFetcher
        });

        // Act

        IConfigCatConfigFetcher actualConfigFetcher;
        using (client)
        {
            actualConfigFetcher = GetConfigFetcherFrom(client, out _);
        }

        // Assert

        if (configFetcher is not null)
        {
            Assert.AreSame(actualConfigFetcher, configFetcher);
            Assert.IsFalse(configFetcher.IsDisposed);
        }
        else
        {
            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(() => actualConfigFetcher.FetchAsync(fetchRequest, default));
        }
    }

    [TestMethod]
    public async Task CustomConfigFetcher_Success()
    {
        // Arrange

        var configJson = File.ReadAllText(Path.Combine(new ConfigLocation.LocalFile("data", "sample_v5.json").GetRealLocation()));

        var responseHeader = new[]
        {
            new KeyValuePair<string, string>("CF-RAY", "CF-12345"),
            new KeyValuePair<string, string>("ETag", "\"abc\""),
        };

        var configFetcherMock = new Mock<IConfigCatConfigFetcher>();
        configFetcherMock
            .Setup(m => m.FetchAsync(It.IsAny<FetchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FetchResponse(HttpStatusCode.OK, reasonPhrase: null, responseHeader, configJson));

        var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            ConfigFetcher = configFetcherMock.Object
        });

        // Act

        string value;
        using (client)
        {
            value = await client.GetValueAsync("stringDefaultCat", "");
        }

        // Assert

        Assert.AreEqual("Cat", value);
    }

    [TestMethod]
    public async Task CustomConfigFetcher_Failure()
    {
        // Arrange

        var logEvents = new List<LogEvent>();
        var logger = LoggingHelper.CreateCapturingLogger(logEvents, LogLevel.Info);

        var responseHeader = new[]
        {
            new KeyValuePair<string, string>("ETag", "\"abc\""),
            new KeyValuePair<string, string>("CF-RAY", "CF-12345"),
        };

        var configFetcherMock = new Mock<IConfigCatConfigFetcher>();
        configFetcherMock
            .Setup(m => m.FetchAsync(It.IsAny<FetchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FetchResponse(HttpStatusCode.Forbidden, "Forbidden", responseHeader));

        var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            ConfigFetcher = configFetcherMock.Object,
            Logger = logger.AsWrapper()
        });

        // Act

        using (client)
        {
            await client.WaitForReadyAsync();
        }

        // Assert

        var errors = logEvents.Where(evt => evt.EventId == 1100).ToArray();
        Assert.AreEqual(1, errors.Length);

        var error = errors[0].Message;
        Assert.AreEqual(2, error.ArgNames.Length);
        Assert.AreEqual(2, error.ArgValues.Length);
        Assert.IsTrue(error.ArgValues[0] is string);
        Assert.IsTrue(error.ArgValues[1] is string);

        var message = errors[0].Message.InvariantFormattedMessage;
        StringAssert.StartsWith(message, "Your SDK Key seems to be wrong: '**********************/****************789012'.");

        var rayId = (string)error.ArgValues[0]!;
        StringAssert.Contains(message, rayId);
    }

    [DataTestMethod]
    [DataRow("", TestSdkKey, TestETag, false)]
    [DataRow("", TestSdkKey, TestETag, true)]
    [DataRow("", "configcat%2dsdk%2d1/PKDVCLf%2dHq%2dh%2dkCzMp%2dL7Q/u28_1qNyZ0Wz%2dldYHIU7%2dg", TestETag, false)]
    [DataRow("", "configcat%2dsdk%2d1/PKDVCLf%2dHq%2dh%2dkCzMp%2dL7Q/u28_1qNyZ0Wz%2dldYHIU7%2dg", TestETag, true)]
    [DataRow("", TestSdkKey, null, false)]
    [DataRow("", TestSdkKey, null, true)]
    [DataRow("?", TestSdkKey, TestETag, false)]
    [DataRow("?", TestSdkKey, TestETag, true)]
    [DataRow("?", TestSdkKey, null, false)]
    [DataRow("?", TestSdkKey, null, true)]
    [DataRow("?ccetag=123", TestSdkKey, TestETag, false)]
    [DataRow("?ccetag=123", TestSdkKey, TestETag, true)]
    [DataRow("?ccetag=123#f", TestSdkKey, TestETag, false)]
    [DataRow("?ccetag=123#f", TestSdkKey, TestETag, true)]
    [DataRow("#f", TestSdkKey, TestETag, false)]
    [DataRow("#f", TestSdkKey, TestETag, true)]
    public void AdjustUriForBrowser_Works(string queryAndFragment, string sdkKey, string? etag, bool useAbsoluteUri)
    {
        // Arrange

        var absoluteUri = ConfigCatClientOptions.GetConfigUri(ConfigCatClientOptions.BaseUrlGlobal, TestSdkKey);
        absoluteUri = new Uri(absoluteUri.GetLeftPart(UriPartial.Path) + queryAndFragment);
        var uri = useAbsoluteUri
            ? absoluteUri
            : new Uri(absoluteUri.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped), UriKind.Relative);

        var requestHeaders = DefaultConfigFetcher.GetRequestHeaders(ConfigCatClient.GetProductVersion(PollingModes.ManualPoll));
        var fetchRequest = new FetchRequest(uri, etag, requestHeaders, Timeout.InfiniteTimeSpan);

        // Act

        HttpClientConfigFetcher.AdjustUriForBrowser(ref uri, fetchRequest);

        // Assert

        var adjustedAbsoluteUri = uri.IsAbsoluteUri ? uri : new Uri(new Uri("https://x"), uri);

        if (useAbsoluteUri)
        {
            Assert.AreEqual(absoluteUri.GetLeftPart(UriPartial.Path), adjustedAbsoluteUri.GetLeftPart(UriPartial.Path));
        }
        else
        {
            Assert.AreEqual(absoluteUri.AbsolutePath, adjustedAbsoluteUri.AbsolutePath);
        }

        var parsedQuery = HttpUtility.ParseQueryString(absoluteUri.Query);
        var expectedUserAgentHeaderValue = fetchRequest.Headers.First(kvp => kvp.Key == DefaultConfigFetcher.UserAgentHeaderName).Value;
        var expectedQueryParams = (etag is not null || parsedQuery.Count > 0
            ? new KeyValuePair<string, string>[]
            {
                new(DefaultConfigFetcher.SdkQueryParamName, expectedUserAgentHeaderValue),
                new(DefaultConfigFetcher.ETagQueryParamName, etag ?? string.Empty),
            }
            : new KeyValuePair<string, string>[]
            {
                new(DefaultConfigFetcher.SdkQueryParamName, expectedUserAgentHeaderValue),
            })
            .Concat(parsedQuery
                .Cast<string>()
                .SelectMany(key => parsedQuery.GetValues(key)!.Select(value => new KeyValuePair<string, string>(key, value)))
            )
            .ToArray();

        parsedQuery = HttpUtility.ParseQueryString(adjustedAbsoluteUri.Query);

        var actualQueryParams = parsedQuery
            .Cast<string>()
            .SelectMany(key => parsedQuery.GetValues(key)!.Select(value => new KeyValuePair<string, string>(key, value)))
            .ToArray();

        CollectionAssert.AreEqual(actualQueryParams, expectedQueryParams);

        Assert.AreEqual(string.Empty, adjustedAbsoluteUri.Fragment);
    }

    [DataTestMethod]
    [DataRow("/", false)]
    [DataRow("/configuration-files/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", false)]
    [DataRow("file:///configuration-files/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", false)]
    [DataRow("http://cdn-global.configcat.com/configuration-files/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", true)]
    [DataRow("https://cdn-global.configcat.com/configuration-files/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", true)]
    [DataRow("https://cdn-global.configcat.com/configuration%2dfiles/configcat%2dsdk%2d1/PKDVCLf%2dHq%2dh%2dkCzMp%2dL7Q/u28_1qNyZ0Wz%2dldYHIU7%2dg/config_v6.json", true)]
    [DataRow("https://cdn-global.configcat.com./configuration-files/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", true)]
    [DataRow("https://cdn-global.configcat.com/configcat-proxy/configuration-files/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", false)]
    [DataRow("https://cdn-global.configcat.com/configuration-files/configcat-proxy/configcat-sdk-1/PKDVCLf-Hq-h-kCzMp-L7Q/u28_1qNyZ0Wz-ldYHIU7-g/config_v6.json", false)]
    public void IsCdnUri_Works(string uri, bool expectedResult)
    {
        // Arrange

        // Act

        var actualResult = ConfigCatClientOptions.IsCdnUri(new Uri(uri, UriKind.RelativeOrAbsolute));

        // Assert

        Assert.AreEqual(expectedResult, actualResult);
    }

    private static IConfigCatConfigFetcher GetConfigFetcherFrom(ConfigCatClient client, out DefaultConfigFetcher configFetcher)
    {
        var configService = client.GetType().GetField("configService", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(client)!;
        configFetcher = (DefaultConfigFetcher)configService.GetType().GetField("ConfigFetcher", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(configService)!;
        return (IConfigCatConfigFetcher)configFetcher.GetType().GetField("configFetcher", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(configFetcher)!;
    }

    private sealed class TestHttpClientConfigFetcher : HttpClientConfigFetcher
    {
        public TestHttpClientConfigFetcher(HttpClientFactory httpClientFactory)
            : base(httpClientFactory) { }

        public TestHttpClientConfigFetcher()
            : base() { }

        public TestHttpClientConfigFetcher(IWebProxy proxy)
            : base(proxy) { }

        public TestHttpClientConfigFetcher(HttpMessageHandler externalHandler)
            : base(externalHandler) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposed = true;
            }
        }

        public bool IsDisposed { get; private set; }

        public bool IsRunningInBrowser { get => this.isRunningInBrowser; set => this.isRunningInBrowser = value; }
        public TimeSpan HandlerRenewalThreshold { get => this.handlerRenewalThreshold; set => this.handlerRenewalThreshold = value; }
        public TimeSpan RequestRetryDelay { get => this.requestRetryDelay; set => this.requestRetryDelay = value; }

        public IEnumerable<KeyValuePair<string, string>>? ExtraHeaders { get; set; }

        internal override HttpClient CreateHttpClient(HttpMessageHandler handler, TimeSpan timeout)
        {
            return OnCreateHttpClient is { } onCreateHttpClient
                ? onCreateHttpClient(handler, timeout, base.CreateHttpClient)
                : base.CreateHttpClient(handler, timeout);
        }

        public Func<HttpMessageHandler, TimeSpan, Func<HttpMessageHandler, TimeSpan, HttpClient>, HttpClient>? OnCreateHttpClient { get; set; }

        protected override void SetRequestHeaders(HttpRequestHeaders httpRequestHeaders, IReadOnlyList<KeyValuePair<string, string>> headers)
        {
            base.SetRequestHeaders(httpRequestHeaders, headers);

            if (ExtraHeaders is not null)
            {
                foreach (var kvp in ExtraHeaders)
                {
                    httpRequestHeaders.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
