using ConfigCat.Client;

namespace MauiSample;

public partial class MainPage : ContentPage
{
    private readonly IConfigCatClient configCatClient;

    public MainPage(IConfigCatClient configCatClient)
    {
        this.configCatClient = configCatClient;

        InitializeComponent();
    }

    private void OnEvaluateBtnClicked(object sender, EventArgs e)
    {
        // Creating a user object to identify the user (optional)
        var user = new User("<SOME USERID>")
        {
            Country = "US",
            Email = "configcat@example.com",
            Custom =
            {
                { "SubscriptionType", "Pro"},
                { "Role", "Admin"},
                { "version", "1.0.0" }
            }
        };

        // Read more about the User Object: https://configcat.com/docs/advanced/user-object

        // If you configure the SDK to use Auto Polling mode and to wait for the client to reach the ready state
        // at application startup (see MauiProgram.cs), you can use synchronous feature flag evaluation, thus,
        // avoid async void event handlers.

        var value = this.configCatClient.Snapshot().GetValue("isPOCFeatureEnabled", false, user);

        this.EvaluationResultLabel.Text = $"Value returned from ConfigCat: {value}";
        this.EvaluationResultLabel.IsVisible = true;
    }
}

