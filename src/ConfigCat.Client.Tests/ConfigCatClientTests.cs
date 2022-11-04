﻿using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Linq;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Runtime.CompilerServices;
using ConfigCat.Client.Utils;
using System.Net.Http;

[assembly: Parallelize(Scope = Microsoft.VisualStudio.TestTools.UnitTesting.ExecutionScope.MethodLevel, Workers = 0)]

#pragma warning disable CS0618 // Type or member is obsolete
namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class ConfigCatClientTests
    {
        Mock<IConfigService> configServiceMock = new Mock<IConfigService>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();
        Mock<IRolloutEvaluator> evaluatorMock = new Mock<IRolloutEvaluator>();
        Mock<IConfigDeserializer> configDeserializerMock = new Mock<IConfigDeserializer>();
        Mock<IConfigFetcher> fetcherMock = new Mock<IConfigFetcher>();

        [TestInitialize]
        public void TestInitialize()
        {
            configServiceMock.Reset();
            loggerMock.Reset();
            evaluatorMock.Reset();
            configDeserializerMock.Reset();
            fetcherMock.Reset();

            loggerMock.Setup(l => l.LogLevel).Returns(LogLevel.Warning);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenSdkKeyIsEmpty_ShouldThrowArgumentNullException()
        {
            string sdkKey = string.Empty;

            new ConfigCatClient(sdkKey);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenSdkKeyIsNull_ShouldThrowArgumentNullException()
        {
            string sdkKey = null;

            new ConfigCatClient(sdkKey);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationSdkKeyIsNull_ShouldThrowArgumentNullException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                SdkKey = null
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationSdkKeyIsNull_ShouldThrowArgumentException_NewApi()
        {
            new ConfigCatClient(options => { options.SdkKey = null; });
        }



        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenConfigurationSdkKeyIsEmpty_ShouldThrowArgumentException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                SdkKey = string.Empty
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationSdkKeyIsEmpty_ShouldThrowArgumentException_NewApi()
        {
            new ConfigCatClient(options => { options.SdkKey = string.Empty; });
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationPollIntervalsZero_ShouldThrowArgumentOutOfRangeException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf",
                PollIntervalSeconds = 0
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationPollIntervalsZero_ShouldThrowArgumentOutOfRangeException_NewApi()
        {
            new ConfigCatClient(options =>
            {
                options.SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf";
                options.PollingMode = PollingModes.AutoPoll(TimeSpan.FromSeconds(0));
            });
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void CreateAnInstance_WhenLazyLoadConfigurationTimeToLiveSecondsIsZero_ShouldThrowArgumentOutOfRangeException()
        {
            var clientConfiguration = new LazyLoadConfiguration
            {
                SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf",
                CacheTimeToLiveSeconds = 0
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void CreateAnInstance_WhenLazyLoadConfigurationTimeToLiveSecondsIsZero_ShouldThrowArgumentOutOfRangeException_NewApi()
        {
            new ConfigCatClient(options =>
            {
                options.SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf";
                options.PollingMode = PollingModes.LazyLoad(TimeSpan.FromSeconds(0));
            });
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenOptionsIsNull_ShouldThrowArgumentNullException()
        {
            Action<ConfigCatClientOptions> config = null;

            new ConfigCatClient(config);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationIsNull_ShouldThrowArgumentNullException()
        {
            AutoPollConfiguration config = null;

            new ConfigCatClient(config);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenLazyLoadConfigurationIsNull_ShouldThrowArgumentNullException()
        {
            LazyLoadConfiguration clientConfiguration = null;

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenManualPollConfigurationIsNull_ShouldThrowArgumentNullException()
        {
            ManualPollConfiguration clientConfiguration = null;

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenLoggerIsNull_ShouldThrowArgumentNullException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf",
                Logger = null
            };

            new ConfigCatClient(clientConfiguration);

        }

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenLoggerIsNull_ShouldThrowArgumentNullException_NewApi()
        {
            new ConfigCatClient(options =>
            {
                options.SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf";
                options.Logger = null;
            });

        }

        [TestMethod]
        public void CreateAnInstance_WithValidConfiguration_ShouldCreateAnInstance()
        {
            var config = new AutoPollConfiguration
            {
                SdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf"
            };

            new ConfigCatClient(config);
        }

        [TestMethod]
        public void CreateAnInstance_WithSdkKey_ShouldCreateAnInstance()
        {
            string sdkKey = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf";

            new ConfigCatClient(sdkKey);
        }

        [TestMethod]
        public void CreateConfigurationBuilderInstance_ShouldCreateAnInstance()
        {
            var builder = ConfigCatClient.Create("SDKKEY");

            Assert.IsNotNull(builder);
        }

        [TestMethod]
        public void GetValue_ConfigServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            configServiceMock
                .Setup(m => m.GetConfig())
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = client.GetValue(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public async Task GetValueAsync_ConfigServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            configServiceMock
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = await client.GetValueAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public void GetValue_EvaluateServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            evaluatorMock
                .Setup(m => m.Evaluate(It.IsAny<Setting>(), It.IsAny<string>(), defaultValue, null, It.IsAny<ProjectConfig>(), It.IsAny<EvaluationDetailsFactory>()))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object, new Hooks());

            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

            // Act

            var actual = client.GetValue(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);

            Assert.AreEqual(1, flagEvaluatedEvents.Count);
            Assert.AreEqual(defaultValue, flagEvaluatedEvents[0].EvaluationDetails.Value);
            Assert.IsTrue(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
        }

        [TestMethod]
        public async Task GetValueAsync_EvaluateServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            evaluatorMock
                .Setup(m => m.Evaluate(It.IsAny<Setting>(), It.IsAny<string>(), defaultValue, null, It.IsAny<ProjectConfig>(), It.IsAny<EvaluationDetailsFactory>()))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object, new Hooks());

            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

            // Act

            var actual = await client.GetValueAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);

            Assert.AreEqual(1, flagEvaluatedEvents.Count);
            Assert.AreEqual(defaultValue, flagEvaluatedEvents[0].EvaluationDetails.Value);
            Assert.IsTrue(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsNotAvailable(bool isAsync)
        {
            // Arrange

            const string key = "boolean";
            const bool defaultValue = false;

            const string cacheKey = "123";
            var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: _ => new ProjectConfig { JsonString = File.ReadAllText(configJsonFilePath), HttpETag = "12345", TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper);
                },
                out var configService, out _);

            var user = new User("a@configcat.com") { Email = "a@configcat.com" };

            // Act

            var actual = isAsync
                ? await client.GetValueDetailsAsync(key, defaultValue, user)
                : client.GetValueDetails(key, defaultValue, user);

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(key, actual.Key);
            Assert.AreEqual(defaultValue, actual.Value);
            Assert.IsTrue(actual.IsDefaultValue);
            Assert.IsNull(actual.VariationId);
            Assert.AreEqual(DateTime.MinValue, actual.FetchTime);
            Assert.AreSame(user, actual.User);
            Assert.IsNotNull(actual.ErrorMessage);
            Assert.IsNull(actual.ErrorException);
            Assert.IsNull(actual.MatchedEvaluationRule);
            Assert.IsNull(actual.MatchedEvaluationPercentageRule);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsAvailableButNoRulesApply(bool isAsync)
        {
            // Arrange

            const string key = "boolean";
            const bool defaultValue = false;

            const string cacheKey = "123";
            var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
            var timeStamp = DateTime.UtcNow;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: _ => new ProjectConfig { JsonString = File.ReadAllText(configJsonFilePath), HttpETag = "12345", TimeStamp = timeStamp },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper);
                },
                out var configService, out _);

            if (isAsync)
            {
                await client.ForceRefreshAsync();
            }
            else
            {
                client.ForceRefresh();
            }

            // Act

            var actual = isAsync
                ? await client.GetValueDetailsAsync(key, defaultValue, user: null)
                : client.GetValueDetails(key, defaultValue, user: null);

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(key, actual.Key);
            Assert.AreEqual(false, actual.Value);
            Assert.IsFalse(actual.IsDefaultValue);
            Assert.AreEqual("a0e56eda", actual.VariationId);
            Assert.AreEqual(timeStamp, actual.FetchTime);
            Assert.IsNull(actual.User);
            Assert.IsNull(actual.ErrorMessage);
            Assert.IsNull(actual.ErrorException);
            Assert.IsNull(actual.MatchedEvaluationRule);
            Assert.IsNull(actual.MatchedEvaluationPercentageRule);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsAvailableAndComparisonRuleApplies(bool isAsync)
        {
            // Arrange

            const string key = "boolean";
            const bool defaultValue = false;

            const string cacheKey = "123";
            var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
            var timeStamp = DateTime.UtcNow;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: _ => new ProjectConfig { JsonString = File.ReadAllText(configJsonFilePath), HttpETag = "12345", TimeStamp = timeStamp },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper);
                },
                out var configService, out _);

            if (isAsync)
            {
                await client.ForceRefreshAsync();
            }
            else
            {
                client.ForceRefresh();
            }

            var user = new User("a@configcat.com") { Email = "a@configcat.com" };

            // Act

            var actual = isAsync
                ? await client.GetValueDetailsAsync(key, defaultValue, user)
                : client.GetValueDetails(key, defaultValue, user);

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(key, actual.Key);
            Assert.AreEqual(true, actual.Value);
            Assert.IsFalse(actual.IsDefaultValue);
            Assert.AreEqual("67787ae4", actual.VariationId);
            Assert.AreEqual(timeStamp, actual.FetchTime);
            Assert.AreSame(user, actual.User);
            Assert.IsNull(actual.ErrorMessage);
            Assert.IsNull(actual.ErrorException);
            Assert.IsNotNull(actual.MatchedEvaluationRule);
            Assert.IsNull(actual.MatchedEvaluationPercentageRule);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task GetValueDetails_ShouldReturnCorrectEvaluationDetails_SettingIsAvailableAndPercentageRuleApplies(bool isAsync)
        {
            // Arrange

            const string key = "boolean";
            const bool defaultValue = false;

            const string cacheKey = "123";
            var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
            var timeStamp = DateTime.UtcNow;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: _ => new ProjectConfig { JsonString = File.ReadAllText(configJsonFilePath), HttpETag = "12345", TimeStamp = timeStamp },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper);
                },
                out var configService, out _);

            if (isAsync)
            {
                await client.ForceRefreshAsync();
            }
            else
            {
                client.ForceRefresh();
            }

            var user = new User("a@example.com") { Email = "a@example.com" };

            // Act

            var actual = isAsync
                ? await client.GetValueDetailsAsync(key, defaultValue, user)
                : client.GetValueDetails(key, defaultValue, user);

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(key, actual.Key);
            Assert.AreEqual(true, actual.Value);
            Assert.IsFalse(actual.IsDefaultValue);
            Assert.AreEqual("67787ae4", actual.VariationId);
            Assert.AreEqual(timeStamp, actual.FetchTime);
            Assert.AreSame(user, actual.User);
            Assert.IsNull(actual.ErrorMessage);
            Assert.IsNull(actual.ErrorException);
            Assert.IsNull(actual.MatchedEvaluationRule);
            Assert.IsNotNull(actual.MatchedEvaluationPercentageRule);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task GetValueDetails_ConfigServiceThrowException_ShouldReturnDefaultValue(bool isAsync)
        {
            // Arrange

            const string key = "Feature";
            const string defaultValue = "Victory for the Firstborn!";
            const string errorMessage = "Error";

            if (isAsync)
            {
                configServiceMock.Setup(m => m.GetConfigAsync()).Throws(new ApplicationException(errorMessage));
            }
            else
            {
                configServiceMock.Setup(m => m.GetConfig()).Throws(new ApplicationException(errorMessage));
            }

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = isAsync
                ? await client.GetValueDetailsAsync(key, defaultValue)
                : client.GetValueDetails(key, defaultValue);

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(key, actual.Key);
            Assert.AreEqual(defaultValue, actual.Value);
            Assert.IsTrue(actual.IsDefaultValue);
            Assert.IsNull(actual.VariationId);
            Assert.AreEqual(DateTime.MinValue, actual.FetchTime);
            Assert.IsNull(actual.User);
            Assert.AreEqual(errorMessage, actual?.ErrorMessage);
            Assert.IsInstanceOfType(actual.ErrorException, typeof(ApplicationException));
            Assert.IsNull(actual.MatchedEvaluationRule);
            Assert.IsNull(actual.MatchedEvaluationPercentageRule);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task GetValueDetails_EvaluateServiceThrowException_ShouldReturnDefaultValue(bool isAsync)
        {
            // Arrange

            const string key = "boolean";
            const string defaultValue = "Victory for the Firstborn!";
            const string errorMessage = "Error";

            const string cacheKey = "123";
            var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");
            var timeStamp = DateTime.UtcNow;

            evaluatorMock
                .Setup(m => m.Evaluate(It.IsAny<Setting>(), It.IsAny<string>(), defaultValue, It.IsAny<User>(), It.IsAny<ProjectConfig>(), It.IsNotNull<EvaluationDetailsFactory>()))
                .Throws(new ApplicationException(errorMessage));

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: _ => new ProjectConfig { JsonString = File.ReadAllText(configJsonFilePath), HttpETag = "12345", TimeStamp = timeStamp },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper);
                },
                evaluatorFactory: _ => evaluatorMock.Object, new Hooks(),
                out var configService, out _);

            if (isAsync)
            {
                await client.ForceRefreshAsync();
            }
            else
            {
                client.ForceRefresh();
            }

            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

            var user = new User("a@example.com") { Email = "a@example.com" };

            // Act

            var actual = isAsync
                ? await client.GetValueDetailsAsync(key, defaultValue, user)
                : client.GetValueDetails(key, defaultValue, user);

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(key, actual.Key);
            Assert.AreEqual(defaultValue, actual.Value);
            Assert.IsTrue(actual.IsDefaultValue);
            Assert.IsNull(actual.VariationId);
            Assert.AreEqual(timeStamp, actual.FetchTime);
            Assert.AreSame(user, actual.User);
            Assert.AreEqual(errorMessage, actual?.ErrorMessage);
            Assert.IsInstanceOfType(actual.ErrorException, typeof(ApplicationException));
            Assert.IsNull(actual.MatchedEvaluationRule);
            Assert.IsNull(actual.MatchedEvaluationPercentageRule);

            Assert.AreEqual(1, flagEvaluatedEvents.Count);
            Assert.AreSame(actual, flagEvaluatedEvents[0].EvaluationDetails);
        }

        [TestMethod]
        public async Task GetAllKeys_ConfigServiceThrowException_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfigAsync()).Throws<Exception>();

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actualKeys = await instance.GetAllKeysAsync();

            // Assert

            Assert.IsNotNull(actualKeys);
            Assert.AreEqual(0, actualKeys.Count());
            loggerMock.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void GetAllKeys_ConfigServiceThrowException_ShouldReturnsWithEmptyArray_Sync()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfig()).Throws<Exception>();

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actualKeys = instance.GetAllKeys();

            // Assert

            Assert.IsNotNull(actualKeys);
            Assert.AreEqual(0, actualKeys.Count());
            loggerMock.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void GetAllKeys_DeserializerThrowException_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            var o = new SettingsWithPreferences();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<string>(), out o))
                .Throws<Exception>();

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actualKeys = instance.GetAllKeys();

            // Assert

            Assert.IsNotNull(actualKeys);
            Assert.AreEqual(0, actualKeys.Count());
            loggerMock.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task GetAllKeysAsync_DeserializeFailed_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            var o = new SettingsWithPreferences();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<string>(), out o))
                .Returns(false);

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actualKeys = await instance.GetAllKeysAsync();

            // Assert

            Assert.IsNotNull(actualKeys);
            Assert.AreEqual(0, actualKeys.Count());
            loggerMock.Verify(m => m.Warning(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void GetAllKeys_DeserializeFailed_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfig()).Returns(ProjectConfig.Empty);
            var o = new SettingsWithPreferences();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<string>(), out o))
                .Returns(false);

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actualKeys = instance.GetAllKeys();

            // Assert

            Assert.IsNotNull(actualKeys);
            Assert.AreEqual(0, actualKeys.Count());
            loggerMock.Verify(m => m.Warning(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void GetVariationId_EvaluateServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            evaluatorMock
                .Setup(m => m.EvaluateVariationId(It.IsAny<Setting>(), It.IsAny<string>(), defaultValue, null, It.IsAny<ProjectConfig>(), It.IsAny<EvaluationDetailsFactory>()))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object, new Hooks());

            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

            // Act

            var actual = client.GetVariationId(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);

            Assert.AreEqual(1, flagEvaluatedEvents.Count);
            Assert.AreEqual(defaultValue, flagEvaluatedEvents[0].EvaluationDetails.VariationId);
            Assert.IsTrue(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
        }

        [TestMethod]
        public async Task GetVariationIdAsync_EvaluateServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            evaluatorMock
                .Setup(m => m.EvaluateVariationId(It.IsAny<Setting>(), It.IsAny<string>(), defaultValue, null, It.IsAny<ProjectConfig>(), It.IsAny<EvaluationDetailsFactory>()))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object, new Hooks());

            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            client.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);

            // Act

            var actual = await client.GetVariationIdAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);

            Assert.AreEqual(1, flagEvaluatedEvents.Count);
            Assert.AreEqual(defaultValue, flagEvaluatedEvents[0].EvaluationDetails.VariationId);
            Assert.IsTrue(flagEvaluatedEvents[0].EvaluationDetails.IsDefaultValue);
        }

        [TestMethod]
        public void GetVariationId_DeserializeFailed_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfig()).Returns(ProjectConfig.Empty);
            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            var o = new SettingsWithPreferences();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<string>(), out o))
                .Returns(false);

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actual = instance.GetAllVariationId();

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(0, actual.Count());
            loggerMock.Verify(m => m.Warning(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task GetVariationIdAsync_DeserializeFailed_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            var o = new SettingsWithPreferences();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<string>(), out o))
                .Returns(false);

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            var actual = await instance.GetAllVariationIdAsync();

            // Assert

            Assert.IsNotNull(actual);
            Assert.AreEqual(0, actual.Count());
            loggerMock.Verify(m => m.Warning(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void GetAllVariationId_ConfigServiceThrowException_ShouldReturnEmptyEnumerable()
        {
            // Arrange

            configServiceMock
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = client.GetAllVariationId(null);

            // Assert

            CollectionAssert.AreEqual(ArrayUtils.EmptyArray<string>(), actual.ToArray());
        }

        [TestMethod]
        public async Task GetAllVariationIdAsync_ConfigServiceThrowException_ShouldReturnEmptyEnumerable()
        {
            // Arrange

            configServiceMock
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = await client.GetAllVariationIdAsync(null);

            // Assert

            CollectionAssert.AreEqual(ArrayUtils.EmptyArray<string>(), actual.ToArray());
        }

        [TestMethod]
        public void Dispose_ConfigServiceIsDisposable_ShouldInvokeDispose()
        {
            // Arrange

            var myMock = new FakeConfigService(Mock.Of<IConfigFetcher>(), new CacheParameters(), Mock.Of<ILogger>().AsWrapper());

            IConfigCatClient instance = new ConfigCatClient(
                myMock,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            instance.Dispose();

            // Assert

            Assert.AreEqual(1, myMock.DisposeCount);
        }

        [TestMethod]
        public async Task ForceRefresh_ShouldInvokeConfigServiceRefreshConfigAsync()
        {
            // Arrange

            configServiceMock.Setup(m => m.RefreshConfigAsync()).Returns(Task.FromResult(0));

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            await instance.ForceRefreshAsync();

            // Assert

            configServiceMock.Verify(m => m.RefreshConfigAsync(), Times.Once);
        }

        [TestMethod]
        public void ForceRefresh_ShouldInvokeConfigServiceRefreshConfig()
        {
            // Arrange

            configServiceMock.Setup(m => m.RefreshConfig());

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            instance.ForceRefresh();

            // Assert

            configServiceMock.Verify(m => m.RefreshConfig(), Times.Once);
        }

        [TestMethod]
        public async Task ForceRefreshAsync_ShouldInvokeConfigServiceRefreshConfigAsync()
        {
            // Arrange

            configServiceMock.Setup(m => m.RefreshConfigAsync()).Returns(Task.FromResult(0));

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            await instance.ForceRefreshAsync();

            // Assert

            configServiceMock.Verify(m => m.RefreshConfigAsync(), Times.Once);
        }


        [TestMethod]
        public async Task ForceRefresh_ConfigServiceThrowException_ShouldNotReThrowTheExceptionAndLogsError()
        {
            // Arrange

            configServiceMock.Setup(m => m.RefreshConfigAsync()).Throws<Exception>();

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            await instance.ForceRefreshAsync();

            // Assert

            loggerMock.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void ForceRefresh_ConfigServiceThrowException_ShouldNotReThrowTheExceptionAndLogsError_Sync()
        {
            // Arrange

            configServiceMock.Setup(m => m.RefreshConfig()).Throws<Exception>();

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            instance.ForceRefresh();

            // Assert

            loggerMock.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ForceRefreshAsync_ConfigServiceThrowException_ShouldNotReThrowTheExceptionAndLogsError()
        {
            // Arrange

            configServiceMock.Setup(m => m.RefreshConfigAsync()).Throws<Exception>();

            IConfigCatClient instance = new ConfigCatClient(
                configServiceMock.Object,
                loggerMock.Object,
                evaluatorMock.Object,
                configDeserializerMock.Object);

            // Act

            await instance.ForceRefreshAsync();

            // Assert

            loggerMock.Verify(m => m.Error(It.IsAny<string>()), Times.Once);
        }

        private static IConfigCatClient CreateClientFromLocalFile(string fileName, User defaultUser = null)
        {
            return new ConfigCatClient(options =>
            {
                options.SdkKey = "localhost";
                options.FlagOverrides = FlagOverrides.LocalFile(
                    Path.Combine("data", fileName),
                    autoReload: false,
                    OverrideBehaviour.LocalOnly
                );
                options.DefaultUser = defaultUser;
            });
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task DefaultUser_GetValue(bool isAsync)
        {
            IConfigCatClient client = CreateClientFromLocalFile("sample_v5.json", new User("a@configcat.com") { Email = "a@configcat.com" });

            var getValueAsync = isAsync
                ? new Func<string, string, User, Task<string>>(client.GetValueAsync)
                : (key, defaultValue, user) => Task.FromResult(client.GetValue(key, defaultValue, user));

            const string key = "stringIsInDogDefaultCat";

            // 1. Checks that default user set in options is used for evaluation 
            Assert.AreEqual("Dog", await getValueAsync(key, string.Empty, null));

            client.ClearDefaultUser();

            // 2. Checks that default user can be cleared
            Assert.AreEqual("Cat", await getValueAsync(key, string.Empty, null));

            client.SetDefaultUser(new User("b@configcat.com") { Email = "b@configcat.com" });

            // 3. Checks that default user set on client is used for evaluation 
            Assert.AreEqual("Dog", await getValueAsync(key, string.Empty, null));

            // 4. Checks that default user can be overridden by parameter
            Assert.AreEqual("Cat", await getValueAsync(key, string.Empty, new User("c@configcat.com") { Email = "c@configcat.com" }));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task DefaultUser_GetAllValues(bool isAsync)
        {
            IConfigCatClient client = CreateClientFromLocalFile("sample_v5.json", new User("a@configcat.com") { Email = "a@configcat.com" });

            var getAllValuesAsync = isAsync
                ? new Func<User, Task<IDictionary<string, object>>>(client.GetAllValuesAsync)
                : user => Task.FromResult(client.GetAllValues(user));

            const string key = "stringIsInDogDefaultCat";

            // 1. Checks that default user set in options is used for evaluation 
            Assert.AreEqual("Dog", (await getAllValuesAsync(null))[key]);

            client.ClearDefaultUser();

            // 2. Checks that default user can be cleared
            Assert.AreEqual("Cat", (await getAllValuesAsync(null))[key]);

            client.SetDefaultUser(new User("b@configcat.com") { Email = "b@configcat.com" });

            // 3. Checks that default user set on client is used for evaluation 
            Assert.AreEqual("Dog", (await getAllValuesAsync(null))[key]);

            // 4. Checks that default user can be overridden by parameter
            Assert.AreEqual("Cat", (await getAllValuesAsync(new User("c@configcat.com") { Email = "c@configcat.com" }))[key]);
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task DefaultUser_GetVariationId(bool isAsync)
        {
            IConfigCatClient client = CreateClientFromLocalFile("sample_variationid_v5.json", new User("a@configcat.com") { Email = "a@configcat.com" });

            var getVariationIdAsync = isAsync
                ? new Func<string, string, User, Task<string>>(client.GetVariationIdAsync)
                : (key, defaultValue, user) => Task.FromResult(client.GetVariationId(key, defaultValue, user));

            const string key = "boolean";

            // 1. Checks that default user set in options is used for evaluation 
            Assert.AreEqual("67787ae4", await getVariationIdAsync(key, string.Empty, null));

            client.ClearDefaultUser();

            // 2. Checks that default user can be cleared
            Assert.AreEqual("a0e56eda", await getVariationIdAsync(key, string.Empty, null));

            client.SetDefaultUser(new User("b@configcat.com") { Email = "b@configcat.com" });

            // 3. Checks that default user set on client is used for evaluation 
            Assert.AreEqual("67787ae4", await getVariationIdAsync(key, string.Empty, null));

            // 4. Checks that default user can be overridden by parameter
            Assert.AreEqual("a0e56eda", await getVariationIdAsync(key, string.Empty, new User("c@example.com") { Email = "c@example.com" }));
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public async Task DefaultUser_GetAllVariationId(bool isAsync)
        {
            IConfigCatClient client = CreateClientFromLocalFile("sample_variationid_v5.json", new User("a@configcat.com") { Email = "a@configcat.com" });

            var getAllVariationIdAsync = isAsync
                ? new Func<User, Task<IEnumerable<string>>>(client.GetAllVariationIdAsync)
                : user => Task.FromResult(client.GetAllVariationId(user));

            // 1. Checks that default user set in options is used for evaluation 
            Assert.IsTrue((await getAllVariationIdAsync(null)).Contains("67787ae4"));

            client.ClearDefaultUser();

            // 2. Checks that default user can be cleared
            Assert.IsTrue((await getAllVariationIdAsync(null)).Contains("a0e56eda"));

            client.SetDefaultUser(new User("b@configcat.com") { Email = "b@configcat.com" });

            // 3. Checks that default user set on client is used for evaluation 
            Assert.IsTrue((await getAllVariationIdAsync(null)).Contains("67787ae4"));

            // 4. Checks that default user can be overridden by parameter
            Assert.IsTrue((await getAllVariationIdAsync(new User("c@example.com") { Email = "c@example.com" })).Contains("a0e56eda"));
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        [DoNotParallelize]
        public void Get_ReturnsCachedInstance_NoWarning(bool passConfigureToSecondGet)
        {
            // Arrange

            var warnings = new List<string>();

            loggerMock.Setup(m => m.Warning(It.IsAny<string>())).Callback(warnings.Add);

            void Configure(ConfigCatClientOptions options)
            {
                options.PollingMode = PollingModes.ManualPoll;
                options.Logger = loggerMock.Object;
            };

            // Act

            using var client1 = ConfigCatClient.Get("test", Configure);
            var warnings1 = warnings.ToArray();

            warnings.Clear();
            using var client2 = ConfigCatClient.Get("test", passConfigureToSecondGet ? Configure : null);
            var warnings2 = warnings.ToArray();

            // Assert

            Assert.AreEqual(1, ConfigCatClient.Instances.Count);
            Assert.AreSame(client1, client2);
            Assert.IsFalse(warnings1.Any(msg => msg.Contains("configuration action is being ignored")));

            if (passConfigureToSecondGet)
            {
                Assert.IsTrue(warnings2.Any(msg => msg.Contains("configuration action is being ignored")));
            }
            else
            {
                Assert.IsFalse(warnings2.Any(msg => msg.Contains("configuration action is being ignored")));
            }
        }

        [TestMethod]
        [DoNotParallelize]
        public void Dispose_CachedInstanceRemoved()
        {
            // Arrange

            var client1 = ConfigCatClient.Get("test", options => options.PollingMode = PollingModes.ManualPoll);

            // Act

            var instanceCount1 = ConfigCatClient.Instances.Count;

            client1.Dispose();

            var instanceCount2 = ConfigCatClient.Instances.Count;

            // Assert

            Assert.AreEqual(1, instanceCount1);
            Assert.AreEqual(0, instanceCount2);
        }

        [TestMethod]
        [DoNotParallelize]
        public void DisposeAll_CachedInstancesRemoved()
        {
            // Arrange

            var client1 = ConfigCatClient.Get("test1", options => options.PollingMode = PollingModes.AutoPoll());
            var client2 = ConfigCatClient.Get("test2", options => options.PollingMode = PollingModes.ManualPoll);

            // Act

            int instanceCount1;

            instanceCount1 = ConfigCatClient.Instances.Count;

            ConfigCatClient.DisposeAll();

            var instanceCount2 = ConfigCatClient.Instances.Count;

            // Assert

            Assert.AreEqual(2, instanceCount1);
            Assert.AreEqual(0, instanceCount2);
        }

        [TestMethod]
        [DoNotParallelize]
        public void CachedInstancesCanBeGCdWhenNoReferencesAreLeft()
        {
            // Arrange

            [MethodImpl(MethodImplOptions.NoInlining)]
            void CreateClients(out int instanceCount)
            {
                // We need to prevent the auto poll service from raising the ClientReady event from its background work loop
                // because that could interfere with this test: when raising the event, the service acquires a strong reference to the client,
                // which would temporarily prevent the client from being GCd. This could break the test in the case of unlucky timing.
                // Setting maxInitWaitTime to zero prevents this because then the event is raised immediately at creation.
                var client1 = ConfigCatClient.Get("test1", options => options.PollingMode = PollingModes.AutoPoll(maxInitWaitTime: TimeSpan.Zero));
                var client2 = ConfigCatClient.Get("test2", options => options.PollingMode = PollingModes.ManualPoll);

                instanceCount = ConfigCatClient.Instances.Count;
            }

            // Act

            CreateClients(out var instanceCount1);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var instanceCount2 = ConfigCatClient.Instances.Count;

            // Assert

            Assert.AreEqual(2, instanceCount1);
            Assert.AreEqual(0, instanceCount2);
        }

        private static IConfigCatClient CreateClientWithMockedFetcher(string cacheKey,
            Mock<ILogger> loggerMock,
            Mock<IConfigFetcher> fetcherMock,
            Func<ProjectConfig, ProjectConfig> onFetch,
            Func<IConfigFetcher, CacheParameters, LoggerWrapper, IConfigService> configServiceFactory,
            out IConfigService configService,
            out IConfigCatCache configCache)
        {
            return CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock, onFetch, configServiceFactory,
                evaluatorFactory: loggerWrapper => new RolloutEvaluator(loggerWrapper), hooks: null,
                out configService, out configCache);
        }

        private static IConfigCatClient CreateClientWithMockedFetcher(string cacheKey,
            Mock<ILogger> loggerMock,
            Mock<IConfigFetcher> fetcherMock,
            Func<ProjectConfig, ProjectConfig> onFetch,
            Func<IConfigFetcher, CacheParameters, LoggerWrapper, IConfigService> configServiceFactory,
            Func<LoggerWrapper, IRolloutEvaluator> evaluatorFactory,
            Hooks hooks,
            out IConfigService configService,
            out IConfigCatCache configCache)
        {
            fetcherMock.Setup(m => m.Fetch(It.IsAny<ProjectConfig>())).Returns(onFetch);
            fetcherMock.Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>())).ReturnsAsync(onFetch);

            var loggerWrapper = new LoggerWrapper(loggerMock.Object);

            configCache = new InMemoryConfigCache();

            var cacheParams = new CacheParameters
            {
                ConfigCache = configCache,
                CacheKey = cacheKey
            };

            configService = configServiceFactory(fetcherMock.Object, cacheParams, loggerWrapper);
            return new ConfigCatClient(configService, loggerMock.Object, evaluatorFactory(loggerWrapper), new ConfigDeserializer(), hooks);
        }

        private static int ParseETagAsInt32(string etag)
        {
            return int.TryParse(etag, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0;
        }

        [TestMethod]
        public async Task OfflineMode_AutoPolling_OfflineToOnlineTransition()
        {
            const string cacheKey = "123";
            int httpETag = 0;
            
            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: cfg => new ProjectConfig { JsonString = "{}", HttpETag = (++httpETag).ToString(CultureInfo.InvariantCulture), TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                    var pollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromMilliseconds(int.MaxValue));
                    return new AutoPollConfigService(pollingMode, fetcherMock.Object, cacheParams, loggerWrapper, isOffline: true);
                },
                out var configService, out _);

            using (client)
            {
                // 1. Checks that client is initialized to offline mode
                Assert.IsTrue(client.IsOffline);
                Assert.AreEqual(default, configService.GetConfig().HttpETag);
                Assert.AreEqual(default, (await configService.GetConfigAsync()).HttpETag);

                // 2. Checks that repeated calls to SetOffline() have no effect
                client.SetOffline();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                // 3. Checks that SetOnline() does enable HTTP calls
                client.SetOnline();

                Assert.IsTrue(((AutoPollConfigService)configService).WaitForInitialization());
                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.AreNotEqual(0, etag1);
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 4. Checks that ForceRefresh() initiates a HTTP call in online mode
                client.ForceRefresh();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Once);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                var etag2 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.IsTrue(etag2 > etag1);
                Assert.AreEqual(etag2, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 5. Checks that ForceRefreshAsync() initiates a HTTP call in online mode
                await client.ForceRefreshAsync();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Once);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Exactly(2));

                var etag3 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.IsTrue(etag3 > etag2);
                Assert.AreEqual(etag3, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));
            }

            // 6. Checks that SetOnline() has no effect after client gets disposed
            client.SetOnline();
            Assert.IsTrue(client.IsOffline);
        }

        [TestMethod]
        public async Task OfflineMode_AutoPolling_OnlineToOfflineTransition()
        {
            const string cacheKey = "123";
            int httpETag = 0;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: cfg => new ProjectConfig { JsonString = "{}", HttpETag = (++httpETag).ToString(CultureInfo.InvariantCulture), TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                    var pollingMode = PollingModes.AutoPoll(pollInterval: TimeSpan.FromMilliseconds(int.MaxValue));
                    return new AutoPollConfigService(pollingMode, fetcherMock.Object, cacheParams, loggerWrapper, isOffline: false);
                },
                out var configService, out _);

            using (client)
            {
                // 1. Checks that client is initialized to online mode
                Assert.IsFalse(client.IsOffline);
                Assert.IsTrue(((AutoPollConfigService)configService).WaitForInitialization());
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.AreNotEqual(0, etag1);
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 2. Checks that repeated calls to SetOnline() have no effect 
                client.SetOnline();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                // 3. Checks that SetOffline() does disable HTTP calls
                client.SetOffline();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 4. Checks that ForceRefresh() does not initiate a HTTP call in offline mode
                client.ForceRefresh();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 5. Checks that ForceRefreshAsync() does not initiate a HTTP call in offline mode
                await client.ForceRefreshAsync();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));
            }

            // 6. Checks that SetOnline() has no effect after client gets disposed
            client.SetOnline();
            Assert.IsTrue(client.IsOffline);
        }

        [TestMethod]
        public async Task OfflineMode_LazyLoading_OfflineToOnlineTransition()
        {
            const string cacheKey = "123";
            int httpETag = 0;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: cfg => new ProjectConfig { JsonString = "{}", HttpETag = (++httpETag).ToString(CultureInfo.InvariantCulture), TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                    var pollingMode = PollingModes.LazyLoad(cacheTimeToLive: TimeSpan.FromMilliseconds(int.MaxValue));
                    return new LazyLoadConfigService(fetcherMock.Object, cacheParams, loggerWrapper, pollingMode.CacheTimeToLive, isOffline: true);
                },
                out var configService, out _);

            using (client)
            {
                // 1. Checks that client is initialized to offline mode
                Assert.IsTrue(client.IsOffline);
                Assert.AreEqual(default, configService.GetConfig().HttpETag);
                Assert.AreEqual(default, (await configService.GetConfigAsync()).HttpETag);

                // 2. Checks that repeated calls to SetOffline() have no effect
                client.SetOffline();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                // 3. Checks that SetOnline() does enable HTTP calls
                client.SetOnline();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Once);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                Assert.AreNotEqual(0, etag1);
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 4. Checks that ForceRefresh() initiates a HTTP call in online mode
                client.ForceRefresh();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Exactly(2));
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                var etag2 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.IsTrue(etag2 > etag1);
                Assert.AreEqual(etag2, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 5. Checks that ForceRefreshAsync() initiates a HTTP call in online mode
                await client.ForceRefreshAsync();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Exactly(2));
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                var etag3 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.IsTrue(etag3 > etag2);
                Assert.AreEqual(etag3, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));
            }

            // 6. Checks that SetOnline() has no effect after client gets disposed
            client.SetOnline();
            Assert.IsTrue(client.IsOffline);
        }

        [TestMethod]
        public async Task OfflineMode_LazyLoading_OnlineToOfflineTransition()
        {
            const string cacheKey = "123";
            int httpETag = 0;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: cfg => new ProjectConfig { JsonString = "{}", HttpETag = (++httpETag).ToString(CultureInfo.InvariantCulture), TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    // Make sure that config is fetched only once (Task.Delay won't accept a larger interval than int.MaxValue msecs...)
                    var pollingMode = PollingModes.LazyLoad(cacheTimeToLive: TimeSpan.FromMilliseconds(int.MaxValue));
                    return new LazyLoadConfigService(fetcherMock.Object, cacheParams, loggerWrapper, pollingMode.CacheTimeToLive, isOffline: false);
                },
                out var configService, out var configCache);

            using (client)
            {
                // 1. Checks that client is initialized to online mode
                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                var etag1 = ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                Assert.AreNotEqual(default, etag1);
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 2. Checks that repeated calls to SetOnline() have no effect 
                client.SetOnline();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                // 3. Checks that SetOffline() does disable HTTP calls
                client.SetOffline();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                // We make sure manually that the cached config is expired for the next GetConfig() call
                var cachedConfig = configCache.Get(cacheKey);
                cachedConfig = new ProjectConfig(
                    cachedConfig.JsonString,
                    cachedConfig.TimeStamp - TimeSpan.FromMilliseconds(int.MaxValue * 2.0),
                    cachedConfig.HttpETag);
                configCache.Set(cacheKey, cachedConfig);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 4. Checks that ForceRefresh() does not initiate a HTTP call in offline mode
                client.ForceRefresh();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 5. Checks that ForceRefreshAsync() does not initiate a HTTP call in offline mode
                await client.ForceRefreshAsync();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));
            }

            // 6. Checks that SetOnline() has no effect after client gets disposed
            client.SetOnline();
            Assert.IsTrue(client.IsOffline);
        }

        [TestMethod]
        public async Task OfflineMode_ManualPolling_OfflineToOnlineTransition()
        {
            const string cacheKey = "123";
            int httpETag = 0;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: cfg => new ProjectConfig { JsonString = "{}", HttpETag = (++httpETag).ToString(CultureInfo.InvariantCulture), TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper, isOffline: true);
                },
                out var configService, out _);

            using (client)
            {
                // 1. Checks that client is initialized to offline mode
                Assert.IsTrue(client.IsOffline);
                Assert.AreEqual(default, configService.GetConfig().HttpETag);
                Assert.AreEqual(default, (await configService.GetConfigAsync()).HttpETag);

                // 2. Checks that repeated calls to SetOffline() have no effect
                client.SetOffline();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                // 3. Checks that SetOnline() does enable HTTP calls
                client.SetOnline();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                Assert.AreEqual(0, etag1);
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 4. Checks that ForceRefresh() initiates a HTTP call in online mode
                client.ForceRefresh();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Once);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                var etag2 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.AreNotEqual(etag2, etag1);
                Assert.AreEqual(etag2, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 5. Checks that ForceRefreshAsync() initiates a HTTP call in online mode
                await client.ForceRefreshAsync();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Once);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Once);

                var etag3 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                Assert.IsTrue(etag3 > etag2);
                Assert.AreEqual(etag3, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));
            }

            // 6. Checks that SetOnline() has no effect after client gets disposed
            client.SetOnline();
            Assert.IsTrue(client.IsOffline);
        }

        [TestMethod]
        public async Task OfflineMode_ManualPolling_OnlineToOfflineTransition()
        {
            const string cacheKey = "123";
            int httpETag = 0;

            var client = CreateClientWithMockedFetcher(cacheKey, loggerMock, fetcherMock,
                onFetch: cfg => new ProjectConfig { JsonString = "{}", HttpETag = (++httpETag).ToString(CultureInfo.InvariantCulture), TimeStamp = DateTime.UtcNow },
                configServiceFactory: (fetcher, cacheParams, loggerWrapper) =>
                {
                    return new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper, isOffline: false);
                },
                out var configService, out var configCache);

            using (client)
            {
                // 1. Checks that client is initialized to online mode
                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                var etag1 = ParseETagAsInt32(configService.GetConfig().HttpETag);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                Assert.AreEqual(0, etag1);
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 2. Checks that repeated calls to SetOnline() have no effect 
                client.SetOnline();

                Assert.IsFalse(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                // 3. Checks that SetOffline() does disable HTTP calls
                client.SetOffline();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 4. Checks that ForceRefresh() does not initiate a HTTP call in offline mode
                client.ForceRefresh();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));

                // 5. Checks that ForceRefreshAsync() does not initiate a HTTP call in offline mode
                await client.ForceRefreshAsync();

                Assert.IsTrue(client.IsOffline);
                fetcherMock.Verify(m => m.Fetch(It.IsAny<ProjectConfig>()), Times.Never);
                fetcherMock.Verify(m => m.FetchAsync(It.IsAny<ProjectConfig>()), Times.Never);

                Assert.AreEqual(etag1, ParseETagAsInt32(configService.GetConfig().HttpETag));
                Assert.AreEqual(etag1, ParseETagAsInt32((await configService.GetConfigAsync()).HttpETag));
            }

            // 6. Checks that SetOnline() has no effect after client gets disposed
            client.SetOnline();
            Assert.IsTrue(client.IsOffline);
        }

        [TestMethod]
        public async Task Hooks_MockedClientRaisesEvents()
        {
            const string cacheKey = "123";
            var configJsonFilePath = Path.Combine("data", "sample_variationid_v5.json");

            var clientReadyEventCount = 0;
            var configChangedEvents = new List<ConfigChangedEventArgs>();
            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            var errorEvents = new List<ConfigCatClientErrorEventArgs>();
            var beforeClientDisposeEventCount = 0;

            var hooks = new Hooks();
            hooks.ClientReady += (s, e) => clientReadyEventCount++;
            hooks.ConfigChanged += (s, e) => configChangedEvents.Add(e);
            hooks.FlagEvaluated += (s, e) => flagEvaluatedEvents.Add(e);
            hooks.Error += (s, e) => errorEvents.Add(e);
            hooks.BeforeClientDispose += (s, e) => beforeClientDisposeEventCount++;

            var loggerWrapper = new LoggerWrapper(loggerMock.Object, hooks);

            const string errorMessage = "Error occured during fetching.";
            var errorException = new HttpRequestException();

            var onFetch = (ProjectConfig latestConfig) =>
            {
                loggerWrapper.Error(errorMessage, errorException);
                return latestConfig;
            };
            fetcherMock.Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>())).ReturnsAsync(onFetch);

            var configCache = new InMemoryConfigCache();

            var cacheParams = new CacheParameters
            {
                ConfigCache = configCache,
                CacheKey = cacheKey
            };

            var configService = new ManualPollConfigService(fetcherMock.Object, cacheParams, loggerWrapper, hooks: hooks);

            // 1. Client gets created
            var client = new ConfigCatClient(configService, loggerMock.Object, new RolloutEvaluator(loggerWrapper), new ConfigDeserializer(), hooks);

            Assert.AreEqual(1, clientReadyEventCount);
            Assert.AreEqual(0, configChangedEvents.Count);
            Assert.AreEqual(0, flagEvaluatedEvents.Count);
            Assert.AreEqual(0, errorEvents.Count);
            Assert.AreEqual(0, beforeClientDisposeEventCount);

            // 2. Fetch fails
            await client.ForceRefreshAsync();

            Assert.AreEqual(0, configChangedEvents.Count);
            Assert.AreEqual(1, errorEvents.Count);
            Assert.AreSame(errorMessage, errorEvents[0].Message);
            Assert.AreSame(errorException, errorEvents[0].Exception);

            // 3. Fetch succeeds
            var config = new ProjectConfig { JsonString = File.ReadAllText(configJsonFilePath), HttpETag = "12345", TimeStamp = DateTime.UtcNow };

            onFetch = _ => config;
            fetcherMock.Reset();
            fetcherMock.Setup(m => m.FetchAsync(It.IsAny<ProjectConfig>())).ReturnsAsync(onFetch);

            await client.ForceRefreshAsync();

            Assert.AreEqual(1, configChangedEvents.Count);
            Assert.AreSame(config, configChangedEvents[0].NewConfig);

            // 4. All flags are evaluated
            var keys = await client.GetAllKeysAsync();
            var evaluationDetails = new List<EvaluationDetails>();
            foreach (var key in keys)
            {
                evaluationDetails.Add(await client.GetValueDetailsAsync<object>(key, defaultValue: ""));
            }

            Assert.AreEqual(evaluationDetails.Count, flagEvaluatedEvents.Count);
            CollectionAssert.AreEqual(evaluationDetails, flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());

            // 5. Client gets disposed of
            client.Dispose();

            Assert.AreEqual(1, clientReadyEventCount);
            Assert.AreEqual(1, configChangedEvents.Count);
            Assert.AreEqual(evaluationDetails.Count, flagEvaluatedEvents.Count);
            Assert.AreEqual(1, errorEvents.Count);
            Assert.AreEqual(1, beforeClientDisposeEventCount);
        }

        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        [DoNotParallelize]
        public async Task Hooks_RealClientRaisesEvents(bool subscribeViaOptions)
        {
            var clientReadyCallCount = 0;
            var configChangedEvents = new List<ConfigChangedEventArgs>();
            var flagEvaluatedEvents = new List<FlagEvaluatedEventArgs>();
            var errorEvents = new List<ConfigCatClientErrorEventArgs>();
            var beforeClientDisposeCallCount = 0;

            EventHandler handleClientReady = (s, e) => clientReadyCallCount++;
            EventHandler<ConfigChangedEventArgs> handleConfigChanged = (s, e) => configChangedEvents.Add(e);
            EventHandler<FlagEvaluatedEventArgs> handleFlagEvaluated = (s, e) => flagEvaluatedEvents.Add(e);
            EventHandler<ConfigCatClientErrorEventArgs> handleError = (s, e) => errorEvents.Add(e);
            EventHandler handleBeforeClientDispose = (s, e) => beforeClientDisposeCallCount++;

            void Subscribe(IProvidesHooks hooks)
            {
                hooks.ClientReady += handleClientReady;
                hooks.ConfigChanged += handleConfigChanged;
                hooks.FlagEvaluated += handleFlagEvaluated;
                hooks.Error += handleError;
                hooks.BeforeClientDispose += handleBeforeClientDispose;
            }

            void Unsubscribe(IProvidesHooks hooks)
            {
                hooks.ClientReady -= handleClientReady;
                hooks.ConfigChanged -= handleConfigChanged;
                hooks.FlagEvaluated -= handleFlagEvaluated;
                hooks.Error -= handleError;
                hooks.BeforeClientDispose -= handleBeforeClientDispose;
            }

            // 1. Client gets created
            var client = ConfigCatClient.Get(BasicConfigCatClientIntegrationTests.SDKKEY, options =>
            {
                if (subscribeViaOptions)
                {
                    Subscribe(options);
                    Unsubscribe(options);
                    Subscribe(options);
                    Subscribe(options);
                }

                options.PollingMode = PollingModes.ManualPoll;
            });

            if (!subscribeViaOptions)
            {
                Subscribe(client);
                Unsubscribe(client);
                Subscribe(client);
                Subscribe(client);
            }

            Assert.AreEqual(subscribeViaOptions ? 2 : 0, clientReadyCallCount);
            Assert.AreEqual(0, configChangedEvents.Count);
            Assert.AreEqual(0, flagEvaluatedEvents.Count);
            Assert.AreEqual(0, errorEvents.Count);
            Assert.AreEqual(0, beforeClientDisposeCallCount);

            // 2. Fetch succeeds
            await client.ForceRefreshAsync();

            Assert.AreEqual(2, configChangedEvents.Count);
            Assert.AreNotEqual(ProjectConfig.Empty, configChangedEvents[0].NewConfig);
            Assert.AreSame(configChangedEvents[0], configChangedEvents[1]);

            // 3. Non-existent flag is evaluated

            const string invalidKey = "<invalid-key>";

            await client.GetValueAsync(invalidKey, defaultValue: (object)null);

            Assert.AreEqual(2, errorEvents.Count);
            Assert.IsNotNull(errorEvents[0].Message);
            Assert.IsNull(errorEvents[0].Exception);
            Assert.AreSame(errorEvents[0], errorEvents[1]);

            Assert.AreEqual(2, flagEvaluatedEvents.Count);
            Assert.AreEqual(invalidKey, flagEvaluatedEvents[0].EvaluationDetails.Key);
            Assert.AreEqual(errorEvents[0].Message, flagEvaluatedEvents[0].EvaluationDetails.ErrorMessage);
            Assert.IsNull(errorEvents[0].Exception);
            Assert.AreSame(flagEvaluatedEvents[0], flagEvaluatedEvents[1]);

            flagEvaluatedEvents.Clear();

            // 4. All flags are evaluated
            var keys = await client.GetAllKeysAsync();
            var evaluationDetails = new List<EvaluationDetails>();
            foreach (var key in keys)
            {
                evaluationDetails.Add(await client.GetValueDetailsAsync<object>(key, defaultValue: ""));
            }

            Assert.AreEqual(evaluationDetails.Count * 2, flagEvaluatedEvents.Count);
            CollectionAssert.AreEqual(
                evaluationDetails.SelectMany(ed => Enumerable.Repeat(ed, 2)).ToArray(),
                flagEvaluatedEvents.Select(e => e.EvaluationDetails).ToArray());

            // 5. Client gets disposed of
            client.Dispose();

            Assert.AreEqual(subscribeViaOptions ? 2 : 0, clientReadyCallCount);
            Assert.AreEqual(2, configChangedEvents.Count);
            Assert.AreEqual(evaluationDetails.Count * 2, flagEvaluatedEvents.Count);
            Assert.AreEqual(2, errorEvents.Count);
            Assert.AreEqual(2, beforeClientDisposeCallCount);
        }

        internal class FakeConfigService : ConfigServiceBase, IConfigService
        {
            public byte DisposeCount { get; private set; }

            public FakeConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper log)
                : base(configFetcher, cacheParameters, log, isOffline: false, hooks: null)
            {
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }

            public Task<ProjectConfig> GetConfigAsync()
            {
                return Task.FromResult(ProjectConfig.Empty);
            }

            public override Task RefreshConfigAsync()
            {
                return Task.FromResult(0);
            }

            public ProjectConfig GetConfig()
            {
                return ProjectConfig.Empty;
            }

            public override void RefreshConfig()
            { }
        }
    }
}
