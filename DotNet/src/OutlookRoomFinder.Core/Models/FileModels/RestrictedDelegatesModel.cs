namespace OutlookRoomFinder.Core.Models.FileModels
{
    public class RestrictedDelegatesModel : IRestrictedDelegatesModel
    {
        public string UserPrincipalName { get; set; }

        public string UserType { get; set; }
    }
}