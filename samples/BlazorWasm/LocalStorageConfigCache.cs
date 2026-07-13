using System.Text;
using ConfigCat.Client;
using ConfigCat.Extensions.Hosting.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorWasm;

internal sealed class LocalStorageConfigCache(IJSRuntime js) : IConfigCatCache
{
    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var data = await js.InvokeAsync<string>("localStorage.getItem", key);
        return string.IsNullOrEmpty(data) ? default : Encoding.UTF8.GetString(Convert.FromBase64String(data));
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await js.InvokeVoidAsync("localStorage.setItem", key, Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));
    }

    public sealed class ConfigureClientOptions()
        : ConfigureNamedOptions<ExtendedConfigCatClientOptions>(name: null, action: null)
    {
        private IJSRuntime? js;

        public async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            try
            {
                var js = serviceProvider.GetRequiredService<IJSRuntime>();

                // This function is defined in wwwroot/index.html.
                if (await js.InvokeAsync<bool>("isLocalStorageAvailable"))
                {
                    this.js = js;
                }
            }
            catch { /* intentional no-op */ }
        }

        public override void Configure(string? name, ExtendedConfigCatClientOptions options)
        {
            if (this.js is not null)
            {
                options.ConfigCache = new LocalStorageConfigCache(this.js);
            }
        }
    }
}
