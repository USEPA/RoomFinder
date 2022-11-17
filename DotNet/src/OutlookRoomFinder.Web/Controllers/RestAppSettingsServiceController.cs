using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Models.MSAL;
using Serilog;
using System;
using System.Linq;

namespace OutlookRoomFinder.Web.Controllers
{
    [ResponseCache(Duration = 3600)]
    [Authorize]
    [Produces("application/json")]
    public class RestAppSettingsServiceController : Controller
    {
        private ILogger Logger { get; }
        private IAppSettings AppSettings { get; }

        public RestAppSettingsServiceController(ILogger logger, IAppSettings config)
        {
            Logger = logger;
            AppSettings = config;
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("api/clientsettings/{environmentName}")]
        public IActionResult Get([FromRoute] string environmentName)
        {
            var isIE = InterogateUserAgent();
            var webApiRootUrl = $"{this.Request.Scheme}://{this.Request.Host}/";

            var config = new MsalConfiguration
            {
                Env = new ConfigEnvironment
                {
                    Name = environmentName,
                    Version = AppSettings.DeployedVersion,
                    ReferrerIsIE = isIE
                },
                Auth = new ConfigAuthOptions
                {
                    ClientId = AppSettings.AzureAd.ClientId,
                    Authority = $"{AppSettings.AzureAd.Instance}{AppSettings.AzureAd.Domain}",
                    BaseWebApiUrl = webApiRootUrl,
                    Audience = $"{AppSettings.AzureAd.Audience}/access_as_user",
                    AzureDomain = AppSettings.AzureAd.Domain,
                }
            };

            return Ok(config);
        }

        private bool InterogateUserAgent()
        {
            if (this.HttpContext == null)
            {
                throw new ArgumentException(nameof(this.HttpContext));
            }

            bool isIE = false;
            if (this.HttpContext.Request.Headers.TryGetValue("User-Agent", out Microsoft.Extensions.Primitives.StringValues headers))
            {
                isIE = headers.ToArray().Any(header => header.IndexOf("MSIE ") > -1 || header.IndexOf("Trident/") > -1);
            }
            return isIE;
        }

        [HttpGet]
        [Route("api/clientsettings/bearer")]
        public IActionResult GetBearer()
        {
            var user = this.Request.HttpContext?.User;
            Logger.Information($"User {user?.Identity?.IsAuthenticated}");
            return Ok();
        }
    }
}