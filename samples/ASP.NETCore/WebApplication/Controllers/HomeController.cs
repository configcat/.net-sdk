using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication.Models;

using ConfigCat.Client;

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
            ViewData["Title"] = this.configCatClient.GetValue("keySampleText", "Default Home Page Title");

            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = this.configCatClient.GetValue("keySampleText", "Your application description page.");            

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
