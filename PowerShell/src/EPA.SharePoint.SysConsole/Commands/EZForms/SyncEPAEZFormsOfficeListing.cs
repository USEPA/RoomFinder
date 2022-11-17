using CommandLine;
using EPA.Office365.Database;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("syncEPAEZFormsOfficeListing", HelpText = "Will sync epa office listing in database with CSV file.")]
    public class SyncEPAEZFormsOfficeListingOptions : CommonOptions
    {
        /// <summary>
        /// Includes the destination directory for the files
        /// </summary>
        [Option("log-directory", Required = true, HelpText = "Includes the destination directory for the files.")]
        public string LogDirectory { get; set; }

        /// <summary>
        /// Site Action for temporary actions
        /// </summary>
        [Option("site-action", Required = true, Default = EzformSiteAction.BuildOfficeList)]
        public EzformSiteAction SiteAction { get; set; }

        /// <summary>
        /// Represents an im
        /// </summary>
        [Option("offices", Required = false, HelpText = "The collection of offices to be updated")]
        public IEnumerable<EZFormsDepartments> Offices { get; set; }
    }

    public enum EzformSiteAction
    {
        BuildOfficeList,
        PopulateOffice
    }

    public static class SyncEPAEZFormsOfficeListingOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this SyncEPAEZFormsOfficeListingOptions opts, IAppSettings appSettings)
        {
            var cmd = new SyncEPAEZFormsOfficeListing(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will sync and update Office Listsings
    /// </summary>
    public class SyncEPAEZFormsOfficeListing : BaseEZFormsRecertification<SyncEPAEZFormsOfficeListingOptions>
    {
        public SyncEPAEZFormsOfficeListing(SyncEPAEZFormsOfficeListingOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        #region Private variables

        private string _accessConnection;

        #endregion

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();

            if (!System.IO.Directory.Exists(Opts.LogDirectory))
            {
                throw new System.IO.DirectoryNotFoundException($"{Opts.LogDirectory} could not be found.");
            }
        }

        public override int OnRun()
        {
            _accessConnection = Settings.ConnectionStrings.AnalyticsConnection;


            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
            dbContextOptionsBuilder.UseSqlServer(_accessConnection);
            using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);
            if (Opts.SiteAction == EzformSiteAction.BuildOfficeList)
            {
                GetOfficeList(_context);
            }
            else if (Opts.SiteAction == EzformSiteAction.PopulateOffice)
            {
                PopulateOfficeList(_context);
            }

            return 1;
        }


        public void GetOfficeList(AnalyticDbContext accessDb)
        {
            // process offices and set variable details
            var listOfShips = new List<string>();
            var listOfOffices = new List<EzFormsOffice>();
            var regex = new System.Text.RegularExpressions.Regex(@"\bR\d+\-");

            var dbOffices = accessDb.OfficeEntities.AsQueryable().Where(w => w.CurrentRow).ToList();
            try
            {
                foreach (var spListItem in dbOffices)
                {
                    var orgName = spListItem.OrgName;
                    var reportsTo = spListItem.ReportsTo;
                    bool? isord = default;
                    bool? region = default;

                    try
                    {
                        var orgNameClean = orgName.Trim();
                        if (regex.IsMatch(orgName) || orgName.IndexOf("@R1") > -1)
                        {
                            region = true;
                            orgNameClean = orgNameClean.Replace("REG-0", "R0").Replace("REG-", "R");
                        }


                        var orgComma = orgNameClean.IndexOf('-'); // has a dash or is a lower level
                        orgNameClean = orgNameClean.Replace('-', ','); // format to the AD Department String

                        if ((!string.IsNullOrEmpty(reportsTo) && reportsTo.Equals("0")) || spListItem.ChangeFromRow == 1)
                        {
                            listOfShips.Add(orgNameClean);
                        }
                        else
                        {
                            var aaShip = orgNameClean.Substring(0, orgComma);
                            if (orgNameClean.IndexOf(@"OAR,OTAQ", StringComparison.CurrentCultureIgnoreCase) > -1
                                || orgNameClean.IndexOf(@"ORD", StringComparison.CurrentCultureIgnoreCase) > -1)
                            {
                                isord = true;
                            }

                            if (region.HasValue && region.Value && !listOfShips.Any(ls => ls.Equals(aaShip, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                // Provide signular Region notation as a valued ship
                                listOfShips.Add(aaShip.Replace("R0", "R"));
                            }

                            listOfOffices.Add(new EzFormsOffice()
                            {
                                aaship = aaShip,
                                label = orgNameClean,
                                isord = isord,
                                region = region
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Data parsing error {0}", ex.Message);
                    }
                }

                var sortedarray = listOfOffices.Distinct(new EzFormsOfficeComparer()).OrderBy(s => s.label);
                var model = new EzFormsTopology()
                {
                    TopLevel = listOfShips,
                    Offices = sortedarray.ToList()
                };

                if (ShouldProcess("Writing file to disc"))
                {
                    var jsonPath = string.Format("{0}\\LookupOfficeListing.txt", Opts.LogDirectory);
                    var officeListJson = JsonConvert.SerializeObject(model, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        MaxDepth = 5,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    System.IO.File.WriteAllText(jsonPath, officeListJson);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Error {0}", ex.Message);
            }
        }

        public void PopulateOfficeList(AnalyticDbContext accessDb)
        {
            var currentOffices = accessDb.OfficeEntities.ToList();
            currentOffices.ForEach(f => { f.CurrentRow = false; });

            /*
             * 
             * 
Title = OrgName
OrgName = Department
AlphaCode=DepartmentNumber
ReportsTo = ParentOrgCode
ChangeFromRow = OrgLevel
             * */

            var officesInCsv = currentOffices.Where(co => Opts.Offices.Any(coo => co.AlphaCode == coo.DepartmentNumber));
            foreach (var office in officesInCsv)
            {
                var updCode = Opts.Offices.FirstOrDefault(f => f.DepartmentNumber == office.AlphaCode);

                LogVerbose("Now updating orgcode {0} to department {1}", office.OrgName, updCode.Department);
                office.CurrentRow = true;
                office.ReportsTo = updCode.ParentOrgCode;
                office.OrgName = updCode.Department;
                office.Title = updCode.OrgName;
                office.ChangeFromRow = updCode.OrgLevel;
            }

            var officesNoInCol = Opts.Offices.Where(oo => !currentOffices.Any(coo => coo.AlphaCode == oo.DepartmentNumber)).ToList();
            foreach (var newCode in officesNoInCol)
            {
                var model = new EntityOffice()
                {
                    AlphaCode = newCode.DepartmentNumber,
                    OrgName = newCode.Department,
                    Title = newCode.OrgName,
                    ReportsTo = newCode.ParentOrgCode,
                    ChangeFromRow = newCode.OrgLevel,
                    CurrentRow = true
                };
                LogVerbose("Now adding orgcode {0} with department {1}", newCode.DepartmentNumber, newCode.Department);
                accessDb.OfficeEntities.Add(model);
            }

            if (ShouldProcess("Saving Database data"))
            {
                var totalRowsCommitted = accessDb.SaveChanges();
                LogVerbose("Total Committed Rows {0}", totalRowsCommitted);
            }
        }
    }
}