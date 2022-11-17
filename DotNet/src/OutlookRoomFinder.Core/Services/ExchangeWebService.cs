using Microsoft.Graph;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.FileModels;
using OutlookRoomFinder.Core.Models.Filter;
using OutlookRoomFinder.Core.Models.Outlook;
using OutlookRoomFinder.Core.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;

namespace OutlookRoomFinder.Core.Services
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing as this is the Exception handler.")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing until sufficient code-coverage is in place.")]
    [SuppressMessage("Minor Code Smell", "S2221:\"Exception\" should not be caught when not required by called methods", Justification = "<Pending>")]
    [SuppressMessage("Major Code Smell", "S1200:Classes should not be coupled to too many other classes (Single Responsibility Principle)", Justification = "Pivotal Service")]
    public class ExchangeWebService : IExchangeService
    {
        protected ILogger Logger { get; }
        private readonly GraphServiceClient graphServiceClient;
        private readonly int GetUserAvailabilityBatchSize;
        private readonly string GraphEndpoint;
        private readonly GraphMsalAuthenticationProvider msalProvider;

        public ExchangeWebService(ILogger logger, IAppSettings appSettings)
        {
            this.Logger = logger;

            if (appSettings == null
                || appSettings?.Exchange == null
                || appSettings?.Graph == null)
            {
                throw new ArgumentNullException(nameof(appSettings));
            }

            GetUserAvailabilityBatchSize = appSettings.Exchange?.GetUserAvailabilityBatchSize ?? 75;

            // Graph API conversion
            GraphEndpoint = appSettings.Graph.DefaultEndpoint;
            var Authority = appSettings.AzureAd.Authority;
            var ClientId = appSettings.Graph.ClientId;
            var ClientSecret = appSettings.Graph.ClientSecret;

            var confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                      .WithClientSecret(ClientSecret)
                                                      .WithAuthority(new Uri(Authority))
                                                      .Build();

            msalProvider = new GraphMsalAuthenticationProvider(logger, confidentialClientApplication, new[] {
              appSettings.Graph.DefaultScope
            });

            graphServiceClient = new GraphServiceClient(baseUrl: GraphEndpoint, authenticationProvider: msalProvider);
        }

        #region MS Graph processing

        /// <summary>
        ///  See also <seealso cref="IExchangeService.GetAvailableUsersAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IList<User>> GetAvailableUsersAsync()
        {
            // PageIterator
            // https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/5c43c001610ed43968672278a56be6768f0099f3/tests/Microsoft.Graph.DotnetCore.Test/Tasks/PageIteratorTests.cs
            var userCollection = new List<User>();
            var userrequest = graphServiceClient.Users.Request();
            var users = await userrequest.GetAsync().ConfigureAwait(false);
            Logger.Logging(LogEventLevel.Information, $"users found {users?.Count()}");

            // Create complete collection
            var pageIterator = PageIterator<User>.CreatePageIterator(graphServiceClient, users, (usr) =>
            {
                userCollection.Add(usr);
                return true;
            });
            await pageIterator.IterateAsync().ConfigureAwait(false);

            return userCollection;
        }

        [SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "C#7 support not implemented")]
        public async Task<IEnumerable<IResourceItem>> GetAvailabilityAsync(IEnumerable<IResourceItem> resources, MeetingTimeWindow desiredAppointmentDateTime, bool includeUnavailable)
        {
            if (desiredAppointmentDateTime == null)
            {
                throw new ArgumentNullException(nameof(desiredAppointmentDateTime));
            }

            TimeSpan duration = desiredAppointmentDateTime?.Duration ?? new TimeSpan(0, 30, 0);
            CalenderJsonObject cal = new CalenderJsonObject
            {
                Schedules = new List<string>(resources.Select(s => s.EmailAddress)),
                StartTime = DateTimeTimeZone.FromDateTime(TimeZoneInfo.ConvertTimeToUtc(desiredAppointmentDateTime.StartTime, TimeZoneInfo.Local), TimeZoneInfo.Utc.Id),
                Endtime = DateTimeTimeZone.FromDateTime(TimeZoneInfo.ConvertTimeToUtc(desiredAppointmentDateTime.EndTime, TimeZoneInfo.Local), TimeZoneInfo.Utc.Id),
                AvailabilityViewInterval = duration.Minutes.ToString()
            };

            var serviceFullUrl = new Uri($"{GraphEndpoint}/users/{cal.Schedules.FirstOrDefault()}/calendar/getschedule");
            var jsonData = JsonConvert.SerializeObject(cal);
            using var jsoncontent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            var results = new List<IResourceItem>();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using HttpClient client = GraphClientFactory.Create(msalProvider);

            var retry = true;
            while (retry)
            {
                retry = false;
                var jsonResult = await InvokePostAsync(serviceFullUrl, async () =>
                {
                    var response = await client.PostAsync(serviceFullUrl, jsoncontent).ConfigureAwait(false);
                    return await DeserializeResult<GraphScheduleValueResult>(response).ConfigureAwait(false);
                }).ConfigureAwait(false);

                var schedules = jsonResult?.ScheduleValues.ConvertIntoSureEnumerable();

                ProcessSchedulesIntoResults(results, resources, schedules, includeUnavailable);

                if (!string.IsNullOrEmpty(jsonResult?.OdataNextLink))
                {
                    retry = true;
                    serviceFullUrl = new Uri(jsonResult.OdataNextLink);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns schedule model based on the mailboxes and the span of time in which you are reporting
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="desiredAppointmentDateTime"></param>
        /// <returns></returns>
        /// <remarks>
        /// Documentation for Batching
        ///     Batching: https://github.com/microsoftgraph/msgraph-sdk-design/blob/master/content/BatchRequestContent.md
        ///     JSON Batching: https://docs.microsoft.com/en-us/graph/json-batching?toc=.%2Fref%2Ftoc.json&view=graph-rest-beta
        /// </remarks>
        [SuppressMessage("Major Code Smell", "S1854:Unused assignments should be removed", Justification = "C#7 support not implemented")]
        [SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Graph Batch API is complex")]
        public async Task<IEnumerable<ConferenceScheduleModel>> GetBatchCalendarData<T>(IEnumerable<T> resources, MeetingTimeWindow desiredAppointmentDateTime) where T : IResourceItem
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }
            if (desiredAppointmentDateTime == null)
            {
                throw new ArgumentNullException(nameof(desiredAppointmentDateTime));
            }

            var mailboxes = new List<ConferenceScheduleModel>();

            var nextDay = desiredAppointmentDateTime.StartTime.AddDays(1);
            MeetingTimeWindow tw = new MeetingTimeWindow(desiredAppointmentDateTime.StartTime, (desiredAppointmentDateTime.EndTime < nextDay ? nextDay : desiredAppointmentDateTime.EndTime));
            var StartTime = TimeZoneInfo.ConvertTimeToUtc(tw.StartTime, TimeZoneInfo.Local);
            var Endtime = TimeZoneInfo.ConvertTimeToUtc(tw.EndTime, TimeZoneInfo.Local);


            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            // Send batch request with BatchRequestContent.
            using HttpClient client = GraphClientFactory.Create(msalProvider);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

            int batchCount = GetResourceBatchCount(resources);
            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                var batchServiceFullUrl = $"{GraphEndpoint}/$batch";
                var batchStartIndex = (batchIndex * GetUserAvailabilityBatchSize);
                var batchSize = Math.Min(resources.Count() - batchStartIndex, GetUserAvailabilityBatchSize);
                var batchedAttendees = new T[batchSize];
                resources.ToList().CopyTo(batchStartIndex, batchedAttendees, 0, batchSize);

                // Add batch request steps to BatchRequestContent.
                using BatchRequestContent batchRequestContent = new BatchRequestContent();
                int attendeeBatchIndex = 1;
                var batchSteps = new Dictionary<int, string>(batchSize);
                foreach (var batchAttendee in batchedAttendees)
                {
                    var serviceFullUrl = new Uri($"{GraphEndpoint}/users/{batchAttendee.EmailAddress}/calendarView?startDateTime={StartTime:s}&endDateTime={Endtime:s}");
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, serviceFullUrl);
                    requestMessage.Headers.TryAddWithoutValidation("Content-Type", "application/json");

                    // Create batch request steps with request ids.
                    batchSteps[attendeeBatchIndex] = batchAttendee.EmailAddress;
                    BatchRequestStep requestStep = new BatchRequestStep($"{attendeeBatchIndex++}", requestMessage, null);
                    batchRequestContent.AddBatchRequestStep(requestStep);
                }

                try
                {
                    HttpResponseMessage response = await client.PostAsync(new Uri(batchServiceFullUrl), batchRequestContent).ConfigureAwait(false);

                    // Handle http responses using BatchResponseContent.
                    BatchResponseContent batchResponseContent = new BatchResponseContent(response);
                    var responses = await batchResponseContent.GetResponsesAsync().ConfigureAwait(false);
                    responses.ForEach((message) =>
                    {
                        Logger.Logging(LogEventLevel.Debug, $"Batch response {message.Key} resulted in {message.Value.StatusCode}", new[] { "MSGraphBatch", batchServiceFullUrl });
                    });

                    foreach (var batchStep in batchSteps)
                    {
                        mailboxes.Add(await ProcessBatchStepsIntoResult(desiredAppointmentDateTime, client, batchStep, batchResponseContent).ConfigureAwait(false));
                    }

                    batchServiceFullUrl = await batchResponseContent.GetNextLinkAsync().ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(batchServiceFullUrl))
                    {
                        Logger.Logging(LogEventLevel.Debug, $"Batch returned {batchServiceFullUrl} which indicates the batch requires an additional page.", new[] { "MSGraphBatch" });
                    }
                }
                catch (Microsoft.Graph.ClientException cex)
                {
                    Logger.Logging(LogEventLevel.Error, $"Failed in request {cex.GetBaseException().Message}", new[] { "MSGraphBatch", "Microsoft.Graph.ClientException" });
                }
                catch (Microsoft.Graph.ServiceException sex)
                {
                    Logger.Logging(LogEventLevel.Error, $"Failed in request {sex.GetBaseException().Message}", new[] { "MSGraphBatch", "Microsoft.Graph.ServiceException" });
                }
                catch (Exception ex)
                {
                    Logger.Logging(LogEventLevel.Error, $"Failed in request {ex.Message}", new[] { "MSGraphBatch" });
                }
            }

            return mailboxes;
        }

        private int GetResourceBatchCount<T>(IEnumerable<T> resources) where T : IResourceItem
        {
            return (int)Math.Ceiling((double)resources.Count() / GetUserAvailabilityBatchSize);
        }

        /// <summary>
        /// Calls the Graph Calendar view for each mailbox specified in <paramref name="mailboxIds"/>
        /// </summary>
        /// <param name="desiredAppointmentDateTime">The specified start and endtime for a meeting</param>
        /// <param name="mailboxIds">The collection of mailboxes, the larger the collection the more requests made to the graph</param>
        /// <returns></returns>
        public async Task<IEnumerable<CalendarEventViewModel>> GetAppointmentsAsync(MeetingTimeWindow desiredAppointmentDateTime, ICollection<string> mailboxIds)
        {
            if (desiredAppointmentDateTime == null)
            {
                throw new ArgumentNullException(nameof(desiredAppointmentDateTime));
            }

            if (mailboxIds == null || !mailboxIds.Any())
            {
                return new List<CalendarEventViewModel>();
            }

            IList<QueryOption> options = new List<Microsoft.Graph.QueryOption>
            {
                new QueryOption("startDateTime", TimeZoneInfo.ConvertTimeToUtc(desiredAppointmentDateTime.StartTime, TimeZoneInfo.Local).ToString("s")),
                new QueryOption("endDateTime", TimeZoneInfo.ConvertTimeToUtc(desiredAppointmentDateTime.EndTime, TimeZoneInfo.Local).ToString("s")),
                new QueryOption("top", "10")
            };

            var model = new List<CalendarEventViewModel>();
            foreach (var mailboxId in mailboxIds)
            {
                try
                {
                    // Create complete collection
                    Logger.Logging(LogEventLevel.Information, $"Processing mailbox {mailboxId}");
                    var request = graphServiceClient.Users[mailboxId].CalendarView.Request(options).WithMaxRetry(5);
                    var events = await request.GetAsync().ConfigureAwait(false);

                    var pageIterator = PageIterator<Event>.CreatePageIterator(graphServiceClient, events, (ical) =>
                    {
                        Logger.Logging(LogEventLevel.Information, $"Processing mailbox {mailboxId} at event {ical.Start?.DateTime} TZ {ical.Start?.TimeZone}");
                        model.Add(ProcessCalendarEventToViewModel(mailboxId, ical));
                        return true;
                    });
                    await pageIterator.IterateAsync().ConfigureAwait(false);
                }
                catch (Microsoft.Graph.ClientException cex)
                {
                    Logger.Logging(LogEventLevel.Error, $"Processing mailbox {mailboxId} Failed in request {cex.GetBaseException().Message}");
                }
                catch (Microsoft.Graph.ServiceException sex)
                {
                    Logger.Logging(LogEventLevel.Error, $"Processing mailbox {mailboxId} Failed in request {sex.GetBaseException().Message}");
                }
                catch (Exception ex)
                {
                    Logger.Logging(LogEventLevel.Error, $"Processing mailbox {mailboxId} Failed in request {ex.Message}");
                }
            }
            return model;
        }

        /// <summary>
        /// See <seealso cref="IExchangeService.GetAppointmentAsync(string, string)"/>
        /// </summary>
        /// <param name="mailboxEmail">The mailbox to which the <paramref name="itemId"/> belongs.</param>
        /// <param name="itemId">Represents the Mail object Id</param>
        public async Task<CalendarEventViewModel> GetAppointmentAsync(string mailboxEmail, string itemId)
        {
            var request = graphServiceClient.Users[mailboxEmail].Events[itemId].Request().WithMaxRetry(5);
            var ical = await request.GetAsync().ConfigureAwait(false);
            if (ical != null)
            {
                return (new CalendarEventViewModel
                {
                    IsAllDayEvent = ical.IsAllDay ?? false,
                    IsCancelled = ical.IsCancelled ?? false,
                    MailboxId = mailboxEmail,
                    Location = ical.Location?.DisplayName,
                    LocationEmail = ical.Location?.UniqueId,
                    OrganizerName = ical.Organizer?.EmailAddress?.Name,
                    OrganizerEmail = ical.Organizer?.EmailAddress?.Address,
                    Subject = ical.Subject,
                    StartTime = ical.Start.ConvertUtcDateToLocalTimeZone(),
                    EndTime = ical.End.ConvertUtcDateToLocalTimeZone()
                });
            }
            return default;
        }


        #endregion

        private async Task<ConferenceScheduleModel> ProcessBatchStepsIntoResult(MeetingTimeWindow desiredAppointmentDateTime, HttpClient getclient, KeyValuePair<int, string> batchStep, BatchResponseContent batchResponseContent)
        {
            var model = new ConferenceScheduleModel { EmailAddress = batchStep.Value };
            var appointments = new List<CalendarEventViewModel>();

            using HttpResponseMessage httpResponse = await batchResponseContent.GetResponseByIdAsync($"{batchStep.Key}").ConfigureAwait(false);
            if (httpResponse == null)
            {
                Logger.Logging(LogEventLevel.Debug, $"HttpResponse null for {batchStep.Value}");
            }
            var data = await DeserializeResult<GraphOutlookItemValueResult>(httpResponse).ConfigureAwait(false);

            var shouldCrawlBatchResponse = true;
            while (shouldCrawlBatchResponse)
            {
                shouldCrawlBatchResponse = false;
                var outlookItems = data?.CalendarEvents.ConvertIntoSureEnumerable();
                outlookItems.ForEach(
                    (calendarEvent) =>
                    {
                        appointments.Add(ProcessOutlookItemToViewModel(calendarEvent, desiredAppointmentDateTime));
                    });

                if (!string.IsNullOrEmpty(data?.OdataNextLink))
                {
                    var serviceFullUrl = new Uri(data.OdataNextLink);
                    shouldCrawlBatchResponse = true;

                    var response = await getclient.GetAsync(serviceFullUrl).ConfigureAwait(false);
                    data = await DeserializeResult<GraphOutlookItemValueResult>(response).ConfigureAwait(false);
                }
            }

            if (appointments.Any())
            {
                model.Events = appointments;
            }

            return model;
        }

        private void ProcessSchedulesIntoResults(ICollection<IResourceItem> results, IEnumerable<IResourceItem> resources, IEnumerable<ScheduleInformation> schedules, bool includeUnavailable)
        {
            (from r in resources
             join ar in schedules on r.EmailAddress equals ar.ScheduleId
             where includeUnavailable || ar.ScheduleItems?.Any() == false
             select new
             {
                 Room = r,
                 Schedules = ar.ScheduleItems.ConvertIntoSureEnumerable()
             }).ForEach((resourceSchedule) =>
             {
                 var resource = resourceSchedule.Room;
                 var resourceScheduledItems = resourceSchedule.Schedules;

                 if (includeUnavailable || !resourceScheduledItems.Any())
                 {
                     var roomIsAvailable = !resourceScheduledItems.Any();
                     var dependenciesAreAvailable = CheckDependencySchedules(schedules, resource, roomIsAvailable);
                     var roomAndDependentsAreAvailable = roomIsAvailable && dependenciesAreAvailable;
                     if (!includeUnavailable && !roomAndDependentsAreAvailable)
                     {
                         return;
                     }

                     if (results.Any(findroom => findroom.EmailAddress.Equals(resource.EmailAddress, StringComparison.OrdinalIgnoreCase)))
                     {
                         results.FirstOrDefault(findroom => findroom.EmailAddress.Equals(resource.EmailAddress, StringComparison.OrdinalIgnoreCase)).UpdateAvailabilityStatus(roomAndDependentsAreAvailable);
                     }
                     else
                     {
                         resource.UpdateAvailabilityStatus(roomAndDependentsAreAvailable);
                         results.Add(resource);
                     }
                 }
             });
        }

        /// <summary>
        /// Validate the resource
        /// </summary>
        /// <param name="schedules"></param>
        /// <param name="resource"></param>
        /// <param name="roomIsAvailable"></param>
        /// <returns>
        ///     (TRUE) if dependent resources are available and the room is available
        ///     (FALSE) if either the room or any of the dependents are NOT available
        /// </returns>
        private bool CheckDependencySchedules(IEnumerable<ScheduleInformation> schedules, IResourceItem resource, bool roomIsAvailable)
        {
            if (!roomIsAvailable)
            {
                // No need to process dependencies if room is not available
                return roomIsAvailable;
            }

            // Room has a related room (upon which it is dependent or forces dependent setting)
            if (resource.EntryType == MeetingAttendeeType.Room && resource.Dependencies?.Any() == true)
            {
                var dependenciesWithSchedules = resource.Dependencies.Join(schedules,
                    resourceDependency => resourceDependency,
                    graphSchedule => graphSchedule.ScheduleId,
                    (resourceDependency, graphSchedule) => new
                    {
                        DependentRoom = resourceDependency,
                        Schedules = graphSchedule?.ScheduleItems.ConvertIntoSureEnumerable()
                    })
                .Where(dependency => dependency.Schedules.Any());

                // Return the status of any dependents [if NO schedules, then dependents are available]
                roomIsAvailable = dependenciesWithSchedules?.Any() == false;
            }

            return roomIsAvailable;
        }

        public bool? IsEventDuringAppointmentTime(MeetingTimeWindow desiredAppointmentDateTime, MeetingTimeWindow eventWindow)
        {
            _ = eventWindow ?? throw new ArgumentNullException(nameof(eventWindow));
            _ = desiredAppointmentDateTime ?? throw new ArgumentNullException(nameof(desiredAppointmentDateTime));

            return ((eventWindow.StartTime >= desiredAppointmentDateTime.StartTime && eventWindow.StartTime < desiredAppointmentDateTime.EndTime)
                || (eventWindow.EndTime <= desiredAppointmentDateTime.EndTime && desiredAppointmentDateTime.EndTime >= eventWindow.StartTime));
        }

        public bool DoesNotOverlap(IEnumerable<CalendarEvent> meetings, MeetingTimeWindow desiredAppointmentDateTime)
        {
            _ = meetings ?? throw new ArgumentNullException(nameof(meetings));
            _ = desiredAppointmentDateTime ?? throw new ArgumentNullException(nameof(desiredAppointmentDateTime));

            var foundAppointmentTimes = meetings.Any(e =>
            (desiredAppointmentDateTime.StartTime >= e.StartTime && desiredAppointmentDateTime.StartTime < e.EndTime)
            || (desiredAppointmentDateTime.EndTime > e.StartTime && desiredAppointmentDateTime.EndTime < e.EndTime));
            if (!foundAppointmentTimes)
            {
                return true;
            }
            return false;
        }

        public bool DoesNotOverlap(IEnumerable<MeetingTimeWindow> meetings)
        {
            DateTime endPrior = DateTime.MinValue;
            foreach (var meeting in meetings.OrderBy(x => x.StartTime))
            {
                if (meeting.StartTime > meeting.EndTime)
                {
                    return false;
                }

                if (meeting.StartTime < endPrior)
                {
                    return false;
                }

                endPrior = meeting.EndTime;
            }
            return true;
        }


        private CalendarEventViewModel ProcessCalendarEventToViewModel(string mailboxId, Event calendarEvent)
        {
            var start = calendarEvent.Start;
            var end = calendarEvent.End;

            return new CalendarEventViewModel
            {
                StartTime = TimeZoneInfo.ConvertTime(DateTime.Parse(start.DateTime), TimeZoneInfo.Utc, TimeZoneInfo.Local),
                EndTime = TimeZoneInfo.ConvertTime(DateTime.Parse(end.DateTime), TimeZoneInfo.Utc, TimeZoneInfo.Local),
                TimeZone = TimeZoneInfo.Local.ToString(),
                Location = calendarEvent.Location?.DisplayName ?? string.Empty,
                LocationEmail = calendarEvent.Location?.UniqueId,
                Subject = calendarEvent.Subject,
                IsAllDayEvent = calendarEvent.IsAllDay,
                IsCancelled = calendarEvent.IsCancelled,
                IsRecurring = calendarEvent.Recurrence?.Pattern != null,
                IsOnlineMeeting = !string.IsNullOrEmpty(calendarEvent.OnlineMeetingUrl),
                MailboxId = mailboxId,
                OrganizerName = calendarEvent.Organizer?.EmailAddress?.Name,
                OrganizerEmail = calendarEvent.Organizer?.EmailAddress?.Address,
                ShowAs = calendarEvent.ShowAs
            };
        }

        private CalendarEventViewModel ProcessOutlookItemToViewModel(Event calendarEvent, MeetingTimeWindow desiredAppointmentDateTime)
        {
            var start = TimeZoneInfo.ConvertTime(DateTime.Parse(calendarEvent.Start.DateTime), TimeZoneInfo.Utc, TimeZoneInfo.Local);
            var end = TimeZoneInfo.ConvertTime(DateTime.Parse(calendarEvent.End.DateTime), TimeZoneInfo.Utc, TimeZoneInfo.Local);
            var eventWindow = new MeetingTimeWindow(start, end);
            var isEventDuringWindow = IsEventDuringAppointmentTime(desiredAppointmentDateTime, eventWindow);

            return new CalendarEventViewModel
            {
                StartTime = start,
                EndTime = end,
                TimeZone = TimeZoneInfo.Local.ToString(),
                Location = calendarEvent.Location?.DisplayName ?? string.Empty,
                LocationEmail = calendarEvent.Location?.UniqueId,
                Subject = calendarEvent?.Subject ?? string.Empty,
                BusyStatus = calendarEvent.ShowAs ?? FreeBusyStatus.Unknown,
                IsAllDayEvent = calendarEvent.IsAllDay ?? false,
                OriginalStart = calendarEvent.OriginalStart,
                Status = isEventDuringWindow ?? false
            };
        }

        private async Task<T> InvokePostAsync<T>(Uri serviceFullUrl, Func<Task<T>> clientAsync, int maxAttempts = 3, int backoffIntervalInSeconds = 6) where T : GraphOdata
        {
            var result = default(T);
            var retryAttempts = 0;
            bool retry = true;
            while (retry)
            {
                try
                {
                    retry = false;
                    result = await clientAsync().ConfigureAwait(false);
                }
                catch (WebException wex)
                {
                    // Check if request was throttled - http status code 429
                    // Check is request failed due to server unavailable - http status code 503
                    if (wex.Response is HttpWebResponse response &&
                        (response.StatusCode == (HttpStatusCode)429 // Service throttling [use retry logic]
                            || response.StatusCode == (HttpStatusCode)503 // Service unavailable [Azure API - unavailable || use retry logic]
                            || response.StatusCode == (HttpStatusCode)504 // Gateway Timeout [Azure API - timeout on response || use retry logic]
                            ))
                    {
                        TimeSpan backoffSpan = ExtractBackoffTimeSpan(response, backoffIntervalInSeconds);
                        Logger.Logging(LogEventLevel.Warning, $"Microsoft Graph API => exceeded usage limits. Iteration => {backoffSpan.Seconds} Sleeping for {retryAttempts} seconds before retrying..");

                        //Add delay for retry
                        await Task.Delay(backoffSpan).ConfigureAwait(false);

                        //Add to retry count and check max attempts.
                        retryAttempts++;
                        retry = (retryAttempts < maxAttempts);
                    }
                    else
                    {
                        Logger.Logging(LogEventLevel.Error, $"HTTP Failed to query URI {serviceFullUrl} exception: {wex}");
                        throw;
                    }
                }
                catch (AggregateException agex)
                {
                    agex.InnerExceptions.ForEach(exception => { Logger.Logging(LogEventLevel.Warning, $"AggregateException URI {serviceFullUrl} => {exception.Message}"); });
                    throw new Exception($"Multiple errors occurred, check logs and assert {serviceFullUrl}");
                }
                catch (Exception ex)
                {
                    Logger.Logging(LogEventLevel.Warning, $"Generic Failed to query URI {serviceFullUrl} => {ex.Message}");
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Extract the Retry-After throttling suggestion
        /// </summary>
        /// <param name="response"></param>
        /// <param name="backoffIntervalInSeconds"></param>
        /// <returns></returns>
        private static TimeSpan ExtractBackoffTimeSpan(HttpWebResponse response, int backoffIntervalInSeconds = 6)
        {
            var graphBackoffInterval = backoffIntervalInSeconds;
            var graphApiRetrySeconds = response.GetResponseHeader("Retry-After");
            if (!string.IsNullOrEmpty(graphApiRetrySeconds)
                && int.TryParse(graphApiRetrySeconds, out int headergraphBackoffInterval))
            {
                graphBackoffInterval = headergraphBackoffInterval <= 0 ? backoffIntervalInSeconds : headergraphBackoffInterval;
            }
            var backoffSpan = TimeSpan.FromSeconds(graphBackoffInterval);
            return backoffSpan;
        }

        [SuppressMessage("Minor Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Passing parameters to avoid requires more sloppy code.")]
        private async Task<T> DeserializeResult<T>(HttpResponseMessage response)
        {
            if (response == null)
            {
                Logger.Logging(LogEventLevel.Debug, $"Failed to retreive HttpResponse.");
                return default;
            }

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                Logger.Logging(LogEventLevel.Warning, $"Failed to call the Web Api: {response.StatusCode}");
                string responsecontent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.Logging(LogEventLevel.Warning, $"Failed to call the Web Api: {responsecontent}");
                var errorModel = JsonConvert.DeserializeObject<Microsoft.Graph.Error>(responsecontent);
                Logger.Logging(LogEventLevel.Error, $"Error message {response.ReasonPhrase}.  Error details {errorModel.Message}.");
            }

            return default;
        }
    }
}