using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConfigCat.Client;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication.Controllers
{
    [Produces("application/json")]
    [Route("api/backdoor")]
    public class BackdoorController : Controller
    {
        private readonly IConfigCatClient configCatClient;

        public BackdoorController(IConfigCatClient configCatClient)
        {
            this.configCatClient = configCatClient;
        }

        // GET: api/backdoor/configcatchanged
        [HttpGet]
        [Route("configcatchanged")]
        public IActionResult ConfigCatChanged()
        {
            this.configCatClient.ForceRefresh();

            return this.Ok("configCatClient.ForceRefresh() invoked");
        }
    }
}