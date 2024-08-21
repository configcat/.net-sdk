using ConfigCat.Client;

namespace MauiSample;

public partial class MainPage : ContentPage
{
    private readonly IConfigCatClient configCatClient;

    public MainPage()
    {
        InitializeComponent();
        this.configCatClient = Application.Current!.Handler.MauiContext!.Services.GetRequiredService<IConfigCatClient>();
    }

    private async void OnEvaluateBtnClicked(object sender, EventArgs e)
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
        var value = await this.configCatClient.GetValueAsync("isPOCFeatureEnabled", false, user);

        this.EvaluationResultLabel.Text = $"Value returned from ConfigCat: {value}";
        this.EvaluationResultLabel.IsVisible = true;
    }
}

