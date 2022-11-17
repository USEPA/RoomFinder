using Microsoft.Graph;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.Outlook;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Core.Services
{
    public interface IExchangeService
    {
        /// <summary>
        /// Calls the MS Graph Batch API to process requests in large batches
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="desiredAppointmentDateTime"></param>
        /// <returns></returns>
        Task<IEnumerable<ConferenceScheduleModel>> GetBatchCalendarData<T>(IEnumerable<T> resources, MeetingTimeWindow desiredAppointmentDateTime) where T : IResourceItem;

        /// <summary>
        /// Evaluates each of the resources against the desired appointment datetime
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="desiredAppointmentDateTime"></param>
        /// <param name="includeUnavailable"></param>
        /// <returns></returns>
        Task<IEnumerable<IResourceItem>> GetAvailabilityAsync(IEnumerable<IResourceItem> resources, MeetingTimeWindow desiredAppointmentDateTime, bool includeUnavailable);

        /// <summary>
        /// Posts an Graph Client request to retreive the specific mailbox item
        /// </summary>
        /// <param name="mailboxEmail">The mailbox to which the <paramref name="itemId"/> belongs.</param>
        /// <param name="itemId">Represents the Mail object Id</param>
        Task<CalendarEventViewModel> GetAppointmentAsync(string mailboxEmail, string itemId);

        Task<IEnumerable<CalendarEventViewModel>> GetAppointmentsAsync(MeetingTimeWindow desiredAppointmentDateTime, ICollection<string> mailboxIds);

        /// <summary>
        ///  Search Users in the Tenant
        /// </summary>
        /// <returns></returns>
        Task<IList<User>> GetAvailableUsersAsync();

        /// <summary>
        /// Evalutes if the appointmentTime is in the eventWindow
        /// </summary>
        /// <param name="desiredAppointmentDateTime"></param>
        /// <param name="eventWindow"></param>
        /// <returns></returns>
        bool? IsEventDuringAppointmentTime(MeetingTimeWindow desiredAppointmentDateTime, MeetingTimeWindow eventWindow);

        /// <summary>
        /// Evaluates the appointment time to validate if any of the meeting occurrences are in the appointment time
        /// </summary>
        /// <param name="meetings"></param>
        /// <param name="desiredAppointmentDateTime"></param>
        /// <returns></returns>
        bool DoesNotOverlap(IEnumerable<CalendarEvent> meetings, MeetingTimeWindow desiredAppointmentDateTime);

        /// <summary>
        /// Evaluates the collection to validate if any datetime overlaps
        /// </summary>
        /// <param name="meetings"></param>
        /// <returns></returns>
        bool DoesNotOverlap(IEnumerable<MeetingTimeWindow> meetings);
    }
}
