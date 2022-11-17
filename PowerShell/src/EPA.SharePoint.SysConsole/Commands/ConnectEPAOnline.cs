using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.PipeBinds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("ConnectEPAOnline", HelpText = "This will use credentials from the Windows Credential Manager, as defined by the label 'O365Creds'.")]
    public class ConnectEPAOnlineOptions : CommonOptions
    {
        [Option("url", Required = true, SetName = "AllParameterSets", HelpText = "The Url of the site collection to connect to.")]
        public string Url { get; set; }

        [Option("credentials", Required = false, SetName = "Main", HelpText = "Credentials of the user to connect with. Either specify a PSCredential object or a string. In case of a string value a lookup will be done to the Windows Credential Manager for the correct credentials.")]
        public CredentialPipeBind Credentials { get; set; }

        [Option("current-credentials", Required = false, SetName = "Main", HelpText = "If you want to connect with the current user credentials")]
        public bool CurrentCredentials { get; set; }

        [Option("realm", Required = false, SetName = "Token")]
        public string Realm { get; set; }

        [Option("app-id", Required = true, SetName = "Token")]
        public string AppId { get; set; }

        [Option("app-secret", Required = true, SetName = "Token")]
        public string AppSecret { get; set; }

        /// <summary>
        /// Represents a parameter to pull SharePoint App Details from the stored credentials
        /// </summary>
        [Option("app-credentials", Required = true, SetName = "AppCredentials")]
        public CredentialPipeBind AppCredentials { get; set; }

        /// <summary>
        /// Remove the need to check if this is a tenant client context
        /// </summary>
        [Option("skip-tenantcheck", Required = false, SetName = "AllParameterSets")]
        public bool SkipTenantAdminCheck { get; set; }
    }

    public static class ConnectEPAOnlineExtension
    {
        public static int RunGenerateAndReturnExitCode(this ConnectEPAOnlineOptions opts, IAppSettings appSettings)
        {
            var cmd = new ConnectEPAOnline(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// This will use credentials from the Windows Credential Manager, as defined by the label 'O365Creds'
    /// ConnectEPAOnline -Url http://yourlocalserver -Credentials 'O365Creds'
    /// </summary>
    public class ConnectEPAOnline : BaseSpoCommand<ConnectEPAOnlineOptions>
    {
        public ConnectEPAOnline(ConnectEPAOnlineOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
        }

        public override int OnRun()
        {
            NetworkCredential creds = null;
            if (Opts.Credentials != null)
            {
                creds = Opts.Credentials.Credential;
            }


            if (!string.IsNullOrEmpty(Opts.AppId) && !string.IsNullOrEmpty(Opts.AppSecret))
            {
                LogVerbose($"App Id/Secret connecting and setting Add-In {Opts.AppId} keys");
                SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.Url), Opts.Realm, Opts.AppId, Opts.AppSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout, Opts.SkipTenantAdminCheck);
            }
            else if (Opts.AppCredentials != null)
            {
                LogVerbose($"AppCredential connecting and setting Add-In keys");
                SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.Url), Opts.AppCredentials, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
            }
            else
            {
                if (!Opts.CurrentCredentials && creds == null)
                {
                    var username = ReadLine($"{Office365.CoreResources.EnterYourCredentials} username");
                    var password = ReadPassword($"{Office365.CoreResources.EnterYourCredentials} password");
                    creds = new NetworkCredential(username, password);
                }

                LogVerbose($"Received credentials for {creds?.UserName} user");
                if (Guid.TryParse(creds?.UserName, out _))
                {
                    // lets assume the username is a AppId/AppKey from the credential store
                    var initializedConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.Url), Opts.Credentials, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout, Opts.SkipTenantAdminCheck);
                    SPOnlineConnection.CurrentConnection = initializedConnection ?? throw new Exception($"Error establishing a connection to {Opts.Url} assuming AppId/AppKey combo.  Check the diagnostic logs.");
                }
                else
                {
                    var initializedConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.Url), creds, Opts.CurrentCredentials, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout, Opts.SkipTenantAdminCheck);
                    SPOnlineConnection.CurrentConnection = initializedConnection ?? throw new Exception(string.Format("Error establishing a connection to {0}.  Check the diagnostic logs.", Opts.Url));
                }
                LogVerbose("Processed credentials for {0} user", creds.UserName);

            }

            return 1;
        }

        public bool IsValid { get; private set; }
        private string ReadLine(string label, bool trim = true)
        {
            Console.WriteLine($"{label}:");
            var input = Console.ReadLine();
            Console.WriteLine();
            if (trim) input = input?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                IsValid = false;
                Console.WriteLine($"{label} is required, exiting.");
            }

            return input;
        }

        public static string ReadPassword(string label, char mask = '*')
        {
            const int enter = 13, backsp = 8, ctrlbacksp = 127;
            int[] filtered = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const
            var pass = new Stack<char>();
            char chr;
            Console.WriteLine($"{label}:");

            while ((chr = Console.ReadKey(true).KeyChar) != enter)
            {
                if (chr == backsp)
                {
                    if (pass.Count > 0)
                    {
                        Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (chr == ctrlbacksp)
                {
                    while (pass.Count > 0)
                    {
                        Console.Write("\b \b");
                        pass.Pop();
                    }
                }
                else if (filtered.Count(x => chr == x) > 0) { }
                else
                {
                    pass.Push(chr);
                    Console.Write(mask);
                }
            }

            Console.WriteLine();
            Console.WriteLine();
            return new string(pass.Reverse().ToArray());
        }

    }
}
