namespace EPA.SharePoint.SysConsole.Models.Apps
{
    public static class Constants_SiteRequest
    {

        /// <summary>
        /// Contains the Display name for the SiteRequests
        /// </summary>
        public const string SiteRequestListName = "SiteRequests";


        public class SiteRequestFields : ConstantsListFields
        {
            public const string FieldLookup_SiteCollectionName = "SiteCollectionName";
            public const string FieldText_SiteName = "SiteName";
            public const string FieldText_SiteURL = "SiteURL";
            public const string FieldText_SiteOwner = "SiteOwner";
            public const string FieldBoolean_SiteExists = "SiteExists";
            public const string FieldBoolean_SiteMovedOrDeleted = "SiteMovedOrDeleted";
            public const string FieldMultiText_Description = "Description";
            public const string FieldText_JoinFlag = "JoinFlag";
            public const string FieldText_OpenFlag = "OpenFlag";
            public const string FieldLookup_AAShipRegionOffice = "AAShipRegionOffice";
            public const string FieldText_Topics = "Topics";
            public const string ChoiceField_TypeOfSite = "TypeOfSite";
            public const string FieldText_TypeOfSiteID = "TypeOfSiteID";
            public const string FieldText_OfficeTeamCommunityName = "OfficeTeamCommunityName";
            public const string FieldText_IntendedAudience = "IntendedAudience";
            public const string FieldText_EpaLineOfBusiness = "EpaLineOfBusiness";
            public const string FieldText_TemplateName = "TemplateName";
            public const string FieldText_SiteSponsor = "SiteSponsor";
            public const string FieldText_OrganizationAcronym = "OrganizationAcronym";
            public const string FieldText_RequestCompletedFlag = "RequestCompletedFlag";
            public const string FieldText_RequestRejectedFlag = "RequestRejectedFlag";
            public const string FieldText_RequestorEmail = "RequestorEmail";
            public const string FieldText_SCAEmail = "SCAEmail";
            public const string FieldText_SiteOwnerName = "SiteOwnerName";
            public const string FieldText_CustomSite = "CustomSite";
            public const string FieldText_missingMetaDataId = "missingMetaDataId";
            public const string FieldText_SiteSponsorApprovedFlag = "SiteSponsorApprovedFlag";
            public const string FieldMultiText_RequestRejectedComment = "RequestRejectedComment";
            public const string FieldText_WebGuid = "WebGuid";
        }


        /// <summary>
        /// Contains the display name for the list which contains users associated with Site Collections
        /// </summary>
        public const string AdminsLK = "AdminsLK";

        /// <summary>
        /// Contains the constant list column field names
        /// </summary>
        public class AdminsLKFields : ConstantsListFields
        {
            public const string FieldUser_AdminEmailObject = "AdminEmailObject";
            public const string FieldText_AdminEmail = "AdminEmail";
            public const string FieldLookup_CollectionSiteType = "CollectionSiteType";
            public const string FieldText_EmailTo = "EmailTo";
            public const string FieldBoolean_NonSCAKeeper = "Non_x002d_SCA_x0020_Keeper";
            public const string FieldLookup_SiteCollectionName = "SiteCollectionName";
        }


        /// <summary>
        /// Contains the display name of the Site Collection Lookup list
        /// </summary>
        public const string CollectionSiteTypesLK = "CollectionSiteTypesLK";

        /// <summary>
        /// Contains the constant values for the List Fields
        /// </summary>
        public class CollectionSiteTypesLKFields : ConstantsListFields
        {
            public const string FieldText_TemplateName = "TemplateName";
            public const string FieldText_Name1 = "Name1";
            public const string FieldBoolean_ShowInDropDown = "ShowinDropDown";
            public const string FieldText_CollectionURL = "CollectionURL";
            public const string FieldLookup_SiteCollection1 = "SiteCollection1";
            public const string FieldLookup_SiteType = "SiteType";
        }


        /// <summary>
        /// Contains the display name for the list which contains offices and their acronyms
        /// </summary>
        public const string SiteCollectionsLK = "SiteCollectionsLK";

        /// <summary>
        /// Contains the constant list column field names
        /// </summary>
        public class SiteCollectionsLKFields : ConstantsListFields
        {
            public const string FieldInteger_sort = "sort";
            public const string FieldText_acronymn = "acronymn";
        }


        /// <summary>
        /// Contains the display name for the list which contains site types (Organization/Community/Work)
        /// </summary>
        public const string SiteTypesLK = "SiteTypesLK";

        /// <summary>
        /// Contains the constant list column field names
        /// </summary>
        public class SiteTypesLKFields : ConstantsListFields
        {
            public const string FieldText_Name1 = "Name1";
            public const string FieldBoolean_ShowInDropdown = "ShowInDropdown";
            public const string FieldText_TemplateName = "TemplateName";
            public const string FieldText_TemplateUri = "TemplateUri";
        }


        /// <summary>
        /// Missing Metadata
        /// </summary>
        public const string MissingMetadataListName = "missingmetadata";

        /// <summary>
        /// Contains the constant list column field names
        /// </summary>
        public class MissingMetadataFields : ConstantsListFields
        {
            public const string FieldText_Url = "Title";
            public const string FieldText_SiteTitle = "Title1";
            public const string FieldText_Region = "Region";
            public const string FieldText_Type1 = "Type1";
            public const string FieldText_HasMetaDataList = "Has_x0020_MetaData_x0020_List";
            public const string FieldInteger_NumberofItems = "Number_x0020_of_x0020_Items";
            public const string FieldMultiText_SiteOwners = "Site_x0020_Owners";
            public const string FieldText_EmailSentFlag = "EmailSentFlag";
            public const string FieldText_RequestCompletedFlag = "RequestCompletedFlag";
            public const string FieldText_SiteRequestID = "SiteRequestID";
            public const string FieldBoolean_SiteExists = "SiteNotFound";
            public const string FieldLookup_CollectionSiteType = "CollectionSiteType";
            public const string FieldText_WebGuid = "WebGuid";
        }


        public const string EmailerPlaceholderListName = "EmailerPlaceholder";

        /// <summary>
        /// Email Placeholder to relay email messages
        /// </summary>
        public class EmailerPlaceholderFields : ConstantsListFields
        {
            public const string FieldMultiText_EmailTo = "EmailTo";
            public const string FieldText_EmailCC = "EmailCC";
            public const string FieldText_EmailFrom = "EmailFrom";
            public const string FieldMultiText_Body = "Body";
        }


        public const string SCAListName = "SCA List";

        /// <summary>
        /// SCA List from separate site that is synchronized
        /// </summary>
        public class SCAListFields : ConstantsListFields
        {
            public const string FieldUserValue_EmployeeName = "Employee_x0020_Name";
            public const string FieldText_SCARole = "SCA_x0020_Role";
            public const string FieldText_RegionProgram = "Region_x0020_or_x0020_Program_x0";
        }
    }
}
