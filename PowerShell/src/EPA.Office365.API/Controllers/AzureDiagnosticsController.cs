using EPA.Office365.API.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Linq;

namespace EPA.Office365.API.Controllers
{
    //[Authorize]
    [Produces("application/json")]
    [ResponseCache(NoStore = true)]
    public class AzureDiagnosticsController : ControllerBase
    {
        private readonly IActionDescriptorCollectionProvider actionDescriptorCollectionProvider;
        public AzureDiagnosticsController(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            this.actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        }

        [HttpGet("v2.0/routes", Name = "ApiEnvironmentGetAllRoutes")]
        public IActionResult GetAllRoutes()
        {
            var routes = actionDescriptorCollectionProvider.ActionDescriptors.Items.Where(
                ad => ad.AttributeRouteInfo != null).Select(ad => new Tuple<int, string>(ad.AttributeRouteInfo.Order, ad.AttributeRouteInfo.Template)).ToList();
            return Ok(routes);
        }

        [AllowAnonymous()]
        [DisableCors]
        [AzureWebHook]
        public IActionResult AzureAlerts(string id, string @event, JObject data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Log.Information($"AzureAlerts {id} received {@event} at {DateTime.UtcNow} with data {data}");
            return Ok();
        }
    }
}
