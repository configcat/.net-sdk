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
using System.IO;
using System.Runtime.CompilerServices;

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
        public void CachedInstancesCanBeGCdWhenNoReferencesAreLeft()
        {
            // Arrange

            [MethodImpl(MethodImplOptions.NoInlining)]
            void CreateClients(out int instanceCount)
            {
                var client1 = ConfigCatClient.Get("test1", options => options.PollingMode = PollingModes.AutoPoll());
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

        internal class FakeConfigService : ConfigServiceBase, IConfigService
        {
            public byte DisposeCount { get; private set; }

            public FakeConfigService(IConfigFetcher configFetcher, CacheParameters cacheParameters, LoggerWrapper log) : base(configFetcher, cacheParameters, log)
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
