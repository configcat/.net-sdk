using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}
