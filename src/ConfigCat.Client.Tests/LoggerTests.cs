using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void CreateConsoleLogger_ShouldInstanceOfILogger()
        {
            var factory = new ConsoleLoggerFactory();

            var instance = factory.GetLogger("dummy");

            Assert.IsInstanceOfType(instance, typeof(ILogger));

            instance.Debug(null);
            instance.Information(null);
            instance.Warning(null);
            instance.Error(null);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsDebug_ShouldInvokeErrorOrWarnOrInfoOrDebug()
        {
            var logger = new MyLogger("unittest", LogLevel.Debug);

            logger.Debug(null);
            logger.Information(null);
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(4, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsInfo_ShouldInvokeOnlyErrorAndWarnAndInfo()
        {
            var logger = new MyLogger("unittest", LogLevel.Info);
                        
            logger.Information(null);
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(3, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsInfo_ShouldNotInvokeDebug()
        {
            var logger = new MyLogger("unittest", LogLevel.Info);

            logger.Debug(null);

            Assert.AreEqual(0, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsWarn_ShouldInvokeOnlyErrorAndWarnAndInfo()
        {
            var logger = new MyLogger("unittest", LogLevel.Warn);
                       
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(2, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsWarn_ShouldNotInvokeDebugOrInfo()
        {
            var logger = new MyLogger("unittest", LogLevel.Warn);

            logger.Debug(null);
            logger.Information(null);

            Assert.AreEqual(0, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsError_ShouldInvokeOnlyError()
        {
            var logger = new MyLogger("unittest", LogLevel.Error);

            logger.Error(null);

            Assert.AreEqual(1, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsError_ShouldNotInvokeDebugOrInfoOrWarn()
        {
            var logger = new MyLogger("unittest", LogLevel.Error);

            logger.Debug(null);
            logger.Information(null);
            logger.Warning(null);

            Assert.AreEqual(0, logger.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsOff_ShouldNotInvokeAnyLogMessage()
        {
            var logger = new MyLogger("unittest", LogLevel.Off);

            logger.Debug(null);
            logger.Information(null);
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(0, logger.LogMessageInvokeCount);
        }
    }
}


