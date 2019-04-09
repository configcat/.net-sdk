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
    }
}


