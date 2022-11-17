using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public class Remotemapper_Base
    {
        public const string Field_ID = "ID";
        public const string Field_Title = "Title";
        public const string Field_Author = "Author";
        public const string Field_Created = "Created";
        public const string Field_Editor = "Editor";
        public const string Field_Modified = "Modified";
        public const string Field_FileRef = "FileRef";
        public const string Field_FileDirRef = "FileDirRef";
        public const string Field_FileLeafRef = "FileLeafRef";
        public const string Field_LinkTitleNoMenu = "LinkTitleNoMenu";
        public const string Field_DocIcon = "DocIcon";
    }

    /// <summary>
    /// Network drives
    /// </summary>
    public class Remotemapper_Networks : Remotemapper_Base
    {
        public const string ListName = "RemoteMapperNetworks";


        public const string Field_Description = "Description";
        public const string Field_DriveLetter = "DriveLetter";
        public const string Field_GroupName = "GroupName";
        public const string Field_AutoSelectBool = "AutoSelect";
        public const string Field_OrganizationLookup = "Organization";
        public const string Field_SubOrganizationChoice = "SubOrganization";
    }

    /// <summary>
    /// Organization Lookup List
    /// </summary>
    public class Remotemapper_Organization : Remotemapper_Base
    {
        public const string ListName = "RemoteMapperOrganization";


    }
}
