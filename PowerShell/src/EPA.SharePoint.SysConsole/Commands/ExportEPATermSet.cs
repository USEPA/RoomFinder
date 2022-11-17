using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using Serilog;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("exportEPATermSet", HelpText = "The function cmdlet will export the term set to csv format.")]
    public class ExportEPATermSetOptions : TenantCommandOptions
    {
        /// <summary>
        /// The site to search and query Term Sets
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }
    }

    public static class ExportEPATermSetExtension
    {
        public static int RunGenerateAndReturnExitCode(this ExportEPATermSetOptions opts, IAppSettings appSettings)
        {
            var cmd = new ExportEPATermSet(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// The function cmdlet will export the term set to csv format
    /// </summary>
    public class ExportEPATermSet : BaseSpoTenantCommand<ExportEPATermSetOptions>
    {
        public ExportEPATermSet(ExportEPATermSetOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var TenantUrl = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(TenantUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override void OnBeforeRun()
        {
            base.OnBeforeRun();
        }

        public override int OnRun()
        {

            using var siteContext = this.ClientContext.Clone(Opts.SiteUrl);

            // Get access to taxonomy CSOM.
            var taxonomySession = TaxonomySession.GetTaxonomySession(siteContext);
            siteContext.Load(taxonomySession);
            siteContext.ExecuteQueryRetry();

            // Retrieve Term Stores
            var termStores = taxonomySession.TermStores;
            siteContext.Load(termStores);
            siteContext.ExecuteQuery();

            // Bind to Term Store
            var termstore = termStores[0];
            siteContext.Load(termstore);
            siteContext.ExecuteQuery();

            // Retrieve Groups
            var termstoreGroups = termstore.Groups;
            siteContext.Load(termstoreGroups);
            siteContext.ExecuteQuery();

            // Retrieve TermSets in each group
            foreach (var termGroup in termstoreGroups)
            {
                siteContext.Load(termGroup);
                siteContext.ExecuteQuery();
                LogVerbose($"Group Name: {termGroup.Name}");
                var termSets = termGroup.TermSets;
                siteContext.Load(termSets);
                siteContext.ExecuteQuery();

                foreach (var TermSet in termSets)
                {
                    LogVerbose($"Termset: {TermSet.Name} ====>");
                    if (TermSet.Name == "Department")
                    {
                        LogVerbose("\t\tTerms:");
                        var Terms = TermSet.Terms;
                        siteContext.Load(Terms);
                        siteContext.ExecuteQuery();
                        foreach (var Term in Terms)
                        {
                            LogVerbose($" == > {Term.Name}");
                        }

                    }

                }
            }
            return 1;
        }
    }
}
