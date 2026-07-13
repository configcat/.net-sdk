using System.Diagnostics;
using System.Threading.Tasks;
using ConfigCat.Client;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Models;

namespace WebApplication.Controllers;

public class HomeController : Controller
{
    private readonly IConfigCatClient configCatClient;
    private readonly IConfigCatClientSnapshot configCatClientSnapshot;

    public HomeController(IConfigCatClient configCatClient, IConfigCatClientSnapshot configCatClientSnapshot)
    {
        // You can obtain the default ConfigCat client by simply injecting the singleton IConfigCatClient service.

        this.configCatClient = configCatClient;

        // If you configure the SDK to use Auto Polling mode and to wait for the client to reach the ready state
        // at application startup (see appsettings.json and Program.cs), you can as well inject the scoped
        // IConfigCatClientSnapshot service in your request handlers and use synchronous feature flag evaluation.

        this.configCatClientSnapshot = configCatClientSnapshot;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Message1"] = await this.configCatClient.GetValueAsync("isAwesomeFeatureEnabled", false);

        var userObject = new User("<Some UserID>")
        {
            Email = "configcat@example.com",
            Country = "Canada",
            Custom =
            {
                {"SubscriptionType", "Pro"},
                {"Version", "1.0.0"}
            }
        };

        ViewData["Message2"] = this.configCatClientSnapshot.GetValue("isPOCFeatureEnabled", false, userObject);

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
