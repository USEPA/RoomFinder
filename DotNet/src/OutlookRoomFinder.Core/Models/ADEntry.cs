using System.ComponentModel.DataAnnotations;

namespace OutlookRoomFinder.Core.Models
{
    /// <summary>
    /// Represents the Core ADObject
    /// </summary>
    public class ADEntry : IADEntry
    {
        public ADEntry() { }

        public ADEntry(string name, string emailAddress) : this()
        {
            DisplayName = name;
            EmailAddress = emailAddress;
        }

        [Key]
        public string EmailAddress { get; set; }
        public string SamAccountName { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            if(!string.IsNullOrEmpty(DisplayName))
            {
                return $"AD Entry {DisplayName}";
            }
            return base.ToString();
        }
    }
}
