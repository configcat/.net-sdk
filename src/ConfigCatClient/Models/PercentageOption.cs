using System.Text.Json.Serialization;
using ConfigCat.Client.Evaluation;
using ConfigCat.Client.Utils;

namespace ConfigCat.Client;

/// <summary>
/// Describes a percentage option.
/// </summary>
public sealed class PercentageOption : SettingValueContainer
{
    [JsonConstructor]
    internal PercentageOption() { }

    [JsonInclude, JsonPropertyName("p")]
    internal byte percentage;

    /// <summary>
    /// A number between 0 and 100 that represents a randomly allocated fraction of the users.
    /// </summary>
    [JsonIgnore]
    public byte Percentage => this.percentage;

    /// <inheritdoc />
    public override string ToString()
    {
        return new IndentedTextBuilder()
            .AppendPercentageOption(this)
            .ToString();
    }
}
