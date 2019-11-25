using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConfigCat.Client.Tests
{
    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void LoggerBase_LoglevelIsDebug_ShouldInvokeErrorOrWarnOrInfoOrDebug()
        {
            var l = new MyCounterLogger(LogLevel.Debug);
        
            var logger = new LoggerWrapper(l);

            logger.Debug(null);
            logger.Information(null);
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(4, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsInfo_ShouldInvokeOnlyErrorAndWarnAndInfo()
        {
            var l = new MyCounterLogger(LogLevel.Info);

            var logger = new LoggerWrapper(l);

            logger.Information(null);
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(3, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsInfo_ShouldNotInvokeDebug()
        {
            var l = new MyCounterLogger(LogLevel.Info);

            var logger = new LoggerWrapper(l);

            logger.Debug(null);

            Assert.AreEqual(0, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsWarn_ShouldInvokeOnlyErrorAndWarnAndInfo()
        {
            var l = new MyCounterLogger(LogLevel.Warning);

            var logger = new LoggerWrapper(l);

            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(2, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsWarn_ShouldNotInvokeDebugOrInfo()
        {
            var l = new MyCounterLogger(LogLevel.Warning);

            var logger = new LoggerWrapper(l);

            logger.Debug(null);
            logger.Information(null);

            Assert.AreEqual(0, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsError_ShouldInvokeOnlyError()
        {
            var l = new MyCounterLogger(LogLevel.Error);

            var logger = new LoggerWrapper(l);

            logger.Error(null);

            Assert.AreEqual(1, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsError_ShouldNotInvokeDebugOrInfoOrWarn()
        {
            var l = new MyCounterLogger(LogLevel.Error);

            var logger = new LoggerWrapper(l);

            logger.Debug(null);
            logger.Information(null);
            logger.Warning(null);

            Assert.AreEqual(0, l.LogMessageInvokeCount);
        }

        [TestMethod]
        public void LoggerBase_LoglevelIsOff_ShouldNotInvokeAnyLogMessage()
        {
            var l = new MyCounterLogger(LogLevel.Off);

            var logger = new LoggerWrapper(l);

            logger.Debug(null);
            logger.Information(null);
            logger.Warning(null);
            logger.Error(null);

            Assert.AreEqual(0, l.LogMessageInvokeCount);
        }
    }
}


