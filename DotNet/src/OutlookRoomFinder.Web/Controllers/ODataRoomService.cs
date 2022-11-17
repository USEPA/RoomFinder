using Microsoft.AspNetCore.Mvc;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Exceptions;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.Filter;
using OutlookRoomFinder.Core.Models.Outlook;
using OutlookRoomFinder.Core.Services;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Controllers
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing until sufficient code-coverage is in place.")]
    [Route("api/roomDataService")]
    [Produces("application/json")]
    public class ODataRoomService : Controller
    {
        protected ILogger Logger { get; }
        private IAppSettings AppSettings { get; set; }
        private IExchangeContext ExchangeContext { get; set; }
        private IExchangeService ExchangeContextService { get; set; }

        public ODataRoomService(ILogger logger, IAppSettings config, IExchangeContext exchangeContext, IExchangeService exchangeService)
        {
            this.Logger = logger;
            AppSettings = config;
            ExchangeContext = exchangeContext;
            ExchangeContextService = exchangeService;
        }

        [HttpPost()]
        [Route("v1.0/checkRecurrence")]
        public async Task<IActionResult> GetRecurrenceCheck([FromBody] FindResourceRecurrenceFilter filter)
        {
            if (filter.Recurrence == null)
            {
                ModelState.AddModelError("RecurrencePattern", "Recurrence Pattern can not be empty.");
                return BadRequest(ModelState);
            }

            var result = new List<AttendeeAvailabilityInfo>();


            var helper = new RecurrenceHelper(Logger);
            var recurrence = helper.Evaluate(filter, true);


            var confRooms = ExchangeContext.GetRooms();
            var rooms = confRooms.Where(room => filter.Attendees.Any(attendee => room.EmailAddress == attendee.SmtpAddress));

            using var measurePerformance = new MeasurePerformance();

            var advancedTicks = (new TimeSpan(0, 1, 0)).Ticks;
            var appointmentData = helper.GetAppointments(filter.SetIndex, filter.SetSize, recurrence);
            var appointments = appointmentData.Meetings;
            var isLastSet = appointmentData.IsLastSet;
            foreach (var tw in appointments)
            {
                tw.AdvanceDuration(advancedTicks); // Adjustment to prevent room unavailability.
                var availabilityItems = await ExchangeContextService.GetAvailabilityAsync(rooms, tw, true);

                result.AddRange(availabilityItems.Select(s => new AttendeeAvailabilityInfo
                {
                    EmailAddress = s.EmailAddress,
                    StartTime = tw.StartTime,
                    EndTime = tw.EndTime,
                    Status = s.Status
                }));
            }

            Logger.Logging(LogEventLevel.Debug, measurePerformance.StopTimer($"MP for batch query {rooms.Count()} attendees"));

            var model = new GetAvailabilityResponseFilter()
            {
                IsLastSet = isLastSet,
                Data = result.ToArray(),
                RecurrencePattern = recurrence
            };
            return Ok(model);
        }

        [HttpPost()]
        [Route("v1.0/graphRecurrence")]
        public IActionResult CheckRecurrence([FromBody] FindResourceRecurrenceFilter filter)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("state", "graphRecurrence should not be null");
                return BadRequest(ModelState);
            }

            var helper = new RecurrenceHelper(Logger);
            var recurrence = helper.Evaluate(filter, true);

            return Ok(recurrence);
        }

        [HttpPost()]
        [Route("v1.0/find")]
        public async Task<IActionResult> Find([FromBody] FindResourceFilter filter)
        {
            string serializedInput = filter.ToString();
            Logger.Logging(LogEventLevel.Information, "RoomDataService.Find:  " + serializedInput);
            using var measurePerformance = new MeasurePerformance();

            try
            {
                var confRooms = ExchangeContext.GetRooms();
                if (confRooms == null || !confRooms.Any())
                {
                    Logger.Logging(LogEventLevel.Error, "CacheContext.Rooms is empty");
                    return NoContent();
                }

                var filterDates = new MeetingTimeWindow(filter.Start, filter.End);
                filterDates.AdvanceDuration((new TimeSpan(0, 1, 0)).Ticks); // Adjustment to prevent room unavailability.

                var results = new List<IResourceItem>();
                var rooms = (from r in confRooms
                             where (string.IsNullOrEmpty(filter.State) || (!string.IsNullOrEmpty(r.Location.State) && string.Compare(r.Location.State, filter.State, true) == 0))
                                && (string.IsNullOrEmpty(filter.City) || (!string.IsNullOrEmpty(r.Location.City) && string.Compare(r.Location.City, filter.City, true) == 0))
                                && (string.IsNullOrEmpty(filter.Office) || (!string.IsNullOrEmpty(r.Location.Office) && string.Compare(r.Location.Office, filter.Office, true) == 0))
                                && (string.IsNullOrEmpty(filter.Floor) || (!string.IsNullOrEmpty(r.Location.Floor) && string.Compare(r.Location.Floor, filter.Floor, true) == 0))
                                && (string.IsNullOrEmpty(filter.ListPath) || (r.RoomList != null && string.Compare(r.RoomList.DisplayName, filter.ListPath, true) == 0))
                                && (r.RestrictionType != RestrictionType.Restricted || filter.IncludeRestricted)
                                && r.Capacity >= filter.Capacity
                                && r.Equipment.Intersect(filter.RequiredEquipment).Count() == (filter.RequiredEquipment?.Count ?? 0)
                             select r);
                if (rooms?.Any() == true)
                {
                    if (rooms.Any(room => room.EquipmentDependencies?.Any() == false))
                    {
                        // Room Filter has NO dependencies - call and pull availiablity
                        var availability = await ExchangeContextService.GetAvailabilityAsync(rooms, filterDates, filter.IncludeUnavailable);
                        results.AddRange(availability);
                    }
                    else
                    {
                        // Room Filter has Equipment Dependencies - call and pull all availability

                        var EquipmentDependencies = rooms.SelectMany(i => i.EquipmentDependencies).ToList();
                        var equipment = ExchangeContext.GetEquipmentListing().SelectMany(s => s.Resources)
                            .Where(eitem => EquipmentDependencies.Any(ed => ed.EmailAddress == eitem.EmailAddress));

                        var resources = rooms.Cast<IResourceItem>().Union(equipment.Cast<IResourceItem>());
                        var availability = await ExchangeContextService.GetAvailabilityAsync(resources, filterDates, filter.IncludeUnavailable);
                        var availabilityResult = availability.Where(fn => fn.EntryType == MeetingAttendeeType.Room).OfType<ResourceItemMailbox>();
                        if (availabilityResult.Any(eqp => eqp.EquipmentDependencies?.Any() == true))
                        {
                            var equipmentAvailability = availability.Where(fn => fn.EntryType == MeetingAttendeeType.Resource);
                            if (equipmentAvailability.Any() && !filter.IncludeUnavailable)
                            {
                                foreach (var localRoom in availabilityResult.Where(availableRoom => availableRoom.EquipmentDependencies?.Any() == true))
                                {
                                    var roomDependencies = localRoom.EquipmentDependencies.Where(depends =>
                                        equipmentAvailability.Any(equipCheck => equipCheck.EmailAddress == depends.EmailAddress && equipCheck.Status == true))
                                        .ToList();

                                    localRoom.EquipmentDependencies = roomDependencies;
                                }
                            }
                        }
                        results.AddRange(availabilityResult);
                    }
                }

                Logger.Logging(LogEventLevel.Debug, measurePerformance.StopTimer(serializedInput));
                return Ok(results);
            }
            catch (Exception ex)
            {
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                // Let the caller know we failed
                return BadRequest("Exception occurred.");
            }
        }

        /// <summary>
        /// Retreive an individual calendar/event
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="mailboxEmail"></param>
        /// <returns></returns>
        [HttpGet()]
        [Route("v1.0/getitem")]
        public IActionResult GetItem(string itemId, string mailboxEmail)
        {
            Logger.Logging(LogEventLevel.Information, $"GetItem ({mailboxEmail}) at {DateTime.UtcNow}");
            try
            {
                var model = ExchangeContextService.GetAppointmentAsync(mailboxEmail, itemId);
                if (model != null)
                {
                    Logger.Logging(LogEventLevel.Information, $"calendarEvents found {itemId}");
                    return Ok(model);
                }
            }
            catch (Microsoft.Graph.ClientException cex)
            {
                Logger.Logging(LogEventLevel.Error, $"Failed in request {cex.GetBaseException().Message}");
            }
            catch (Microsoft.Graph.ServiceException sex)
            {
                Logger.Logging(LogEventLevel.Error, $"Failed in request {sex.GetBaseException().Message}");
            }
            catch (Exception ex)
            {
                Logger.Logging(LogEventLevel.Error, $"Failed in request {ex.Message}");
            }
            return NotFound($"{itemId} not found.");
        }

        /// <summary>
        /// This API is primarily for testing/troubleshooting purposes
        /// </summary>
        /// <param name="roomLocationName"></param>
        /// <returns></returns>
        [HttpGet()]
        [Route("v1.0/roomListRooms/{roomLocationName}")]
        public IActionResult RoomListRooms([FromRoute] string roomLocationName)
        {
            try
            {
                var result = (from r in ExchangeContext.GetRooms()
                              where r.RoomList != null && string.Compare(r.RoomList.DisplayName, roomLocationName, true) == 0
                              select r).ToArray();

                return Ok(result);
            }
            catch (LoggedException lex)
            {
                // Already logged, so, all we need to do is let the caller know we failed
                return BadRequest(string.Format("Failed to retrieve rooms {0}", lex.Message));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                return BadRequest(string.Format("Failed to retrieve rooms {0}", ex));
            }
        }

        [HttpGet()]
        [Route("v1.0/roomLocations")]
        public IActionResult RoomLists(string[] excludedPrefixes)
        {

#if DEBUG
            if (excludedPrefixes == null || !excludedPrefixes.Any())
            {
                excludedPrefixes = new string[] { "2016" };
            }
#endif

            try
            {
                var result = ExchangeContext.GetRoomsListing()
                    .Where(room => !excludedPrefixes.Any(roomPrefix => room.DisplayName.StartsWith(roomPrefix, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(ob => ob.DisplayName);
                return Ok(result);
            }
            catch (LoggedException lex)
            {
                // Already logged, so, all we need to do is let the caller know we failed
                return BadRequest(string.Format("Failed to retrieve rooms {0}", lex.Message));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                return BadRequest(string.Format("Failed to retrieve rooms {0}", ex));
            }
        }

        [HttpGet()]
        [Route("v1.0/RoomLists")]
        public IActionResult RoomLists()
        {
            var result = new List<ADEntry>();
            try
            {
                // Force it to serialize and ignore those elements that should not be returned
                // Without this, a prompt to login occurs (????  not sure why) and ajax call fails but error is blank and status code is 0
                var ldapQuery = ExchangeContext.GetRoomsListing().Select(rl => rl as ADEntry).OrderBy(rl => rl.DisplayName);
                result.AddRange(ldapQuery);
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
            }

            return Ok(result);
        }

        [HttpGet()]
        [Route("v1.0/equipmentTypes")]
        public IActionResult EquipmentTypes()
        {
            var result = new List<string>();
            try
            {
                Logger.Logging(LogEventLevel.Information, "RoomDataService.EquipmentTypes");
                result.AddRange(ExchangeContext.GetRoomEquipmentTypes().OrderBy(ob => ob));
                Logger.Logging(LogEventLevel.Information, $"RoomDataService.EquipmentTypes - returning {result.Count} entries");
            }
            catch (LoggedException)
            {
                // Already logged, so, all we need to do is let the caller know we failed
                throw;
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                throw;
            }

            return Ok(result);
        }

        [HttpGet()]
        [Route("v1.0/states")]
        public IActionResult States()
        {
            var result = new List<string>();
            try
            {
                Logger.Logging(LogEventLevel.Information, "RoomDataService.States");
                NodeSortedList ldapQuery = ExchangeContext.GetRoomLocations();
                result.AddRange(ldapQuery.Keys.OrderBy(stateText => stateText));
            }
            catch (LoggedException ex)
            {
                return BadRequest(ex.ToString());
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                return BadRequest(ex.ToString());
            }

            return Ok(result);
        }

        [HttpGet()]
        [Route("v1.0/cities")]
        public IActionResult Cities(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                ModelState.AddModelError("state", "RoomDataService.Officees:  State should not be null");
                Logger.Logging(LogEventLevel.Warning, "RoomDataService.Officees:  State should not be null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = new List<string>();
            try
            {
                NodeSortedList ldapQuery = ExchangeContext.GetRoomLocations();
                Logger.Logging(LogEventLevel.Debug, $"Debugging FirstKeyCount => {ldapQuery?.Keys.Count}");
                NodeSortedList cities = ldapQuery?[state];
                Logger.Logging(LogEventLevel.Debug, $"Debugging SecondKeyCount => {cities?.Keys.Count}");
                result.AddRange(cities?.Keys.OrderBy(cityText => cityText));
            }
            catch (LoggedException)
            {
                // Already logged, so, all we need to do is let the caller know we failed
                return BadRequest($"Failed to find cities in {state}.");
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                return BadRequest($"Failed to find cities in {state}.");
            }

            return Ok(result);
        }


        [HttpGet()]
        [Route("v1.0/officees")]
        public IActionResult Officees(string state, string city)
        {
            if (string.IsNullOrEmpty(state))
            {
                ModelState.AddModelError("state", "RoomDataService.Officees:  State should not be null");
                Logger.Logging(LogEventLevel.Warning, "RoomDataService.Officees:  State should not be null");
            }

            if (string.IsNullOrEmpty(city))
            {
                ModelState.AddModelError("city", "RoomDataService.Officees:  City should not be null");
                Logger.Logging(LogEventLevel.Warning, "RoomDataService.Officees:  City should not be null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = new List<string>();
            try
            {
                NodeSortedList ldapQuery = ExchangeContext.GetRoomLocations();
                NodeSortedList offices = ldapQuery[state][city];
                result.AddRange(offices.Keys.OrderBy(officeText => officeText));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                return BadRequest($"Failed to find offices in {city}, {state}.");
            }

            return Ok(result);
        }

        [HttpGet()]
        [Route("v1.0/floors")]
        public IActionResult Floors(string state, string city, string office)
        {
            if (string.IsNullOrEmpty(state))
            {
                ModelState.AddModelError("state", "RoomDataService.Officees:  State should not be null");
                Logger.Logging(LogEventLevel.Warning, "RoomDataService.Officees:  State should not be null");
            }

            if (string.IsNullOrEmpty(city))
            {
                ModelState.AddModelError("city", "RoomDataService.Officees:  City should not be null");
                Logger.Logging(LogEventLevel.Warning, "RoomDataService.Officees:  City should not be null");
            }

            if (string.IsNullOrEmpty(office))
            {
                ModelState.AddModelError("office", "RoomDataService.Officees:  Office should not be null");
                Logger.Logging(LogEventLevel.Warning, "RoomDataService.Officees:  Office should not be null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = new List<string>();
            try
            {
                NodeSortedList ldapQuery = ExchangeContext.GetRoomLocations();
                NodeSortedList filteredFloors = ldapQuery[state][city][office];

                result.AddRange(filteredFloors.Keys.OrderBy(floorText => floorText));
            }
            catch (LoggedException)
            {
                // Already logged, so, all we need to do is let the caller know we failed
                return BadRequest($"Failed with find Floors in {city}, {state} => {office}.");
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                return BadRequest($"Failed with find Floors in {city}, {state} => {office}.");
            }

            return Ok(result);
        }

        [HttpGet()]
        [Route("v1.0/clearcache")]
        public void ClearCache()
        {
            // Clear all cache, room and equipment related
            ExchangeContext.ClearCache();
        }
    }
}
