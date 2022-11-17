namespace OutlookRoomFinder.Core.Models.FileModels
{
    public interface IRestrictedDelegatesModel
    {
        string UserPrincipalName { get; set; }
        string UserType { get; set; }
    }
}