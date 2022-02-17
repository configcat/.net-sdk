using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Linq;
using ConfigCat.Client.Cache;
using ConfigCat.Client.Configuration;
using System.Collections.Generic;

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

        [TestInitialize]
        public void TestInitialize()
        {
            configServiceMock.Reset();
            loggerMock.Reset();
            evaluatorMock.Reset();
            configDeserializerMock.Reset();
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
        public void CreateAnInstance_WhenAutoPollConfigurationSdkKeyIsNull_ShouldThrowArgumentNullException_NewApi()
        {
            new ConfigCatClient(options => { options.SdkKey = null; });
        }



        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenConfigurationSdkKeyIsEmpty_ShouldThrowArgumentNullException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                SdkKey = string.Empty
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationSdkKeyIsEmpty_ShouldThrowArgumentNullException_NewApi()
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
        public async Task GetValue_ConfigServiceThrowException_ShouldReturnDefaultValue()
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
        public void GetValue_ConfigServiceThrowException_ShouldReturnDefaultValue_Sync()
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
                .Setup(m => m.Evaluate(It.IsAny<IDictionary<string, Setting>>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = client.GetValue(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public async Task GetValueAsync_EvaluateServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            evaluatorMock
                .Setup(m => m.Evaluate(It.IsAny<IDictionary<string, Setting>>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = await client.GetValueAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
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
                .Setup(m => m.EvaluateVariationId(It.IsAny<IDictionary<string, Setting>>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = client.GetVariationId(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public async Task GetVariationIdAsync_EvaluateServiceThrowException_ShouldReturnDefaultValue()
        {
            // Arrange

            const string defaultValue = "Victory for the Firstborn!";

            evaluatorMock
                .Setup(m => m.EvaluateVariationId(It.IsAny<IDictionary<string, Setting>>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configServiceMock.Object, loggerMock.Object, evaluatorMock.Object, configDeserializerMock.Object);

            // Act

            var actual = await client.GetVariationIdAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public void GetVariationId_DeserializeFailed_ShouldReturnsWithEmptyArray()
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

            Assert.AreEqual(Enumerable.Empty<string>(), actual);
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

            Assert.AreEqual(Enumerable.Empty<string>(), actual);
        }

        [TestMethod]
        public void Dispose_ConfigServiceIsDisposable_ShouldInvokeDispose()
        {
            // Arrange
            
            var myMock = new FakeConfigService(Mock.Of<IConfigFetcher>(), new CacheParameters(), Mock.Of<ILogger>());

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

        internal class FakeConfigService : ConfigServiceBase, IConfigService
        {
            public byte DisposeCount { get; private set; }

            public FakeConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, ILogger log) : base(configFetcher, cacheParameters, log)
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

            public Task RefreshConfigAsync()
            {
                return Task.FromResult(0);
            }

            public ProjectConfig GetConfig()
            {
                return ProjectConfig.Empty;
            }

            public void RefreshConfig()
            { }
        }
    }
}
