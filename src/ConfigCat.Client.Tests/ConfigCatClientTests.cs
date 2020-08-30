﻿using ConfigCat.Client.ConfigService;
using ConfigCat.Client.Evaluate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ConfigCat.Client.Tests
{

    [TestClass]
    public class ConfigCatClientTests
    {
        Mock<IConfigService> configService = new Mock<IConfigService>();
        Mock<ILogger> loggerMock = new Mock<ILogger>();
        Mock<IRolloutEvaluator> evaluateMock = new Mock<IRolloutEvaluator>();
        Mock<IConfigDeserializer> deserializerMock = new Mock<IConfigDeserializer>();

        [TestInitialize]
        public void TestInitialize()
        {
            configService.Reset();
            loggerMock.Reset();
            evaluateMock.Reset();
            deserializerMock.Reset();
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

            configService
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

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

            configService
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

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

            evaluateMock
                .Setup(m => m.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

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

            evaluateMock
                .Setup(m => m.Evaluate(It.IsAny<ProjectConfig>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

            // Act

            var actual = await client.GetValueAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public void GetAllKeys_ConfigServiceThrowException_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            var configServiceMock = new Mock<IConfigService>();
            var loggerMock = new Mock<ILogger>();
            var evaluatorMock = new Mock<IRolloutEvaluator>();
            var configDeserializerMock = new Mock<IConfigDeserializer>();

            configServiceMock.Setup(m => m.GetConfigAsync()).Throws<Exception>();

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

            var configServiceMock = new Mock<IConfigService>();
            var loggerMock = new Mock<ILogger>();
            var evaluatorMock = new Mock<IRolloutEvaluator>();
            var configDeserializerMock = new Mock<IConfigDeserializer>();

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            IDictionary<string, Setting> o = new Dictionary<string, Setting>();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<ProjectConfig>(), out o))
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
        public void GetAllKeys_DeserializeFailed_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            var configServiceMock = new Mock<IConfigService>();
            var loggerMock = new Mock<ILogger>();
            var evaluatorMock = new Mock<IRolloutEvaluator>();
            var configDeserializerMock = new Mock<IConfigDeserializer>();

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            IDictionary<string, Setting> o = new Dictionary<string, Setting>();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<ProjectConfig>(), out o))
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

            evaluateMock
                .Setup(m => m.EvaluateVariationId(It.IsAny<ProjectConfig>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

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

            evaluateMock
                .Setup(m => m.EvaluateVariationId(It.IsAny<ProjectConfig>(), It.IsAny<string>(), defaultValue, null))
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

            // Act

            var actual = await client.GetVariationIdAsync(null, defaultValue);

            // Assert

            Assert.AreEqual(defaultValue, actual);
        }

        [TestMethod]
        public void GetVariationId_DeserializeFailed_ShouldReturnsWithEmptyArray()
        {
            // Arrange

            var configServiceMock = new Mock<IConfigService>();
            var loggerMock = new Mock<ILogger>();
            var evaluatorMock = new Mock<IRolloutEvaluator>();
            var configDeserializerMock = new Mock<IConfigDeserializer>();

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            IDictionary<string, Setting> o = new Dictionary<string, Setting>();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<ProjectConfig>(), out o))
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

            var configServiceMock = new Mock<IConfigService>();
            var loggerMock = new Mock<ILogger>();
            var evaluatorMock = new Mock<IRolloutEvaluator>();
            var configDeserializerMock = new Mock<IConfigDeserializer>();

            configServiceMock.Setup(m => m.GetConfigAsync()).ReturnsAsync(ProjectConfig.Empty);
            IDictionary<string, Setting> o = new Dictionary<string, Setting>();
            configDeserializerMock
                .Setup(m => m.TryDeserialize(It.IsAny<ProjectConfig>(), out o))
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

            configService
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

            // Act

            var actual = client.GetAllVariationId(null);

            // Assert

            Assert.AreEqual(Enumerable.Empty<string>(), actual);
        }

        [TestMethod]
        public async Task GetAllVariationIdAsync_ConfigServiceThrowException_ShouldReturnEmptyEnumerable()
        {
            // Arrange

            configService
                .Setup(m => m.GetConfigAsync())
                .Throws<Exception>();

            var client = new ConfigCatClient(configService.Object, loggerMock.Object, evaluateMock.Object, deserializerMock.Object);

            // Act

            var actual = await client.GetAllVariationIdAsync(null);

            // Assert

            Assert.AreEqual(Enumerable.Empty<string>(), actual);
        }
    }
}
