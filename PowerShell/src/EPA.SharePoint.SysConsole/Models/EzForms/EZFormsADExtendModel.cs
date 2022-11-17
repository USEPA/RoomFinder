namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EZFormsADExtendModel : EZFormsADModel
    {
        public EZFormsADExtendModel() : base()
        {
            Modified = false;
            Disabled = false;
            Recertification = false;
            Failed = false;
        }

        /// <summary>
        /// the original request being sent back
        /// </summary>
        public EzFormsRequestModel SharepointUserObject { get; set; }

        /// <summary>
        /// Incicates a status message
        /// </summary>
        public string WFIDProblem { get; set; }

        /// <summary>
        /// Issue with employee number
        /// </summary>
        public string EmployeeNumberProblem { get; set; }

        /// <summary>
        /// The user was found in AD and the value should be updated
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// The user should be disabled
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Indicates we are pushing the expiration out a specific amount
        /// </summary>
        public bool Recertification { get; set; }
        /// <summary>
        /// Indicates an exception occurred in the processing of this file
        /// </summary>
        public bool Failed { get; private set; }
    }
}
