using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class HttpConfigFetcherTests
{
    [TestMethod]
    public async Task HttpConfigFetcher_WithCustomHttpClientHandler_ShouldUsePassedHandler()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler();

        var instance = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), false,
            TimeSpan.FromSeconds(30));

        // Act

        await instance.FetchAsync(ProjectConfig.Empty);

        // Assert

        Assert.AreEqual(1, myHandler.SendInvokeCount);
    }

    [TestMethod]
    public void HttpConfigFetcher_WithCustomHttpClientHandler_HandlersDisposeShouldNotInvoke()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler();

        var instance = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), false,
            TimeSpan.FromSeconds(30));

        // Act

        instance.Dispose();

        // Assert

        Assert.IsFalse(myHandler.Disposed);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_ResponseHttpCodeIsUnexpected_ShouldReturnPassedConfig()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler(HttpStatusCode.Forbidden);

        using var instance = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), false,
            TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
        Assert.AreEqual(RefreshErrorCode.InvalidSdkKey, actual.ErrorCode);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.AreNotSame(lastConfig, actual.Config);
        Assert.AreSame(lastConfig.Config, actual.Config.Config);
        Assert.AreSame(lastConfig.HttpETag, actual.Config.HttpETag);
        Assert.IsTrue(lastConfig.TimeStamp <= actual.Config.TimeStamp);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_ThrowAnException_ShouldReturnPassedConfig()
    {
        // Arrange

        var exception = new WebException();
        var myHandler = new ExceptionThrowerHttpClientHandler(exception);

        var instance = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), false,
            TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
        Assert.AreEqual(RefreshErrorCode.HttpRequestFailure, actual.ErrorCode);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.AreSame(exception, actual.ErrorException);
        Assert.AreEqual(lastConfig, actual.Config);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_OnlyOneFetchAsyncShouldBeInProgressAtATime_Success()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler(HttpStatusCode.OK, "{ }", TimeSpan.FromSeconds(1));

        var instance = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), false, TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var task1 = instance.FetchAsync(lastConfig);
        var task2 = Task.Run(() => instance.FetchAsync(lastConfig));

        var fetchResults = await Task.WhenAll(task1, task2);

        // Assert

        Assert.AreEqual(1, myHandler.SendInvokeCount);
        Assert.IsTrue(fetchResults[0].IsSuccess);
        Assert.IsTrue(fetchResults[1].IsSuccess);
        Assert.AreSame(fetchResults[0].Config, fetchResults[1].Config);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_OnlyOneFetchAsyncShouldBeInProgressAtATime_Failure()
    {
        // Arrange

        var exception = new WebException();
        var myHandler = new ExceptionThrowerHttpClientHandler(exception, TimeSpan.FromSeconds(1));

        var instance = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(myHandler), false, TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var task1 = instance.FetchAsync(lastConfig);
        var task2 = Task.Run(() => instance.FetchAsync(lastConfig));

        var fetchResults = await Task.WhenAll(task1, task2);

        // Assert

        Assert.AreEqual(1, myHandler.SendInvokeCount);
        Assert.IsTrue(fetchResults[0].IsFailure);
        Assert.IsTrue(fetchResults[1].IsFailure);
        Assert.AreSame(exception, fetchResults[0].ErrorException);
        Assert.AreSame(fetchResults[0].ErrorException, fetchResults[1].ErrorException);
    }

    [DataRow(false, false, true)]
    [DataRow(false, false, false)]
    [DataRow(false, false, null)]
    [DataRow(false, true, true)]
    [DataRow(true, false, true)]
    [DataRow(true, true, true)]
    [DataTestMethod]
    public async Task HttpConfigFetcher_FetchAsync_PendingOperationShouldBeJoined(bool cancel1, bool cancel2, bool? bothAsync)
    {
        // Arrange

        const int delayMs = 2000;
        const string configContent = "{ }";
        var configETag = new EntityTagHeaderValue("\"123\"");

        var fakeHandler = new FakeHttpClientHandler(HttpStatusCode.OK, configContent, TimeSpan.FromMilliseconds(delayMs), configETag);
        var configFetcher = new DefaultConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), new HttpClientConfigFetcher(fakeHandler), false, TimeSpan.FromMilliseconds(delayMs * 2));

        using var cts = new CancellationTokenSource(delayMs / 4);

        // Act

        var pendingFetchBefore = configFetcher.pendingFetch;

        var fetchTask1 = bothAsync is true or null
            ? configFetcher.FetchAsync(ProjectConfig.Empty, cancel1 ? cts.Token : CancellationToken.None)
            : Task.Run(() => configFetcher.Fetch(ProjectConfig.Empty));

        var pendingFetchBetween = configFetcher.pendingFetch;

        var fetchTask2 = bothAsync is true or not null
            ? configFetcher.FetchAsync(ProjectConfig.Empty, cancel2 ? cts.Token : CancellationToken.None)
            : Task.Run(() => configFetcher.Fetch(ProjectConfig.Empty));

        var pendingFetchAfter = configFetcher.pendingFetch;

        // Assert

        Assert.IsNull(pendingFetchBefore);

        if (bothAsync is not false)
        {
            Assert.IsNotNull(pendingFetchBetween);
            Assert.AreEqual(pendingFetchBetween, pendingFetchAfter);
        }

        if (bothAsync is not false && cancel1)
        {
            var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => fetchTask1);
            Assert.AreEqual(ex.CancellationToken, cts.Token);
        }
        else
        {
            var fetchResult = await fetchTask1;
            Assert.AreEqual(configETag, new EntityTagHeaderValue(fetchResult.Config.HttpETag!));
        }

        if (bothAsync is not false && cancel2)
        {
            var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => fetchTask2);
            Assert.AreEqual(ex.CancellationToken, cts.Token);
        }
        else
        {
            var fetchResult = await fetchTask2;
            Assert.AreEqual(configETag, new EntityTagHeaderValue(fetchResult.Config.HttpETag!));
        }

        if (bothAsync is not false)
        {
            await pendingFetchBetween!;

            Assert.IsNull(configFetcher.pendingFetch);
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

        using var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            ConfigFetcher = configFetcherMock.Object
        });

        // Act

        var value = await client.GetValueAsync("stringDefaultCat", "");

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

        using var client = new ConfigCatClient("test-67890123456789012/1234567890123456789012", new ConfigCatClientOptions
        {
            ConfigFetcher = configFetcherMock.Object,
            Logger = logger.AsWrapper()
        });

        // Act

        await client.ForceRefreshAsync();

        // Assert

        var errors = logEvents.Where(evt => evt.EventId == 1100).ToArray();
        Assert.AreEqual(1, errors.Length);

        var error = errors[0].Message;
        Assert.AreEqual(1, error.ArgValues.Length);
        Assert.IsTrue(error.ArgValues[0] is string);

        var rayId = (string)error.ArgValues[0]!;
        StringAssert.Contains(errors[0].Message.InvariantFormattedMessage, rayId);
    }
}
