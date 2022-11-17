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
    [Route("api/equipmentDataService")]
    [Produces("application/json")]
    public class ODataEquipmentService : Controller
    {
        protected ILogger Logger { get; }
        private IAppSettings AppSettings { get; set; }
        private IExchangeContext ExchangeContext { get; set; }
        private IExchangeService ExchangeService { get; set; }

        public ODataEquipmentService(ILogger logger, IAppSettings config, IExchangeContext exchangeContext, IExchangeService exchangeService)
        {
            this.Logger = logger;
            AppSettings = config;
            ExchangeContext = exchangeContext;
            ExchangeService = exchangeService;
        }

        [HttpPost()]
        [Route("v1.0/find")]
        public async Task<IActionResult> Find([FromBody] FindResourceFilter filter)
        {
            string serializedInput = filter.ToString();
            Logger.Logging(LogEventLevel.Information, "EquipmentDataService.Find:  " + serializedInput);
            using var measurePerformance = new MeasurePerformance();

            try
            {
                var localEquipment = ExchangeContext.GetEquipmentListing();
                var equipment = (from l in localEquipment
                                 from i in l.Resources
                                 where (string.IsNullOrEmpty(filter.ListPath) || string.Compare(l.DisplayName, filter.ListPath, true) == 0)
                                    && (filter.RequiredEquipment == null || (filter.RequiredEquipment?.Count ?? 0) == 0 || filter.RequiredEquipment.Contains(i.EquipmentType))
                                    && (string.IsNullOrEmpty(filter.State) || string.Compare(i.Location.State, filter.State, true) == 0)
                                    && (string.IsNullOrEmpty(filter.City) || string.Compare(i.Location.City, filter.City, true) == 0)
                                    && (string.IsNullOrEmpty(filter.Office) || string.Compare(i.Location.Office, filter.Office, true) == 0)
                                    && (string.IsNullOrEmpty(filter.Floor) || string.Compare(i.Location.Floor, filter.Floor, true) == 0)
                                    && (i.RestrictionType != RestrictionType.Restricted || filter.IncludeRestricted)
                                 select i);
                if (equipment?.Any() == true)
                {
                    var result = await ExchangeService.GetAvailabilityAsync(equipment, new MeetingTimeWindow(filter.Start, filter.End), filter.IncludeUnavailable);
                    return Ok(result);
                }

                Logger.Logging(LogEventLevel.Debug, measurePerformance.StopTimer(serializedInput));

                return Ok(new List<ResourceItemEquipment>());
            }
            catch (LoggedException)
            {
                // Already logged, so, all we need to do is let the caller know we failed
                throw;
            }
            catch (Exception ex)
            {
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                // Let the caller know we failed
                throw;
            }
        }

        [ResponseCache(NoStore = false, Duration = 3600)]
        [HttpGet]
        [Route("v1.0/EquipmentLists")]
        public IActionResult EquipmentLists()
        {
            try
            {
                // Force it to serialize and ignore those elements that should not be returned
                // Without this, a prompt to login occurs (????  not sure why) and ajax call fails but error is blank and status code is 0
                var result = ExchangeContext.GetEquipmentListing().OrderBy(eq => eq.DisplayName).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
                ModelState.AddModelError("error", $"Failed to retrieve equipment lists.");
                return BadRequest(ModelState);
            }

        }

        [ResponseCache(NoStore = false, Duration = 3600)]
        [HttpGet]
        [Route("v1.0/EquipmentTypes")]
        public IActionResult EquipmentTypes()
        {
            var result = new List<string>();
            try
            {
                result.AddRange(ExchangeContext.GetEquipmentTypes().OrderBy(ob => ob));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
            }

            return Ok(result);
        }

        [ResponseCache(NoStore = false, Duration = 3600)]
        [HttpGet]
        [Route("v1.0/States")]
        public IActionResult States()
        {
            var result = new List<string>();
            try
            {
                Logger.Logging(LogEventLevel.Information, "EquipmentDataService.States");
                var ldapQuery = ExchangeContext.GetEquipmentLocations();
                result.AddRange(ldapQuery.Keys.OrderBy(ob => ob));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
            }

            return Ok(result);
        }


        [HttpGet]
        [Route("v1.0/Cities")]
        public IActionResult Cities(string state)
        {
            if (string.IsNullOrEmpty(state))
            {
                Logger.Logging(LogEventLevel.Warning, "EquipmentDataService.Cities:  State should not be null");
                ModelState.AddModelError("state", "State is a required filter.");
                return BadRequest(ModelState);
            }

            var result = new List<string>();
            try
            {

                var ldapQuery = ExchangeContext.GetEquipmentLocations();
                result.AddRange((ldapQuery[state].Keys as IEnumerable<string>).OrderBy(ob => ob));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
            }

            return Ok(result);
        }



        [HttpGet]
        [Route("v1.0/Officees")]
        public IActionResult Officees([FromRoute] string state, string city)
        {
            if (string.IsNullOrEmpty(state))
            {
                Logger.Logging(LogEventLevel.Warning, "EquipmentDataService.Officees:  State should not be null");
                ModelState.AddModelError("state", "State is a required filter.");
                return BadRequest(ModelState);
            }
            if (string.IsNullOrEmpty(city))
            {
                Logger.Logging(LogEventLevel.Warning, "EquipmentDataService.Officees:  City should not be null");
                ModelState.AddModelError("city", "City is a required filter.");
                return BadRequest(ModelState);
            }

            var result = new List<string>();
            try
            {

                var ldapQuery = ExchangeContext.GetEquipmentLocations();
                result.AddRange((ldapQuery[state][city].Keys as IEnumerable<string>).OrderBy(ob => ob));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
            }

            return Ok(result);
        }


        [HttpGet]
        [Route("v1.0/Floors")]
        public IActionResult Floors([FromRoute] string state, string city, string office)
        {
            if (string.IsNullOrEmpty(state))
            {
                Logger.Logging(LogEventLevel.Warning, "EquipmentDataService.Officees:  State should not be null");
                ModelState.AddModelError("state", "State is a required filter.");
                return BadRequest(ModelState);
            }
            if (string.IsNullOrEmpty(city))
            {
                Logger.Logging(LogEventLevel.Warning, "EquipmentDataService.Officees:  City should not be null");
                ModelState.AddModelError("city", "City is a required filter.");
                return BadRequest(ModelState);
            }
            if (string.IsNullOrEmpty(office))
            {
                Logger.Logging(LogEventLevel.Warning, "EquipmentDataService.Officees:  Office should not be null");
                ModelState.AddModelError("office", "Office is a required filter.");
                return BadRequest(ModelState);
            }

            var result = new List<string>();
            try
            {
                var ldapQuery = ExchangeContext.GetEquipmentLocations();
                result.AddRange((ldapQuery[state][city][office].Keys as IEnumerable<string>).OrderBy(ob => ob));
            }
            catch (Exception ex)
            {
                // Log and then let the caller know we failed
                Logger.Logging(LogEventLevel.Error, ex.ToString());
            }

            return Ok(result);
        }


        [HttpGet]
        [Route("v1.0/clearcache")]
        public void ClearCache()
        {
            // Clear all cache, room and equipment related
            ExchangeContext.ClearCache();
        }
    }
}
