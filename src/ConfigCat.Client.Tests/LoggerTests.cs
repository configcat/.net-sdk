using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace ConfigCat.Client.Tests;

[TestClass]
public class LoggerTests
{
    [TestMethod]
    public void LoggerBase_LoglevelIsDebug_ShouldInvokeErrorOrWarnOrInfoOrDebug()
    {
        var l = new CounterLogger(LogLevel.Debug);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(4, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsInfo_ShouldInvokeOnlyErrorAndWarnAndInfo()
    {
        var l = new CounterLogger(LogLevel.Info);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(3, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsInfo_ShouldNotInvokeDebug()
    {
        var l = new CounterLogger(LogLevel.Info);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Debug, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsWarn_ShouldInvokeOnlyErrorAndWarnAndInfo()
    {
        var l = new CounterLogger(LogLevel.Warning);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(2, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsWarn_ShouldNotInvokeDebugOrInfo()
    {
        var l = new CounterLogger(LogLevel.Warning);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsError_ShouldInvokeOnlyError()
    {
        var l = new CounterLogger(LogLevel.Error);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(1, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsError_ShouldNotInvokeDebugOrInfoOrWarn()
    {
        var l = new CounterLogger(LogLevel.Error);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsOff_ShouldNotInvokeAnyLogMessage()
    {
        var l = new CounterLogger(LogLevel.Off);

        var logger = new LoggerWrapper(l);

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }


    [TestMethod]
    public void OldLoggerWorks()
    {
        var errorMessages = new List<string>();
        var warningMessages = new List<string>();
        var infoMessages = new List<string>();
        var debugMessages = new List<string>();

#pragma warning disable CS0618 // Type or member is obsolete
        var loggerMock = new Mock<ILogger>();
#pragma warning restore CS0618 // Type or member is obsolete
        loggerMock.SetupGet(m => m.LogLevel).Returns(LogLevel.Debug);
        loggerMock.Setup(m => m.Error(Capture.In(errorMessages)));
        loggerMock.Setup(m => m.Warning(Capture.In(warningMessages)));
        loggerMock.Setup(m => m.Information(Capture.In(infoMessages)));
        loggerMock.Setup(m => m.Debug(Capture.In(debugMessages)));

        var loggerWrapper = new LoggerWrapper(loggerMock.Object);

        loggerWrapper.LogInterpolated(LogLevel.Debug, default, $"{nameof(LogLevel.Debug)} message", "LOG_LEVEL");
        loggerWrapper.LogInterpolated(LogLevel.Info, default, $"{nameof(LogLevel.Info)} message", "LOG_LEVEL");
        loggerWrapper.LogInterpolated(LogLevel.Warning, default, $"{nameof(LogLevel.Warning)} message", "LOG_LEVEL");
        loggerWrapper.LogInterpolated(LogLevel.Error, default, $"{nameof(LogLevel.Error)} message", "LOG_LEVEL");

        CollectionAssert.AreEqual(new[] { $"{nameof(LogLevel.Debug)} message" }, debugMessages);
        CollectionAssert.AreEqual(new[] { $"{nameof(LogLevel.Info)} message" }, infoMessages);
        CollectionAssert.AreEqual(new[] { $"{nameof(LogLevel.Warning)} message" }, warningMessages);
        CollectionAssert.AreEqual(new[] { $"{nameof(LogLevel.Error)} message" }, errorMessages);
    }
}
