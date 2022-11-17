namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Represents a class that can self-validate.
    /// </summary>
    internal interface ISelfValidate
    {
        /// <summary>
        /// Validates this instance.
        /// </summary>
        void Validate();
    }
}
