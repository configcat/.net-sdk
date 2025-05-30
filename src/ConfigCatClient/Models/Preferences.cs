using System.Text.Json.Serialization;

namespace ConfigCat.Client;

internal sealed class Preferences
{
    [JsonPropertyName("u")]
    public string? BaseUrl { get; set; }

    [JsonPropertyName("r")]
    public RedirectMode RedirectMode { get; set; }

    [JsonPropertyName("s")]
    public string? Salt { get; set; }
}
