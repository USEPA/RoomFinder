using CommandLine;
using EPA.Office365;
using EPA.Office365.Database;
using EPA.Office365.Graph;
using EPA.Office365.oAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using System;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb(name: "getebusinessusers", HelpText = "Retreive Office 365 Users || Azure AD Users")]
    public class GetUsersCommandOptions : CommonOptions
    {
    }

    public static class GetUsersCommandExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetUsersCommandOptions opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new GetUsersCommand(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    public class GetUsersCommand : BaseAdalCommand<GetUsersCommandOptions>
    {
        private const int defaultRetryCount = 10;
        private const int defaultDelay = 500;
        private string AccessConnection { get; }
        private string Domain { get; }

        public GetUsersCommand(GetUsersCommandOptions opts, IAppSettings settings, Serilog.ILogger traceLogger)
            : base(opts, settings, traceLogger)
        {
            AccessConnection = Settings.ConnectionStrings.AnalyticsConnection;
            Domain = Settings.Commands.Domain;
        }

        public override void OnBeginInit()
        {
            var useInteractiveLogin = false;
            var scopes = new string[] { Settings.Graph.DefaultScope };

            Settings.AzureAd.ClientId = Settings.SpoADALepaReporting.ClientId;
            Settings.AzureAd.ClientSecret = Settings.SpoADALepaReporting.ClientSecret;
            Settings.AzureAd.PostLogoutRedirectURI = ConstantsAuthentication.GraphResourceId;
            Settings.AzureAd.MSALScopes = scopes.ToArray();

            TokenCache = new AzureADv2TokenCache(Settings, TraceLogger, useInteractiveLogin);
        }

        /// <summary>
        /// Process the Azure AD Users
        /// </summary>
        public override int OnRun()
        {
            var utility = new GraphUtility(TokenCache, TraceLogger);
            var client = utility.CreateGraphClient(defaultRetryCount, defaultDelay);

            try
            {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder<AnalyticDbContext>();
                dbContextOptionsBuilder.UseSqlServer(AccessConnection);
                using var _context = new AnalyticDbContext(dbContextOptionsBuilder.Options);
                // Purge the ETL table's prior import data
                var rowsDeleted = _context.Database.ExecuteSqlRaw("[dbo].[sp_del_etlEBusinessAccounts]");
                TraceLogger.Verbose("etl.eBusinessAccounts. {0} rows deleted.", rowsDeleted);
                // Only return users with OnPremisesSyncEnabled and AccountEnabled set to true
                var resultUsers = client.Users.Request().Filter("onPremisesSyncEnabled eq true and accountEnabled eq true").Select("userPrincipalName,accountEnabled,DisplayName,Id,onPremisesSyncEnabled");
                do
                {
                    var users = resultUsers.WithMaxRetry(5).GetAsync().GetAwaiter().GetResult();

                    foreach (var user in users)
                    {
                        TraceLogger.Verbose($"User found with {user.DisplayName} , {user.UserPrincipalName}, {user.OnPremisesSyncEnabled}, {user.Id}");
                        // Only wants users with the domain suffix within their Principal Name
                        if (user.UserPrincipalName.EndsWith(Domain))
                        {
                            var resultUser = client.Users[user.UserPrincipalName].Request().Select("Department,preferredName,givenName,surname,userPrincipalName," +
                                "accountEnabled,employeeId,DisplayName,mail,onPremisesUserPrincipalName,onPremisesSamAccountName,officeLocation,city,state,streetaddress," +
                                "postalcode,id,onPremisesSecurityIdentifier,onPremisesExtensionAttributes");
                            do
                            {
                                var userDetails = resultUser.WithMaxRetry(5).GetAsync().GetAwaiter().GetResult();
                                TraceLogger.Verbose($"User Details found with {userDetails.DisplayName} , {userDetails.OnPremisesSamAccountName}, " +
                                    $"{userDetails.UserPrincipalName}, {userDetails.Department}, {userDetails.GivenName}, {userDetails.Surname}, " +
                                    $"{userDetails.OnPremisesExtensionAttributes.ExtensionAttribute13}, {userDetails.Mail}, {userDetails.AccountEnabled}, {userDetails.OfficeLocation}, " +
                                    $"{userDetails.StreetAddress}, {userDetails.City}, {userDetails.State}, {userDetails.PostalCode}");
                                var model = new EntityEBusinessAccounts
                                {
                                    SamAccountName = userDetails.OnPremisesSamAccountName,
                                    OfficeName = userDetails.Department,
                                    FirstName = userDetails.GivenName,
                                    LastName = userDetails.Surname,
                                    AffiliationCode = userDetails.OnPremisesExtensionAttributes.ExtensionAttribute13,
                                    Email = userDetails.Mail,
                                    Enabled = userDetails.AccountEnabled,
                                    RoomNumber = userDetails.OfficeLocation,
                                    AddressLine1 = userDetails.StreetAddress,
                                    City = userDetails.City,
                                    State = userDetails.State,
                                    ZipCode = userDetails.PostalCode,
                                    DateImport = DateTime.Now
                                };
                                TraceLogger.Verbose("Now adding {0} with SamAccountName {1}", userDetails.DisplayName, userDetails.UserPrincipalName);
                                _context.EBusinessAccounts.Add(model);

                                resultUser = null;
                            } while (resultUser != null);
                        }
                    }

                    if (ShouldProcess("Saving Database data"))
                    {
                        var totalRowsCommitted = _context.SaveChanges();
                        TraceLogger.Verbose("Total Committed Rows {0}", totalRowsCommitted);
                    }

                    resultUsers = users.NextPageRequest;
                }
                while (resultUsers != null);
                // Process the users within the staging ETL table into dbo.eBusinessAccounts
                var rowsInserted = _context.Database.ExecuteSqlRaw("[dbo].[sp_ins_dboEBusinessAccounts]");
                TraceLogger.Verbose("dbo.eBusinessAccounts. {0} rows inserted.", rowsInserted);
            }
            catch (ServiceException gex)
            {
                TraceLogger.Error(gex, $"Failed with message {gex.Error.Message}");
                return 0;
            }
            return 1;
        }
    }
}
