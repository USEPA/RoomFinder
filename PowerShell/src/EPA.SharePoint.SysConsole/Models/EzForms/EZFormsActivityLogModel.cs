namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EZFormsActivityLogModel
    {
        public string RoutingPhase { get; set; }

        public string ApproverAction { get; set; }

        public string UserTitle { get; set; }

        public string AlternateTitle { get; set; }

        public string ActivityDate { get; set; }

        public override string ToString()
        {
            return string.Format(EzForms_AccessRequest.Formatted_Log, RoutingPhase, ApproverAction, UserTitle, AlternateTitle, ActivityDate);
        }
    }
}
