using System.Threading.Tasks;
using ConfigCat.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication.Controllers;

[Produces("application/json")]
[Route("api/backdoor")]
public class BackdoorController : Controller
{
    private readonly IConfigCatClient configCatClient;

    public BackdoorController([FromKeyedServices("secondary")] IConfigCatClient configCatClient)
    {
        this.configCatClient = configCatClient;
    }

    // GET: api/backdoor/configcatchanged
    // This endpoint can be called by Configcat Webhooks https://configcat.com/docs/advanced/notifications-webhooks
    [HttpGet]
    [Route("configcatchanged")]
    public async Task<IActionResult> ConfigCatChanged()
    {
        await this.configCatClient.ForceRefreshAsync();

        return Ok("configCatClient.ForceRefresh() invoked");
    }
}
