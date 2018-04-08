using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConfigCat.Client.Configuration;

namespace ConfigCat.Client.Tests
{

    [TestClass]
    public class ConfigCatClientTests
    {
        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenProjectSecretIsEmpty_ShouldThrowArgumentNullException()
        {
            string projectSecret = string.Empty;

            new ConfigCatClient(projectSecret);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenProjectSecretIsNull_ShouldThrowArgumentNullException()
        {
            string projectSecret = null;

            new ConfigCatClient(projectSecret);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenAutoPollConfigurationProjectSecretIsNull_ShouldThrowArgumentNullException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                ProjectSecret = null
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void CreateAnInstance_WhenConfigurationProjectSecretIsEmpty_ShouldThrowArgumentNullException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                ProjectSecret = string.Empty
            };

            new ConfigCatClient(clientConfiguration);
        }

        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        [TestMethod]
        public void CreateAnInstance_WhenLazyLoadConfigurationTimeToLiveSecondsIsZero_ShouldThrowArgumentOutOfRangeException()
        {
            var clientConfiguration = new LazyLoadConfiguration
            {
                ProjectSecret = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf",
                CacheTimeToLiveSeconds = 0
            };

            new ConfigCatClient(clientConfiguration);
        }        

        [ExpectedException(typeof(ArgumentNullException))]
        [TestMethod]
        public void CreateAnInstance_WhenLoggerFactoryIsNull_ShouldThrowArgumentNullException()
        {
            var clientConfiguration = new AutoPollConfiguration
            {
                ProjectSecret = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf",
                LoggerFactory = null
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
        
        [TestMethod]
        public void CreateAnInstance_WithValidConfiguration_ShouldCreateAnInstance()
        {
            var config = new AutoPollConfiguration
            {
                ProjectSecret = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf"
            };

            new ConfigCatClient(config);
        }

        [TestMethod]
        public void CreateAnInstance_WithProjectToken_ShouldCreateAnInstance()
        {
            string projectSecret = "hsdrTr4sxbHdSgdhHRZds346hdgsS2vfsgf/GsdrTr4sxbHdSgdhHRZds346hdOPsSgvfsgf";

            new ConfigCatClient(projectSecret);
        }        
    }

    internal class ConfigCatClientConfigurationStub : ConfigurationBase { }
}
