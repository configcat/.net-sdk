using System;
using System.Net;
using System.Threading.Tasks;
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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, Mock.Of<IConfigDeserializer>(), false,
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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, Mock.Of<IConfigDeserializer>(), false,
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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, Mock.Of<IConfigDeserializer>(), false,
            TimeSpan.FromSeconds(30));

        var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

        // Act

        var actual = await instance.FetchAsync(lastConfig);

        // Assert

        Assert.IsTrue(actual.IsFailure);
        Assert.IsNotNull(actual.ErrorMessage);
        Assert.IsNull(actual.ErrorException);
        Assert.AreEqual(lastConfig, actual.Config);
    }

    [TestMethod]
    public async Task HttpConfigFetcher_ThrowAnException_ShouldReturnPassedConfig()
    {
        // Arrange

        var exception = new WebException();
        var myHandler = new ExceptionThrowerHttpClientHandler(exception);

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, new ConfigDeserializer(), false,
            TimeSpan.FromSeconds(30));

        var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, new ConfigDeserializer(), false, TimeSpan.FromSeconds(30));

        var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

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

        var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, Mock.Of<IConfigDeserializer>(), false, TimeSpan.FromSeconds(30));

        var lastConfig = new ProjectConfig("{ }", DateTime.UtcNow, "\"ETAG\"");

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
}
