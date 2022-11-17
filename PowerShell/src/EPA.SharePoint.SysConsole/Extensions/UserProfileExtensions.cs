using EPA.Office365;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Reporting;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;

namespace EPA.SharePoint.SysConsole.Extensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "csom unknown processing error at COM level")]
    public static class UserProfileExtensions
    {
        /// <summary>
        /// Retreive all of the OneDrive for Business profiles
        /// </summary>
        /// <param name="adminSiteContext"></param>
        /// <param name="traceLogger"></param>
        /// <param name="MySiteUrl"></param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        public static List<OD4BProfileModel> GetOneDriveProfiles(this ClientContext adminSiteContext, ITraceLogger traceLogger, string MySiteUrl, bool includeProperties = false)
        {
            var results = new List<OD4BProfileModel>();
            MySiteUrl = MySiteUrl.EnsureTrailingSlashLowered();
            MySiteUrl = MySiteUrl.Substring(0, MySiteUrl.Length - 1);

            using var _UserProfileService = new UserProfileService(adminSiteContext, adminSiteContext.Url);

            var userProfileResult = _UserProfileService.OWService.GetUserProfileByIndex(-1);
            var userProfilesCount = _UserProfileService.OWService.GetUserProfileCount();
            var rowIndex = 1;

            // As long as the next User profile is NOT the one we started with (at -1)...
            while (int.TryParse(userProfileResult.NextValue, out int nextValueIndex) && nextValueIndex != -1)
            {
                if ((rowIndex % 50) == 0 || rowIndex > userProfilesCount)
                {
                    traceLogger.LogInformation($"Next set {rowIndex} of {userProfilesCount}");
                }

                try
                {
                    var personalSpace = userProfileResult.RetrieveUserProperty("PersonalSpace");
                    var personalSpaceUrl = (string.IsNullOrEmpty(personalSpace) ? string.Empty : $"{MySiteUrl}{personalSpace}");
                    var hasPersonalSpace = (!string.IsNullOrEmpty(personalSpace));

                    var model = new OD4BProfileModel();
                    var properties = userProfileResult.UserProfile;

                    if (includeProperties == false)
                    {
                        model = new OD4BProfileModel
                        {
                            PersonalSpaceProperty = personalSpace,
                            Url = personalSpaceUrl,
                            HasProfile = hasPersonalSpace,
                            NameProperty = properties.RetrieveUserProperty("PreferredName"),
                            UserName = properties.RetrieveUserProperty("UserName"),
                            Title = properties.RetrieveUserProperty("Title")
                        };
                    }
                    else
                    {
                        model = new OD4BProfileModel
                        {
                            PersonalSpaceProperty = personalSpace,
                            Url = personalSpaceUrl,
                            HasProfile = hasPersonalSpace,
                            NameProperty = properties.RetrieveUserProperty("PreferredName"),
                            UserName = properties.RetrieveUserProperty("UserName"),
                            PictureUrl = properties.RetrieveUserProperty("PictureURL"),
                            AboutMe = properties.RetrieveUserProperty("AboutMe"),
                            SpsSkills = properties.RetrieveUserProperty("SPS-Skills"),
                            Manager = properties.RetrieveUserProperty("Manager"),
                            WorkPhone = properties.RetrieveUserProperty("WorkPhone"),
                            Department = properties.RetrieveUserProperty("Department"),
                            Company = properties.RetrieveUserProperty("Company"),
                            AccountName = properties.RetrieveUserProperty("AccountName"),
                            DistinguishedName = properties.RetrieveUserProperty("SPS-DistinguishedName"),
                            FirstName = properties.RetrieveUserProperty("FirstName"),
                            LastName = properties.RetrieveUserProperty("LastName"),
                            UserPrincipalName = properties.RetrieveUserProperty("SPS-UserPrincipalName"),
                            Title = properties.RetrieveUserProperty("Title"),
                            WorkEmail = properties.RetrieveUserProperty("WorkEmail"),
                            HomePhone = properties.RetrieveUserProperty("HomePhone"),
                            CellPhone = properties.RetrieveUserProperty("CellPhone"),
                            Office = properties.RetrieveUserProperty("Office"),
                            Location = properties.RetrieveUserProperty("SPS-Location"),
                            Fax = properties.RetrieveUserProperty("Fax"),
                            MailingAddress = properties.RetrieveUserProperty("MailingAddress"),
                            EPAMailingZipCode = properties.RetrieveUserProperty("EPA-MailingZipCode"),
                            School = properties.RetrieveUserProperty("SPS-School"),
                            WebSite = properties.RetrieveUserProperty("WebSite"),
                            Education = properties.RetrieveUserProperty("Education"),
                            JobTitle = properties.RetrieveUserProperty("SPS-JobTitle"),
                            Assistant = properties.RetrieveUserProperty("Assistant"),
                            HireDate = properties.RetrieveUserProperty("SPS-HireDate"),
                            TimeZone = properties.RetrieveUserProperty("SPS-TimeZone"),
                            Locale = properties.RetrieveUserProperty("SPS-Locale"),
                            EmailOptin = properties.RetrieveUserProperty("SPS-EmailOptin"),
                            PrivacyPeople = properties.RetrieveUserProperty("SPS-PrivacyPeople"),
                            PrivacyActivity = properties.RetrieveUserProperty("SPS-PrivacyActivity"),
                            MySiteUpgrade = properties.RetrieveUserProperty("SPS-MySiteUpgrade"),
                            ProxyAddresses = properties.RetrieveUserProperty("SPS-ProxyAddresses"),
                            OWAUrl = properties.RetrieveUserProperty("SPS-OWAUrl")
                        };
                    }
                    results.Add(model);

                    userProfileResult = _UserProfileService.OWService.GetUserProfileByIndex(int.Parse(userProfileResult.NextValue));
                    rowIndex++;
                }
                catch (Exception e)
                {
                    traceLogger.LogWarning("Failed to execute while loop {0}", e.Message);
                }
            }

            // Final processing
            traceLogger.LogWarning($"Total Profiles {rowIndex} processed...");

            return results;
        }
    }
}
