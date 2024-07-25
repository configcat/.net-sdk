using System.Diagnostics.CodeAnalysis;
using ConfigCat.Client;
using Microsoft.AspNetCore.Components;

namespace BlazorWasm.Pages;

public partial class Home : ComponentBase
{
    [Inject, NotNull]
    private IConfigCatClient? ConfigCatClient { get; set; }

    private bool? isAwesomeEnabled;
    private bool? isPOCEnabled;
    private string userEmail = "configcat@example.com";

    private async Task CheckAwesome()
    {
        this.isAwesomeEnabled = await ConfigCatClient.GetValueAsync("isAwesomeFeatureEnabled", false);
    }

    private async Task CheckProofOfConcept()
    {
        var userObject = new User("#SOME-USER-ID#") { Email = this.userEmail };
        // Read more about the User Object: https://configcat.com/docs/advanced/user-object
        this.isPOCEnabled = await ConfigCatClient.GetValueAsync("isPOCFeatureEnabled", false, userObject);
    }
}
