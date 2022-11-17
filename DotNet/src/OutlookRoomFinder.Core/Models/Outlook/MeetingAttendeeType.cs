namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Defines the type of a meeting attendee.
    /// </summary>
    public enum MeetingAttendeeType
    {
        /// <summary>
        /// The attendee is the organizer of the meeting.
        /// </summary>
        Organizer,

        /// <summary>
        /// The attendee is required.
        /// </summary>
        Required,

        /// <summary>
        /// The attendee is optional.
        /// </summary>
        Optional,

        /// <summary>
        /// The attendee is a room.
        /// </summary>
        Room,

        /// <summary>
        /// The attendee is a resource.
        /// </summary>
        Resource
    }
}
