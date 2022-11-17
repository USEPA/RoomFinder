namespace EPA.SharePoint.SysConsole.Models.Groups
{
    public class O365GroupMemberModel
    {
        public string DisplayName { get; set; }

        public string PrimarySmtpAddress { get; set; }

        public override string ToString()
        {
            return $"Name: {DisplayName}";
        }
    }
}
