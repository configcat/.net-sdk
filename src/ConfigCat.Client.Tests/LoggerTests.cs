using System;
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

        var logger = l.AsWrapper();

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

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(3, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsInfo_ShouldNotInvokeDebug()
    {
        var l = new CounterLogger(LogLevel.Info);

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Debug, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsWarn_ShouldInvokeOnlyErrorAndWarnAndInfo()
    {
        var l = new CounterLogger(LogLevel.Warning);

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(2, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsWarn_ShouldNotInvokeDebugOrInfo()
    {
        var l = new CounterLogger(LogLevel.Warning);

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsError_ShouldInvokeOnlyError()
    {
        var l = new CounterLogger(LogLevel.Error);

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(1, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsError_ShouldNotInvokeDebugOrInfoOrWarn()
    {
        var l = new CounterLogger(LogLevel.Error);

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LoggerBase_LoglevelIsOff_ShouldNotInvokeAnyLogMessage()
    {
        var l = new CounterLogger(LogLevel.Off);

        var logger = l.AsWrapper();

        logger.Log(LogLevel.Debug, default, "");
        logger.Log(LogLevel.Info, default, "");
        logger.Log(LogLevel.Warning, default, "");
        logger.Log(LogLevel.Error, default, "");

        Assert.AreEqual(0, l.LogMessageInvokeCount);
    }

    [TestMethod]
    public void LogFilter_ExcludesLogEvents()
    {
        var logEvents = new List<LogEvent>();
        var loggerMock = new Mock<IConfigCatLogger>();
        loggerMock.SetupGet(m => m.LogLevel).Returns(LogLevel.Info);

        LogFilterCallback logFilter = (LogLevel level, LogEventId eventId, ref FormattableLogMessage message, Exception? exception)
            => eventId.Id is not (1001 or 3001 or 5001);

        var logger = loggerMock.Object.AsWrapper(logFilter);

        logger.Log(LogLevel.Debug, 0, "debug");
        logger.Log(LogLevel.Info, 5000, "info");
        logger.Log(LogLevel.Warning, 3000, "warn");
        var ex1 = new Exception();
        logger.Log(LogLevel.Error, 1000, ex1, "error");
        logger.Log(LogLevel.Info, 5001, "info");
        logger.Log(LogLevel.Warning, 3001, "warn");
        var ex2 = new Exception();
        logger.Log(LogLevel.Error, 1001, ex2, "error");

        loggerMock.Verify(m => m.Log(LogLevel.Debug, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Never);

        loggerMock.Verify(m => m.Log(LogLevel.Info, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
        loggerMock.Verify(m => m.Log(LogLevel.Info, 5000, ref It.Ref<FormattableLogMessage>.IsAny, null), Times.Once);

        loggerMock.Verify(m => m.Log(LogLevel.Warning, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
        loggerMock.Verify(m => m.Log(LogLevel.Warning, 3000, ref It.Ref<FormattableLogMessage>.IsAny, null), Times.Once);

        loggerMock.Verify(m => m.Log(LogLevel.Error, It.IsAny<LogEventId>(), ref It.Ref<FormattableLogMessage>.IsAny, It.IsAny<Exception>()), Times.Once);
        loggerMock.Verify(m => m.Log(LogLevel.Error, 1000, ref It.Ref<FormattableLogMessage>.IsAny, ex1), Times.Once);
    }

    [DataRow("", "")]
    [DataRow("abc123", "abc123")]
    [DataRow("/abc123", "/abc123")]
    [DataRow("abc/123", "*bc/123")]
    [DataRow("abc123/", "*bc123/")]
    [DataRow("configcat-sdk-1/TEST_KEY-0123456789012/1234567890123456789012", "***************/**********************/****************789012")]
    [DataTestMethod]
    public void MaskSdkKey_Works(string sdkKey, string expectedMaskedSdkKey)
    {
        Assert.AreEqual(expectedMaskedSdkKey, LoggerExtensions.MaskSdkKey(sdkKey));
    }
}
