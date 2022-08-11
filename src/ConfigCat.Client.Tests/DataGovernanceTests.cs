using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ConfigCat.Client.Configuration;
using ConfigCat.Client.Evaluate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class DataGovernanceTests
    {
        private static readonly Uri GlobalCdnUri = ConfigurationBase.BaseUrlGlobal;
        private static readonly Uri EuOnlyCdnUri = ConfigurationBase.BaseUrlEu;
        private static readonly Uri CustomCdnUri = new Uri("https://custom-cdn.example.com");
        private static readonly Uri ForcedCdnUri = new Uri("https://forced-cdn.example.com");

        [DataRow(DataGovernance.Global, "https://cdn-global.configcat.com")]
        [DataRow(DataGovernance.EuOnly, "https://cdn-eu.configcat.com")]
        [DataRow(null, "https://cdn-global.configcat.com")]
        [DataTestMethod]
        public async Task WithDataGovernanceSetting_ShouldUseProperCdnUrl(DataGovernance dataGovernance, string expectedUrl)
        {
            // Arrange

            var configuration = new ConfigCatClientOptions
            {
                SdkKey = "DEMO",
                DataGovernance = dataGovernance
            };

            byte requestCount = 0;
            var requests = new SortedList<byte, HttpRequestMessage>();

            var handlerMock = new Mock<HttpClientHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((message, _) =>
                {
                    requests.Add(requestCount++, message);
                })
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"p\": {\"u\": \"http://example.com\", \"r\": 0}}"),
                })
                .Verifiable();

            IConfigFetcher fetcher = new HttpConfigFetcher(
                configuration.CreateUri(),
                "DEMO",
                Mock.Of<ILogger>().AsWrapper(),
                handlerMock.Object,
                Mock.Of<IConfigDeserializer>(),
                configuration.IsCustomBaseUrl,
                TimeSpan.FromSeconds(30));

            // Act

            await fetcher.FetchAsync(ProjectConfig.Empty);

            // Assert

            handlerMock.VerifyAll();
            Assert.AreEqual(1, requestCount);
            Assert.AreEqual(new Uri(expectedUrl).Host, requests[0].RequestUri.Host);
        }

        [TestMethod]
        public async Task ClientIsGlobalAndOrgSettingIsGlobal_AllRequestsInvokeGlobalCdn()
        {
            // Arrange

            var fetchConfig = new ConfigCatClientOptions
            {
                SdkKey = "SDK-KEY",
                DataGovernance = DataGovernance.Global
            };

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                { GlobalCdnUri.Host, CreateResponse() }
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.IsTrue(requests.Values.All(message => message.RequestUri.Host == GlobalCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsEuOnlyAndOrgSettingIsGlobal_FirstRequestInvokesEuAfterAllRequestsInvokeGlobal()
        {
            // Arrange

            var fetchConfig = new ConfigCatClientOptions
            {
                SdkKey = "SDK-KEY",
                DataGovernance = DataGovernance.EuOnly
            };

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {GlobalCdnUri.Host, CreateResponse()},
                {EuOnlyCdnUri.Host, CreateResponse()}
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3, requests.Count);
            Assert.AreEqual(EuOnlyCdnUri.Host, requests[1].RequestUri.Host);
            Assert.IsTrue(requests.Values.Skip(1).All(message => message.RequestUri.Host == GlobalCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsGlobalAndOrgSettingIsEuOnly_FirstRequestInvokesGlobalAndRedirectToEuAfterAllRequestsInvokeEu()
        {
            // Arrange

            var fetchConfig = new ConfigCatClientOptions
            {
                SdkKey = "SDK-KEY",
                DataGovernance = DataGovernance.Global
            };

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {GlobalCdnUri.Host, CreateResponse(ConfigurationBase.BaseUrlEu, RedirectMode.Should, false)},
                {EuOnlyCdnUri.Host, CreateResponse(ConfigurationBase.BaseUrlEu, RedirectMode.No, true)}
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3 + 1, requests.Count);
            Assert.AreEqual(GlobalCdnUri.Host, requests[1].RequestUri.Host);
            Assert.AreEqual(EuOnlyCdnUri.Host, requests[2].RequestUri.Host);
            Assert.IsTrue(requests.Values.Skip(2).All(m => m.RequestUri.Host == EuOnlyCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsEuOnlyAndOrgSettingIsEuOnly_AllRequestsInvokeEu()
        {
            // Arrange

            var fetchConfig = new ConfigCatClientOptions
            {
                SdkKey = "SDK-KEY",
                DataGovernance = DataGovernance.EuOnly
            };

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {EuOnlyCdnUri.Host, CreateResponse(ConfigurationBase.BaseUrlEu)}
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3, requests.Count);
            Assert.IsTrue(requests.Values.All(m => m.RequestUri.Host == EuOnlyCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsGlobalAndHasCustomBaseUri_AllRequestInvokeCustomUri()
        {
            // Arrange

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {CustomCdnUri.Host, CreateResponse()}
            };

            var fetchConfig = new ConfigCatClientOptions
            {
                DataGovernance = DataGovernance.Global,
                BaseUrl = CustomCdnUri
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3, requests.Count);
            Assert.IsTrue(requests.Values.All(m => m.RequestUri.Host == CustomCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsEuOnlyAndHasCustomBaseUri_AllRequestInvokeCustomUri()
        {
            // Arrange

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {CustomCdnUri.Host, CreateResponse()}
            };

            var fetchConfig = new ConfigCatClientOptions
            {
                DataGovernance = DataGovernance.EuOnly,
                BaseUrl = CustomCdnUri
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3, requests.Count);
            Assert.IsTrue(requests.Values.All(m => m.RequestUri.Host == CustomCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsGlobalAndOrgIsForced_AllRequestInvokeForcedUri()
        {
            // Arrange

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {GlobalCdnUri.Host, CreateResponse(ForcedCdnUri, RedirectMode.Force, false)},
                {ForcedCdnUri.Host, CreateResponse(ForcedCdnUri, RedirectMode.Force, true)}
            };

            var fetchConfig = new ConfigCatClientOptions
            {
                DataGovernance = DataGovernance.Global
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3 + 1, requests.Count);
            Assert.AreEqual(GlobalCdnUri.Host, requests[1].RequestUri.Host);
            Assert.IsTrue(requests.Values.Skip(1).All(m => m.RequestUri.Host == ForcedCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsEuOnlyAndOrgIsForced_AllRequestInvokeForcedUri()
        {
            // Arrange

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {EuOnlyCdnUri.Host, CreateResponse(ForcedCdnUri, RedirectMode.Force, false)},
                {ForcedCdnUri.Host, CreateResponse(ForcedCdnUri, RedirectMode.Force, true)}
            };

            var fetchConfig = new ConfigCatClientOptions
            {
                DataGovernance = DataGovernance.EuOnly
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3 + 1, requests.Count);
            Assert.AreEqual(EuOnlyCdnUri.Host, requests[1].RequestUri.Host);
            Assert.IsTrue(requests.Values.Skip(1).All(m => m.RequestUri.Host == ForcedCdnUri.Host));
        }

        [TestMethod]
        public async Task ClientIsGlobalAndHasCustomBaseUriAndOrgIsForced_FirstRequestInvokeCustomAndRedirectToForceUriAndAllRequestInvokeForcedUri()
        {
            // Arrange

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {CustomCdnUri.Host, CreateResponse(ForcedCdnUri, RedirectMode.Force, false)},
                {ForcedCdnUri.Host, CreateResponse(ForcedCdnUri, RedirectMode.Force, true)}
            };

            var fetchConfig = new ConfigCatClientOptions
            {
                DataGovernance = DataGovernance.Global,
                BaseUrl = CustomCdnUri
            };

            // Act

            var responses = await Fetch(fetchConfig, responsesRegistry, 3);

            // Assert

            Assert.AreEqual(3 + 1, responses.Count);
            Assert.AreEqual(CustomCdnUri.Host, responses[1].RequestUri.Host);
            Assert.IsTrue(responses.Values.Skip(1).All(m => m.RequestUri.Host == ForcedCdnUri.Host));
        }

        [TestMethod]
        public async Task TestCircuitBreaker_WhenClientIsGlobalRedirectToEuAndRedirectToGlobal_MaximumInvokeCountShouldBeThree()
        {
            // Arrange

            var responsesRegistry = new Dictionary<string, SettingsWithPreferences>
            {
                {GlobalCdnUri.Host, CreateResponse(EuOnlyCdnUri, RedirectMode.Should, false)},
                {EuOnlyCdnUri.Host, CreateResponse(GlobalCdnUri, RedirectMode.Should, false)}
            };

            var fetchConfig = new ConfigCatClientOptions
            {
                DataGovernance = DataGovernance.Global
            };

            // Act

            var requests = await Fetch(fetchConfig, responsesRegistry);

            // Assert

            Assert.AreEqual(3, requests.Count);
        }


        internal static async Task<SortedList<byte, HttpRequestMessage>> Fetch(
            ConfigurationBase fetchConfig,
            Dictionary<string, SettingsWithPreferences> responsesRegistry,
            byte fetchInvokeCount = 1)
        {
            // Arrange

            byte requestCount = 1;
            var requests = new SortedList<byte, HttpRequestMessage>();

            var handlerMock = new Mock<HttpClientHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((message, _) =>
                {
                    requests.Add(requestCount++, message);
                })
                .Returns<HttpRequestMessage, CancellationToken>((message, _) => Task.FromResult(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responsesRegistry[message.RequestUri.Host].Serialize())
                }))
                .Verifiable();

            IConfigFetcher fetcher = new HttpConfigFetcher(
                fetchConfig.CreateUri(),
                "DEMO",
                Mock.Of<ILogger>().AsWrapper(),
                handlerMock.Object,
                new ConfigDeserializer(),
                fetchConfig.IsCustomBaseUrl,
                TimeSpan.FromSeconds(30));

            // Act

            byte i = 0;
            do
            {
                await fetcher.FetchAsync(ProjectConfig.Empty);
                i++;
            } while (fetchInvokeCount > i);

            // Assert

            return requests;
        }

        private static SettingsWithPreferences CreateResponse(Uri url = null, RedirectMode redirectMode = RedirectMode.No, bool withSettings = true)
        {
            return new SettingsWithPreferences
            {
                Preferences = new Preferences
                {
                    Url = (url ?? ConfigurationBase.BaseUrlGlobal).ToString(),
                    RedirectMode = redirectMode
                },
                Settings = withSettings
                ? new Dictionary<string, Setting>
                    {
                        { "myKey", "foo".ToSetting() }
                    }
                : null
            };
        }
    }
}