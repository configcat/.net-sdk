using Microsoft.Extensions.Logging;

namespace WebApplication.Adapters;

public class ConfigCatToMSLoggerAdapter : ConfigCat.Client.ILogger
{
    private readonly ILogger logger;

    public ConfigCatToMSLoggerAdapter(ILogger<ConfigCat.Client.ConfigCatClient> logger)
    {
        this.logger = logger;
    }

    // Allow all log levels here and let MS logger do log level filtering (see appsettings.json)
    public ConfigCat.Client.LogLevel LogLevel { get; set; } = ConfigCat.Client.LogLevel.Debug;

    public void Debug(string message) => this.logger.LogDebug(message);

    public void Information(string message) => this.logger.LogInformation(message);

    public void Warning(string message) => this.logger.LogWarning(message);

    public void Error(string message) => this.logger.LogError(message);
}
