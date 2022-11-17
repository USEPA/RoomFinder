using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Exceptions;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.Outlook;
using OutlookRoomFinder.Core.Services;
using OutlookRoomFinder.Web.Models;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Controllers
{
    [Authorize]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing until sufficient code-coverage is in place.")]
    [Route("api/reporting")]
    [Produces("application/json")]
    [ResponseCache(NoStore = true)]
    public class RestReportingController : Controller
    {
        #region Private Variables

        protected ILogger Logger { get; }
        private readonly IHttpContextAccessor httpContextAccessor;
        private IExchangeContext ExchangeContext { get; set; }
        private IExchangeService ExchangeService { get; set; }

        #endregion

        public RestReportingController(ILogger logger, IHttpContextAccessor httpContextAccessor, IAppSettings config, IExchangeContext exchangeContext, IExchangeService exchangeService)
        {
            this.Logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            ExchangeContext = exchangeContext;
            ExchangeService = exchangeService;

            Logger.Logging(LogEventLevel.Verbose, $"Rooms controller called with JSON={config.Exchange?.JsonFilename} at {DateTime.Now.ToLongDateString()}");
        }


        [HttpGet()]
        [Route("rooms")]
        public IActionResult RoomListing()
        {
            var results = new List<dynamic>();

            try
            {
                var ldapRooms = ExchangeContext.GetRooms();
                results.AddRange(ldapRooms.Select(r => new
                {
                    r.EmailAddress,
                    r.DisplayName
                }).OrderBy(r => r.DisplayName));


            }
            catch (Exception ex)
            {
                ModelState.AddModelError("LDAP", $"Failed to retreive roomevents {ex.Message}");
                return BadRequest(ModelState);
            }

            return Ok(results);
        }

        [HttpPost]
        [Route("roomCustom")]
        public async Task<IActionResult> RoomCustom(ReportCustomModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get today's calendar events total and all rooms.
            var timeWindow = new MeetingTimeWindow(model.StartDate, model.EndDate);
            var events = new List<CalendarEventViewModel>();
            try
            {
                // Query AD for the Rooms
                var ldapRooms = ExchangeContext.GetRooms();

                // simplify to email
                var roomEmails = ldapRooms.Select(s => s.EmailAddress).ToArray();

                // retrieve all appointments for all rooms in this time period
                var retreivedEvents = await ExchangeService.GetAppointmentsAsync(timeWindow, roomEmails);
                events.AddRange(retreivedEvents);

                Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Verbose, $"Reports controller hit with {events.Count} items");
            }
            catch (Exception ex)
            {
                Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Error, $"Repors-GetCalendarEvents {timeWindow.StartTime} to {timeWindow.EndTime} threw exception {ex.Message}");
                return BadRequest($"There was an error in processing your request for events.");
            }

            return Ok(events);
        }

        /// <summary>
        /// Return Rooms with Calendar/Events in the <paramref name="model"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("roomAnalytics")]
        public async Task<IActionResult> RoomAnalytics([FromBody] ReportCustomModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var timeWindow = new MeetingTimeWindow(model.StartDate.ToLocalTime(), model.EndDate.ToLocalTime());
            var roomAnalytics = new List<AnalyticsRoom>();

            Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Verbose, $"RoomAnalytics with start {timeWindow.StartTime} and end {timeWindow.EndTime}");
            try
            {
                using var measurePerformance = new MeasurePerformance();

                // Query AD for the Rooms
                var ldapRooms = ExchangeContext.GetRooms();

                // reduce to email array
                var roomEmails = ldapRooms.Select(s => s.EmailAddress).ToArray();

                // Send Rooms to EWS and evaluate events
                var appointments = await ExchangeService.GetAppointmentsAsync(timeWindow, roomEmails);

                foreach (var room in ldapRooms)
                {
                    var roomAppointments = appointments.Where(a => !string.IsNullOrEmpty(a.LocationEmail)
                        && a.LocationEmail.Contains(room.EmailAddress, StringComparison.OrdinalIgnoreCase));
                    if (roomAppointments.Any())
                    {
                        roomAnalytics.Add(new AnalyticsRoom()
                        {
                            SmtpAddress = room.EmailAddress,
                            RoomName = room.DisplayName,
                            NumberOfMeetings = roomAppointments.Count(),
                            Meetings = (roomAppointments ?? Enumerable.Empty<CalendarEventViewModel>()).ToList()
                        });
                    }
                }

                Logger.Logging(LogEventLevel.Debug, measurePerformance.StopTimer($"RoomAnalytics with start {timeWindow.StartTime} and end {timeWindow.EndTime}"));
            }
            catch (Exception ex)
            {
                Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Error, $"RoomAnalytics failed {timeWindow.ToString()} with message {ex.Message}");
                return BadRequest($"Failed to retreive roomanalytics for {timeWindow.ToString()}");
            }

            return Ok(roomAnalytics.OrderBy(s => s.RoomName));
        }

        /// <summary>
        /// Create report for today's room agenda.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("roomEvents")]
        public async Task<IActionResult> RoomEvents([FromBody] ReportRoomModel model)
        {
            if (!ModelState.IsValid || model == null)
            {
                return BadRequest(ModelState);
            }

            // Get today's calendar events total for the specified rooms.
            var timeWindow = new MeetingTimeWindow(model.StartDate.ToLocalTime(), model.EndDate.ToLocalTime());
            var events = new List<CalendarEventViewModel>();

            try
            {
                using var measurePerformance = new MeasurePerformance();

                // retrieve all appointments for all rooms in this time period
                var retreivedEvents = await ExchangeService.GetAppointmentsAsync(timeWindow, new string[] { model.EmailAddress }).ConfigureAwait(true);
                if (retreivedEvents.Any())
                {
                    // remove all locations not in AWBERC or Erlanger
                    foreach (var item in retreivedEvents)
                    {
                        var itemLocation = item.Location;
                        var itemLocationBuilder = new StringBuilder(itemLocation);
                        if (!string.IsNullOrEmpty(itemLocation))
                        {
                            var differentLocations = itemLocation.Split(new string[] { "; " }, StringSplitOptions.None);
                            for (int counter = 0; counter < differentLocations.Length; counter++)
                            {
                                if (!differentLocations[counter].Contains("Cin", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    differentLocations[counter] = "";
                                }
                                itemLocationBuilder.Append(differentLocations[counter]);
                            }
                        }
                        item.Location = itemLocationBuilder.ToString();
                    }

                    events.AddRange(retreivedEvents);
                }

                Logger.Logging(LogEventLevel.Debug, measurePerformance.StopTimer($"MP for batch query {model.EmailAddress} attendee"));
            }
            catch (Exception ex)
            {
                Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Error, $"RoomEvents {model.EmailAddress} with stack trace {ex.StackTrace}");
                return BadRequest($"There was an error in processing your request for {model.EmailAddress}.");
            }

            return Ok(events);
        }

        /// <summary>
        /// Usage Report, breads down busy/active status by the room and appointments
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("usageReport")]
        async public Task<IActionResult> UsageReport([FromBody] ReportRoomModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var timeWindow = new MeetingTimeWindow(model.StartDate, model.EndDate);

            var attendees = new List<ResourceItemMailbox>();
            if (!string.IsNullOrEmpty(model.EmailAddress))
            {
                attendees.Add(new ResourceItemMailbox { EmailAddress = model.EmailAddress });
            }
            if (model.Resources?.Any() == true)
            {
                attendees.AddRange(model.Resources.Select(smtpAddress => new ResourceItemMailbox { EmailAddress = smtpAddress }));
            }

            // pull calendar data for the specified mailbox
            // aggregate the total events that are Busy during the specified timewindow
            var mailboxSchedule = await ExchangeService.GetBatchCalendarData(attendees, timeWindow);
            return Ok(mailboxSchedule);
        }

        /// <summary>
        /// Return analytics for the equipment associated with various Room Calendar Appointments
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("equipmentAnalytics")]
        public async Task<IActionResult> EquipmentAnalytics([FromBody] ReportCustomModel model)
        {
            var roomAnalytics = new List<AnalyticsEquipment>();

            try
            {
                // Get today's calendar events total and all rooms.
                var timeWindow = new MeetingTimeWindow(model.StartDate.ToLocalTime(), model.EndDate.ToLocalTime());

                // Represents components that could be inside a room
                var ldapEquipments = ExchangeContext.GetEquipmentListing();

                // Query AD for the Rooms
                var ldapEquipmentItems = ldapEquipments.SelectMany(s => s.Resources.Select(sei => sei.EmailAddress)).Distinct();

                // Filter to primary SMTP address
                var equipmentEmails = ldapEquipmentItems.ToArray();

                // Return appointments for specified window
                var appointments = await ExchangeService.GetAppointmentsAsync(timeWindow, equipmentEmails);


                var equipments = new List<string>();
                foreach (var appointment in appointments.Where(appt => !string.IsNullOrEmpty(appt?.Location) && !string.IsNullOrEmpty(appt?.MailboxId)))
                {
                    equipments.AddRange(appointment.MailboxId.Split(';').Where(s => !equipments.Contains(s.Trim()) && ldapEquipments.Any(eq => eq.Resources.Any(eqi => eqi.EmailAddress.Contains(s.Trim())))).Select(s => s.Trim()).ToArray());
                }

                foreach (string equipment in equipments)
                {
                    roomAnalytics.Add(new AnalyticsEquipment()
                    {
                        EquipmentName = equipment,
                        NumberOfMeetings = appointments.Count(a => a.Location != null && a.MailboxId.Contains(equipment))
                    });
                }

            }
            catch (TimeWindowException tex)
            {
                Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Error, tex.ToString());
                return BadRequest(tex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogTelemetry(httpContextAccessor.HttpContext, LogEventLevel.Error, ex.ToString());
                return BadRequest($"Failed to retreive equipment lists {ex.Message}");
            }

            return Ok(roomAnalytics.OrderBy(s => s.EquipmentName));
        }

    }
}
