using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Models;
using Serilog;

namespace OutlookRoomFinder.Web.Controllers
{
    [Produces("application/json")]
    public class ODataLoggingService : Controller
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger LogHelper;

        public ODataLoggingService(ILogger logger, IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.LogHelper = logger;
        }

        [HttpPost()]
        [Route("/v1.0/logs")]
        public IActionResult Post([FromBody] LogEntry logEntry)
        {
            if (logEntry == null)
            {
                return BadRequest($"Invalid telemetry signature for {nameof(logEntry)}");
            }

            if (logEntry.LogType == LogEntryType.Telemetry)
            {
                LogHelper.LogTelemetry(httpContextAccessor.HttpContext, logEntry.LogLevel, logEntry.Operation, logEntry.OperationProperties);
            }
            else
            {
                LogHelper.Logging(logEntry.LogLevel, logEntry.Operation, logEntry.OperationProperties);
            }

            return Ok(logEntry);
        }
    }
}
