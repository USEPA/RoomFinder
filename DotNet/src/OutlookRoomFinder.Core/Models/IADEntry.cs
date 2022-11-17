namespace OutlookRoomFinder.Core.Models
{
    public interface IADEntry
    {
        string SamAccountName { get; set; }
        string DisplayName { get; set; }
        string EmailAddress { get; set; }
    }
}