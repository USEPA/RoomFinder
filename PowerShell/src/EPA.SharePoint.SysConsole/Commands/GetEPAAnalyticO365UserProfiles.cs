using CommandLine;
using CsvHelper;
using EPA.Office365.Database;
using EPA.Office365.Extensions;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("getEPAAnalyticO365UserProfiles", HelpText = "scan mysites for user profiles.")]
    public class GetEPAAnalyticO365UserProfilesOptions : TenantCommandOptions
    {
        [Option("log-directory", Required = true)]
        public string LogDirectory { get; set; }
    }

    public static class GetEPAAnalyticO365UserProfilesExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAAnalyticO365UserProfilesOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAAnalyticO365UserProfiles(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the tenant to discover O365 User Profiles
    /// </summary>
    /// <remarks>
    /// *************************************************************************
    /// ************************ O365 User Profiles, OneDrive, Storage, Followers, Personal Sites ***********************
    /// *************************************************************************
    /// </remarks>
    public class GetEPAAnalyticO365UserProfiles : BaseSpoTenantCommand<GetEPAAnalyticO365UserProfilesOptions>
    {
        public GetEPAAnalyticO365UserProfiles(GetEPAAnalyticO365UserProfilesOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        internal string TenantAdminUrl { get; set; }
        internal List<dynamic> UserProfiles { get; private set; }

        #endregion

        public override void OnInit()
        {
            TenantAdminUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantAdminUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();
            if (!Directory.Exists(Opts.LogDirectory))
            {
                throw new DirectoryNotFoundException($"Directory {Opts.LogDirectory} not found.");
            }
        }

        /// <summary>
        /// Process O365 User Profiles
        /// </summary>
        public override int OnRun()
        {
            TenantContext.EnsureProperties(tssp => tssp.RootSiteUrl);
            var TenantUrl = TenantContext.RootSiteUrl.EnsureTrailingSlashLowered();
            var MySiteTenantUrl = TenantUrl.Replace(".sharepoint.com", "-my.sharepoint.com");
            MySiteTenantUrl = MySiteTenantUrl.Substring(0, MySiteTenantUrl.Length - 1);


            var profiles = new List<OD4BProfileModel>()
            {
                new OD4BProfileModel()
                {
                    PersonalSpaceProperty = "/personal/leonard_shawn_epa_gov",
                    Url = $"{MySiteTenantUrl}/personal/leonard_shawn_epa_gov",
                    HasProfile = true,
                    NameProperty = "leonard, shawn",
                    UserName = "leonard.shawn@epa.gov",
                    Title = "Awesome Developers"
                }
            };


            var webUri = new Uri(this.ClientContext.Url);


            var connectionstring = Settings.ConnectionStrings.AnalyticsConnection;
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(connectionstring);
            using (var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options))
            {

                try
                {
                    // enumerate OD4B profiles and save to the database
                    foreach (var odprofile in profiles)
                    {
                        try
                        {
                            var _userName = odprofile.UserName;
                            var _personalSpace = odprofile.PersonalSpaceProperty;
                            var _odfbSite = odprofile.Url;
                            var _odfb = odprofile.HasProfile;

                            var department = odprofile.Department;
                            var _office = false;
                            var _officep1 = string.Empty;
                            var _officep2 = string.Empty;
                            var _officep3 = string.Empty;
                            var _officep4 = string.Empty;
                            var company = odprofile.Company;
                            var _isExternalUser = IsExternalUserFilter(_userName);


                            var pictureUrl = odprofile.PictureUrl;
                            var _profilePic = !string.IsNullOrEmpty(pictureUrl);
                            var aboutMe = odprofile.AboutMe;
                            var _aboutMe = !string.IsNullOrEmpty(aboutMe);
                            var spsSkills = odprofile.SpsSkills;
                            var _Skills = !string.IsNullOrEmpty(spsSkills);
                            var manager = odprofile.Manager;
                            var _Manager = !string.IsNullOrEmpty(manager);
                            var mailingZipCode = odprofile.EPAMailingZipCode;
                            var _MailingZipCode = !string.IsNullOrEmpty(mailingZipCode);
                            var workPhone = odprofile.WorkPhone;
                            var _WorkPhone = !string.IsNullOrEmpty(workPhone);


                            if (!string.IsNullOrEmpty(department))
                            {
                                _office = true;
                                var _officesplit = department.Split(new char[] { '-' }, StringSplitOptions.None);
                                if (_officesplit.Length > 0)
                                {
                                    _officep1 = (_officesplit.Length > 0) ? _officesplit[0] : string.Empty;
                                    _officep2 = (_officesplit.Length > 1) ? _officesplit[1] : string.Empty;
                                    _officep3 = (_officesplit.Length > 2) ? _officesplit[2] : string.Empty;
                                    _officep4 = (_officesplit.Length > 3) ? _officesplit[3] : string.Empty;
                                }
                                else
                                {
                                    _officep1 = department;
                                }
                            }


                            EntityAnalyticsUserProfiles profile = null;
                            if (_context.EntitiesUserProfile.Any(up => up.Username == _userName))
                            {
                                profile = _context.EntitiesUserProfile.FirstOrDefault(f => f.Username == _userName);
                            }
                            else
                            {
                                profile = new EntityAnalyticsUserProfiles()
                                {
                                    Username = _userName
                                };
                                _context.EntitiesUserProfile.Add(profile);
                            }



                            double _storageUsageGB = 0;
                            double _storageAllocatedGB = 0;
                            double _storagePercentUsed = 0;
                            long _storageHits = 0;
                            long _storageVisits = 0;
                            long _totalFiles = 0;
                            if (_odfb)
                            {
                                try
                                {
                                    SetSiteAdmin(_odfbSite, CurrentUserName, true);

                                    using (var ctx = TenantContext.Context.Clone(_odfbSite))
                                    {
                                        // ---> Site Usage Properties
                                        ctx.Site.EnsureProperties(cts => cts.Usage, cts => cts.RootWeb);

                                        try
                                        {
                                            var templateId = (int)ListTemplateType.MySiteDocumentLibrary;
                                            var onedrivelibrary = ctx.LoadQuery( ctx.Site.RootWeb.Lists.Include(fn => fn.Id, fn => fn.ItemCount, fn => fn.BaseTemplate).Where(w => w.BaseTemplate == templateId));
                                            ctx.ExecuteQueryRetry();

                                            _totalFiles = onedrivelibrary.Any() ? onedrivelibrary.Sum(s => s.ItemCount) : 0;
                                            LogVerbose("Opening Shared library {0}", _totalFiles);
                                        }
                                        catch (Exception ex)
                                        {
                                            LogWarning("Failed to retrieve Shared Documents {0}", ex.Message);
                                        }

                                        UsageInfo _usageInfo = ctx.Site.Usage;
                                        _storageHits = _usageInfo.Hits;
                                        _storageVisits = _usageInfo.Visits;
                                        _storagePercentUsed = _usageInfo.StoragePercentageUsed;
                                        Double _storageBytes = _usageInfo.Storage;
                                        var _storageMB = (_storageBytes / 1048576);
                                        Double _storageQuotaBytes = _usageInfo.Storage / _storagePercentUsed;
                                        var _storageQuota = (_storageQuotaBytes / 1048576);

                                        _storageUsageGB = Math.Round((_storageMB / 1024), 2);
                                        _storageAllocatedGB = Math.Round((_storageQuota / 1024), 2);
                                    }

                                    SetSiteAdmin(_odfbSite, CurrentUserName, false);
                                }
                                catch (Exception uex)
                                {
                                    LogWarning($"Failed to retrieve ODFB {_odfbSite} site collection statistics {uex.Message}");
                                }
                            }

                            profile.AboutMe = _aboutMe;
                            profile.Manager = _Manager;
                            profile.ODFBSite = _odfbSite;
                            profile.ODFBSiteProvisioned = _odfb;
                            profile.ODFBSiteHits = _storageHits;
                            profile.ODFBSiteVisits = _storageVisits;
                            profile.Office = _office;
                            profile.OfficeName = department;
                            profile.OfficeP1 = _officep1;
                            profile.OfficeP2 = _officep2;
                            profile.OfficeP3 = _officep3;
                            profile.OfficeP4 = _officep4;
                            profile.ProfilePicture = _profilePic;
                            profile.Skills = _Skills;
                            profile.WorkPhone = _WorkPhone;
                            profile.ZipCode = _MailingZipCode;
                            profile.ExternalUserFlag = _isExternalUser;
                            profile.TotalFiles = _totalFiles;
                            profile.StorageAllocated = _storageAllocatedGB.TryParseDecimal(0);
                            profile.StorageUsed = _storageUsageGB.TryParseDecimal(0);
                            profile.StorageUsedPerct = _storagePercentUsed.TryParseDecimal(0);
                            profile.DTUPD = DateTime.UtcNow;


                            var rowsProcessed = _context.SaveChanges();
                            LogWarning("Rows {0} processed...", rowsProcessed);
                        }
                        catch (Exception e)
                        {
                            LogWarning("Failed to execute while loop {0}", e.Message);
                        }
                    }
                }
                catch (Exception e)
                {
                    LogWarning("Failed to execute user profile service call {0}", e.Message);
                }


                UserProfiles = new List<dynamic>();
                foreach (var usp in _context.EntitiesUserProfile.AsQueryable().Where(w => !w.ExternalUserFlag))
                {
                    UserProfiles.Add(new
                    {
                        usp.Username,
                        usp.ODFBSite,
                        usp.WorkPhone,
                        usp.ZipCode,
                        usp.Manager,
                        usp.Office,
                        usp.ProfilePicture,
                        usp.AboutMe,
                        usp.Skills,
                        usp.ExternalUserFlag
                    });
                };
            }


            // Completed
            LogVerbose($"Total user profiles lines: {UserProfiles.Count()} at {DateTime.UtcNow} ................. ");

            // emit objects to disk
            if (ShouldProcess("Writing file to disc"))
            {
                var csvPath = $"{Opts.LogDirectory}\\od4b-profiles-{DateTime.UtcNow:MM-yyyy}.txt";
                using TextWriter writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
                var csv = new CsvWriter(writer, System.Globalization.CultureInfo.CurrentCulture);
                csv.WriteRecords(UserProfiles);
            }

            return 1;
        }

    }
}