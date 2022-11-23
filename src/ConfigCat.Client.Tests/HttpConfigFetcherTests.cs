﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
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

            var instance = new HttpConfigFetcher(new Uri("http://example.com"), "1.0", new CounterLogger().AsWrapper(), myHandler, Mock.Of<IConfigDeserializer>(), false,
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
    }
}
