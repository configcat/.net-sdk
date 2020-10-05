using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class DataGovernanceTests
    {
        Uri GlobalUri = new Uri(ConfigurationBase.BaseUrlGlobal);

        [TestMethod]
        public async Task WithDefaultDataGovernanceSetting_ShouldUseGlobalCdnEveryRequests()
        {
            var configuration = new AutoPollConfiguration
            {
                SdkKey = "DEMO"
            };

            var handlerMock = new Mock<HttpClientHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}"),
                })
                .Verifiable();

            IConfigFetcher fetcher = new HttpConfigFetcher(configuration.CreateUri(), "DEMO", Mock.Of<ILogger>(), handlerMock.Object, configuration.IsCustomBaseUrl);

            // Act

            await fetcher.Fetch(ProjectConfig.Empty);

            // Assert

            handlerMock.VerifyAll();
            // TODO invoke count + URL check
        }
    }
}