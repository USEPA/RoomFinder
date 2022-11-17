using Microsoft.AspNetCore.Mvc;
using OutlookRoomFinder.Core.Models;
using Serilog;
using System.Diagnostics.CodeAnalysis;

namespace OutlookRoomFinder.Web.Controllers
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing until sufficient code-coverage is in place.")]
    [Route("api/meetingService")]
    public class ODataMeetingService : Controller
    {
        protected ILogger Logger { get; }

        public ODataMeetingService(ILogger logger)
        {
            this.Logger = logger;
        }

        [HttpPost]
        public IActionResult TeleconferencePost(TeleconferenceModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok(model);
        }
    }
}
