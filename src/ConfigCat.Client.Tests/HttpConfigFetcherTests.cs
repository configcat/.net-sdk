using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests;

[TestClass]
public class HttpConfigFetcherTests
{
    [TestMethod]
    public async Task HttpConfigFetcher_WithCustomHttpClientHandler_ShouldUsePassedHandler()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler();

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, false,
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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, false,
            TimeSpan.FromSeconds(30));

        // Act

        instance.Dispose();

        // Assert

        Assert.IsFalse(myHandler.Disposed);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_ResponseHttpCodeIsUnexpected_ShouldReturnsPassedConfig()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler(HttpStatusCode.Forbidden);

        using var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, false,
            TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, false,
            TimeSpan.FromSeconds(30));

        var lastConfig = ConfigHelper.FromString("{}", timeStamp: ProjectConfig.GenerateTimeStamp(), httpETag: "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.AreSame(exception, actual.ErrorException);
        Assert.AreEqual(lastConfig, actual.Config);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_OnlyOneFetchAsyncShouldBeInProgressAtATime_Success()
    {
        // Arrange

        var myHandler = new FakeHttpClientHandler(HttpStatusCode.OK, "{ }", TimeSpan.FromSeconds(1));

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, false, TimeSpan.FromSeconds(30));

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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, false, TimeSpan.FromSeconds(30));

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

    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    [DataTestMethod]
    public async Task HttpConfigFetcher_FetchAsync_PendingOperationShouldBeJoined(bool cancel1, bool cancel2)
    {
        // Arrange

        const int delayMs = 2000;
        const string configContent = "{ }";
        var configETag = new EntityTagHeaderValue("\"123\"");

        var fakeHandler = new FakeHttpClientHandler(HttpStatusCode.OK, configContent, TimeSpan.FromMilliseconds(delayMs), configETag);
        var configFetcher = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), fakeHandler, false, TimeSpan.FromMilliseconds(delayMs * 2));

        using var cts = new CancellationTokenSource(delayMs / 4);

        // Act

        var pendingFetchBefore = configFetcher.pendingFetch;
        var fetchTask1 = configFetcher.FetchAsync(ProjectConfig.Empty, cancel1 ? cts.Token : CancellationToken.None);
        var pendingFetchBetween = configFetcher.pendingFetch;
        var fetchTask2 = configFetcher.FetchAsync(ProjectConfig.Empty, cancel2 ? cts.Token : CancellationToken.None);
        var pendingFetchAfter = configFetcher.pendingFetch;

        // Assert

        Assert.IsNull(pendingFetchBefore);
        Assert.IsNotNull(pendingFetchBetween);
        Assert.AreEqual(pendingFetchBetween, pendingFetchAfter);

        if (cancel1)
        {
            var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => fetchTask1);
            Assert.AreEqual(ex.CancellationToken, cts.Token);
        }
        else
        {
            var fetchResult = await fetchTask1;
            Assert.AreEqual(configETag, new EntityTagHeaderValue(fetchResult.Config.HttpETag!));
        }

        if (cancel2)
        {
            var ex = await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => fetchTask2);
            Assert.AreEqual(ex.CancellationToken, cts.Token);
        }
        else
        {
            var fetchResult = await fetchTask2;
            Assert.AreEqual(configETag, new EntityTagHeaderValue(fetchResult.Config.HttpETag!));
        }

        await pendingFetchBetween;

        Assert.IsNull(configFetcher.pendingFetch);
    }
}
