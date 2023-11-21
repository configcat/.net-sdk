using System.Collections.Generic;
using System.Diagnostics;
using ConfigCat.Client;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Models;

namespace WebApplication.Controllers;

public class HomeController : Controller
{
    private readonly IConfigCatClient configCatClient;

    public HomeController(IConfigCatClient configCatClient)
    {
        this.configCatClient = configCatClient;
    }

    public IActionResult Index()
    {
        ViewData["Message1"] = this.configCatClient.GetValue("isAwesomeFeatureEnabled", false);

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

        ViewData["Message2"] = this.configCatClient.GetValue("isPOCFeatureEnabled", false, userObject);

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
