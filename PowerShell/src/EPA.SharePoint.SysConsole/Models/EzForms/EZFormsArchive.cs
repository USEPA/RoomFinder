namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// Represents a report for EzForm requests
    /// </summary>
    public class EZFormsArchive : EzFormsEmailBase
    {
        /// <summary>
        /// Initialize the model
        /// </summary>
        public EZFormsArchive()
        {
            this.TotalDays = 0;
        }

        public int TotalDays { get; set; }

        public string TypeOfEmailNotification { get; set; }

    }
}
