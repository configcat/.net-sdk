using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Represents a percentage option.
/// </summary>
public interface IPercentageOption : ISettingValueContainer
{
    /// <summary>
    /// A number between 0 and 100 that represents a randomly allocated fraction of the users.
    /// </summary>
    byte Percentage { get; }
}

internal sealed class PercentageOption : SettingValueContainer, IPercentageOption
{
    [JsonPropertyName("p")]
    public byte Percentage { get; set; }

    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendPercentageOption(this)
            .ToString();
    }
}
