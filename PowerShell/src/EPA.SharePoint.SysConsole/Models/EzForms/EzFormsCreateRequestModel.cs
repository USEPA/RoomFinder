using EPA.SharePoint.SysConsole.Models.Apps;

namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    public class EzFormsCreateRequestModel : ActiveDirectoryUserModel
    {
        public ActiveDirectoryUserModel DottedAccount { get; set; }

        public ActiveDirectoryUserModel ManagerAccount { get; set; }
        public string OfficeOverride { get; internal set; }
    }

}
