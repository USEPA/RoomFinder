namespace EPA.SharePoint.SysConsole.Models.EzForms
{
    /// <summary>
    /// Provides constants for choice fields and various filters
    /// </summary>
    public static class EzForms_Constants
    {
        public const string Supervisor_Director = "Supervisor/Director";

        public const string Local_Servicing_Organization = "Local Servicing Organization";

        public const string Local_Information_Security_Officer = "Local Information Security Officer";

        public const string Information_Security_Officer = "Information Security Officer";

        public const string Senior_Information_Officer = "Senior Information Officer";

        public const string Information_Management_Officer = "Information Management Officer";

        public const string Information_Resource_Management = "Information Resource Management";

        public const string ORD_IT_Operations_Manager = "ORD IT Operations Manager";

        public const string Business_Relationship_Manager = "Business Relationship Manager";

        public const string International_Travel_Representative = "International Travel Representative";

        public const string Approved = "Approved";

        public const string Denied = "Denied";

        public const string Pending = "Pending";

        public const string Recertification = "Recertification";

    }

    public static class EzForms_RequestStatus_Constants
    {
        public const string None = "None";
        public const string Approved = "Approved";
        public const string Denied = "Denied";
        public const string Pending = "Pending";
        public const string Recertification = "Recertification";
    }

    public static class EzForms_RequestType_Constants
    {
        public const string AD_Privileged_Account = "AD Privileged Account";
        public const string Share_Folder = "Share Folder";
        public const string Elevated_Privileges = "Elevated Privileges";
        public const string ORD_Share_Folder = "ORD Share Folder";
        public const string Exclusion_Group = "Exclusion Group";
        public const string Manual_Reboot_Power_Settings = "Manual Reboot-Power Settings";
        public const string Licensed_Software = "Licensed Software";
        public const string Freeware_Shareware = "Freeware-Shareware";
        public const string Employee_Emails_and_Files = "Employee Emails and Files";
        public const string Laptop_for_International_Travel = "Laptop for International Travel";
        public const string EPASS_Exception = "EPASS Exception";

    }

    public class EzForms_AccessRequest : ConstantsListFields
    {
        public const string ListName = "Access Requests";


        /// <summary>
        /// Activity LOG: Phase#|#Status#|#Person#|#Rerouted#|#TimeStamp--#|#-- 
        /// </summary>
        public const string Formatted_Log = "{0}#|#{1}#|#{2}#|#{3}#|#{4}#|#--#|#--";


        public const string Field_DataMigrated = "DataMigrated";
        public const string Field_Request_x0020_Type = "Request_x0020_Type";
        public const string Field_Request_x0020_Status = "Request_x0020_Status";
        public const string Field_Routing_x0020_Phase = "Routing_x0020_Phase";
        public const string Field_Previous_x0020_Routing_x0020_Pha = "Previous_x0020_Routing_x0020_Pha";
        public const string Field_Employee = "Employee";
        public const string Field_Office = "Office";
        public const string Field_Office_x0020_Acronym = "Office_x0020_Acronym";
        public const string Field_Countries = "Country";
        public const string Field_Computer_x0020_Name = "Computer_x0020_Name";
        public const string Field_Request_x0020_Date = "Request_x0020_Date";
        public const string Field_Activity_x0020_Log = "Activity_x0020_Log";
        public const string Field_Approvers = "Approvers";
        public const string Field_Justification = "Justification";
        public const string Field_Location_x0020__x002d__x0020_Cit = "Location_x0020__x002d__x0020_Cit";
        public const string Field_Building_x002C__x0020_Desk_x002F = "Building_x002C__x0020_Desk_x002F";
        public const string Field_Departure_x0020_Date = "Departure_x0020_Date";
        public const string Field_Description = "Description";
        public const string Field_Organization = "Organization";
        public const string Field_Resource = "Resource";
        public const string Field_Product_x0020_Key = "Product_x0020_Key";
        public const string Field_Software_x0020_Title = "Software_x0020_Title";
        public const string Field_ORD_x0020_Location = "ORD_x0020_Location";
        public const string Field_Location = "Location";
        public const string Field_Mailbox = "Mailbox";
        public const string Field_Travel_x0020_Reason = "Travel_x0020_Reason";
        public const string Field_Drive_x0020_letter = "Drive_x0020_letter";
        public const string Field_Permanent_x0020_Access_x0020_Req = "Permanent_x0020_Access_x0020_Req";
        public const string Field_Network_x0020_Storage_x0020_Type = "Network_x0020_Storage_x0020_Type";
        public const string Field_Approver_x0020_Comment = "Approver_x0020_Comment";
        public const string Field_Existing_x0020_Network_x0020_Sha = "Existing_x0020_Network_x0020_Sha";
        public const string Field_New_x0020_Network_x0020_Share = "New_x0020_Network_x0020_Share";
        public const string Field_Reason_x0020_for_x0020_Exclusion = "Reason_x0020_for_x0020_Exclusion";
        public const string Field_User_x0020_Access_x0020_List = "User_x0020_Access_x0020_List";
        public const string Field_Storage = "Storage";
        public const string Field_Applications = "Applications";

        public const string Field_Business_x0020_Relationship_x002 = "Business_x0020_Relationship_x002";
        public const string Field_Division_x0020_Director_x0020_or = "Division_x0020_Director_x0020_or";
        public const string Field_Information_x0020_Management_x00 = "Information_x0020_Management_x00";
        public const string Field_International_x0020_Travel_x0020 = "International_x0020_Travel_x0020";
        public const string Field_Local_x0020_Information_x0020_Se = "Local_x0020_Information_x0020_Se";
        public const string Field_Office_x0020_Information_x0020_S = "Office_x0020_Information_x0020_S";
        public const string Field_ORD_x0020_IT_x0020_Operations_x0 = "ORD_x0020_IT_x0020_Operations_x0";
        public const string Field_Senior_x0020_Information_x0020_S = "Senior_x0020_Information_x0020_S";
        public const string Field_ezformsIRMBC = "ezformsIRMBC";


        public const string Field_ezLanIdText = "ezLanIdText";
        public const string Field_ezFormsADAccount = "ezFormsADAccount";
        public const string Field_ezformsWorkforceID = "ezformsWorkforceID";
        public const string Field_ezFormsWorkforceNumber = "ezFormsWorkforceNumber";
        public const string Field_ezformsNextCertifyDate = "ezformsNextCertifyDate";
        public const string Field_ezUserRoleChoice = "ezUserRoleChoice";
        public const string Field_ezPersonnelBool = "ezPersonnelBool";
        public const string Field_ADContractNumber = "ezContractNumberText";
        public const string Field_ADEquipment = "ezFormsEquipment";
        public const string Field_ezformsADUserTermsDate = "ezformsADUserTermsDate";
        public const string Field_ezLocationSiteBool = "ezLocationSiteBool";

    }

    public class EzForms_AccessUserList : ConstantsListFields
    {
        public const string ListName = "Access User List";

        public const string Field_Office = "Office";
        public const string Field_Office_x0020_Acronym = "Office_x0020_Acronym";
        public const string Field_Approver = "Approver";
    }


    public class EzForms_UserRecertification : ConstantsListFields
    {
        public const string ListName = "User Recertification";

        public const string Field_ezItemID = "ezItemID";
        public const string Field_RowProcessed = "RowProcessed";
        public const string Field_ezRecertificationDate = "ezRecertificationDate";
        public const string Field_Routing_x0020_Phase = "Routing_x0020_Phase";

        public const string Field_ezEmployeeUser = "ezEmployeeUser";
        public const string Field_ezFormsRole = "ezFormsRole";
        public const string Field_ezFormsJustification = "ezFormsJustification";
        public const string Field_ezAccountCreatedDate = "ezAccountCreatedDate";
    }

    /// <summary>
    /// Contains the List Defintion for PrivInfo (ActiveDirectory) information
    /// </summary>
    public class EzForms_PrivADInfo : ConstantsListFields
    {
        public const string ListName = "PrivADInfo";

        public const string Field_SamAccountName = "SamAccountName";
        public const string FieldDate_LastSyncDate = "LastSyncDate";
        public const string Field_EmployeeID = "EmployeeID";
        public const string Field_ADObjectID = "ADObjectID";
        public const string Field_EmployeeNumber = "EmployeeNumber";
        public const string Field_EmailAddress = "EmailAddress";
        public const string Field_DisplayName = "DisplayName";
        public const string Field_GivenName = "GivenName";
        public const string Field_SurName = "SurName";
        public const string FieldDate_DottedAccountExpirationDate = "DottedAccountExpirationDate";
        public const string Field_DottedAccountName = "DottedAccountName";
        public const string Field_EmployeeManager = "EmployeeManager";
        public const string Field_DottedAccountSID = "DottedAccountSID";
        public const string Field_DottedProvisioningMessage = "DottedProvisioningMessage";
        public const string Field_DottedRequestID = "DottedRequestID";
    }


    /// <summary>
    /// Contains the List Defintion for PrivProcess
    /// </summary>
    public class EzForms_PrivProcess : ConstantsListFields
    {
        public const string ListName = "PrivProcess";

        public const string Field_ezItemID = "ezItemID";
        public const string FieldChoice_ezAction = "ezAction";
        public const string FieldBoolean_RowProcessed = "RowProcessed";
        public const string FieldMulti_RowMessage = "RowMessage";

        public const string Field_DottedSamAccountName = "DottedSamAccountName";
        public const string Field_DottedEmployeeNumber = "DottedEmployeeNumber";
        public const string FieldDate_DottedAccountCreated = "DottedAccountCreated";
        public const string Field_ShortAccount = "ShortAccount";
        public const string Field_LANEmployeeID = "LANEmployeeID";
        public const string FieldUser_Employee = "Employee";
        public const string FieldUser_EmployeeManager = "EmployeeManager";
        public const string FieldBoolean_RowError = "RowError";
    }


}
