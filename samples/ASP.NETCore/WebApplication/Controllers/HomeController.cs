using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Models;

using ConfigCat.Client;
using System.Collections.Generic;

namespace WebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfigCatClient configCatClient;

        public HomeController(IConfigCatClient configCatClient)
        {
            this.configCatClient = configCatClient;
        }

        public IActionResult Index()
        {
            ViewData["Message1"] = this.configCatClient.GetValue("isAwesomeFeatureEnabled", "Acquiring value failed, returning default value.");

            var userObject = new User("<Some UserID>")
            {
                Email = "configcat@example.com",
                Country = "Canada",
                Custom = new Dictionary<string, string> {
                    {"SubscriptionType", "Pro"},
                    {"Version", "1.0.0"}
                }
            };

            ViewData["Message2"] = this.configCatClient.GetValue("isPOCFeatureEnabled", "Acquiring value failed, returning default value.", userObject);

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
