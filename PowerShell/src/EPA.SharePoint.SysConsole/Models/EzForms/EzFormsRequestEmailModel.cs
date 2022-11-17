using System;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// EZ Forms Request Model with Email Properties
    /// </summary>
    public class EzFormsRequestEmailModel : EzFormsRequestModel
    {
        /// <summary>
        /// Initialize entity
        /// </summary>
        public EzFormsRequestEmailModel() : base()
        {
        }

        /// <summary>
        /// SharePoint Email: Subject
        /// </summary>
        public string subject { get; set; }

        /// <summary>
        /// SharePoint Email: message
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// The date/time the message was sent
        /// </summary>
        public DateTime messageSent { get; set; }

        /// <summary>
        /// The type of email notification being sent
        /// </summary>
        public NotificationType emailType { get; set; }
    }
}
