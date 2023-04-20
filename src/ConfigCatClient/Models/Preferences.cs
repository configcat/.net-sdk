#if USE_NEWTONSOFT_JSON
using Newtonsoft.Json;
#else
using System.Text.Json.Serialization;
#endif

namespace ConfigCat.Client;

internal sealed class Preferences
{
#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "u")]
#else
    [JsonPropertyName("u")]
#endif
    public string? Url { get; set; }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "r")]
#else
    [JsonPropertyName("r")]
#endif
    public RedirectMode RedirectMode { get; set; }
}
