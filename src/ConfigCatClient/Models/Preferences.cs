using ConfigCat.Client.Utils;

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
    public string? BaseUrl { get; set; }

    private RedirectMode redirectMode;

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "r")]
#else
    [JsonPropertyName("r")]
#endif
    public RedirectMode RedirectMode
    {
        get => this.redirectMode;
        set => ModelHelper.SetEnum(ref this.redirectMode, value);
    }

#if USE_NEWTONSOFT_JSON
    [JsonProperty(PropertyName = "s")]
#else
    [JsonPropertyName("s")]
#endif
    public string? Salt { get; set; }
}
