using OutlookRoomFinder.Core.Services;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Represents information about an attendee for which to request availability information.
    /// </summary>
    public sealed class AttendeeInfo : ISelfValidate
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendeeInfo"/> class.
        /// </summary>
        public AttendeeInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendeeInfo"/> class.
        /// </summary>
        /// <param name="smtpAddress">The SMTP address of the attendee.</param>
        /// <param name="attendeeType">The yype of the attendee.</param>
        /// <param name="excludeConflicts">Indicates whether times when this attendee is not available should be returned.</param>
        public AttendeeInfo(string smtpAddress, MeetingAttendeeType attendeeType, bool excludeConflicts)
            : this()
        {
            this.SmtpAddress = smtpAddress;
            this.AttendeeType = attendeeType;
            this.ExcludeConflicts = excludeConflicts;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttendeeInfo"/> class.
        /// </summary>
        /// <param name="smtpAddress">The SMTP address of the attendee.</param>
        public AttendeeInfo(string smtpAddress)
            : this(smtpAddress, MeetingAttendeeType.Required, false)
        {
            this.SmtpAddress = smtpAddress;
        }

        /// <summary>
        /// Defines an implicit conversion between a string representing an SMTP address and AttendeeInfo.
        /// </summary>
        /// <param name="smtpAddress">The SMTP address to convert to AttendeeInfo.</param>
        /// <returns>An AttendeeInfo initialized with the specified SMTP address.</returns>
        public static implicit operator AttendeeInfo(string smtpAddress)
        {
            return new AttendeeInfo(smtpAddress);
        }

        /// <summary>
        /// Gets or sets the SMTP address of this attendee.
        /// </summary>
        public string SmtpAddress { get; set; }

        /// <summary>
        /// Gets or sets the type of this attendee.
        /// </summary>
        public MeetingAttendeeType AttendeeType { get; set; } = MeetingAttendeeType.Required;

        /// <summary>
        /// Gets or sets a value indicating whether times when this attendee is not available should be returned.
        /// </summary>
        public bool ExcludeConflicts { get; set; }

        #region ISelfValidate Members

        /// <summary>
        /// Validates this instance.
        /// </summary>
        void ISelfValidate.Validate()
        {
            ExchangeUtilities.ValidateParam(this.SmtpAddress, "SmtpAddress");
        }

        #endregion
    }
}