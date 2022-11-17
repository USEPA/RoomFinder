using CommandLine;
using EPA.Office365;
using EPA.Office365.oAuth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb(name: "connectadalv2", HelpText = "Test connectivity to Azure AD via V2 config")]
    public class ConnectADALv2Options : CommonOptions
    {
        [Option("scopes", Required = true, HelpText = "The array of permission scopes for the Microsoft Graph API.", SetName = "Scope", Separator = ',')]
        public IEnumerable<string> Scopes { get; set; }

        [Option("msal-clientId", Required = true, HelpText = "The client id of the app which gives you access to the Microsoft Graph API.", SetName = "Scope")]
        public string MSALClientId { get; set; }

        [Option("app-id", Required = true, HelpText = "The client id of the app which gives you access to the Microsoft Graph API.", SetName = "AAD")]
        public string AppId { get; set; }

        [Option("app-secret", Required = true, HelpText = "The app key of the app which gives you access to the Microsoft Graph API.", SetName = "AAD")]
        public string AppSecret { get; set; }

        [Option("resource-uri", Required = false, HelpText = "The URI of the resource to query")]
        public string ResourceUri { get; set; }
    }

    public static class ConnectADALv2Extension
    {
        public static int RunGenerateAndReturnExitCode(this ConnectADALv2Options opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new ConnectADALv2(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Connects to Azure AD  to claim a token via the V2 endpoints
    /// </summary>
    public class ConnectADALv2 : BaseAdalCommand<ConnectADALv2Options>
    {
        public ConnectADALv2(ConnectADALv2Options opts, IAppSettings settings, Serilog.ILogger traceLogger)
            : base(opts, settings, traceLogger)
        {
        }

        public override void OnBeginInit()
        {
            var useInteractiveLogin = true;

            if (Opts?.Scopes.Any() == false)
            {
                useInteractiveLogin = false;
                Opts.Scopes = new string[] { Settings.Graph.DefaultScope };
            }

            Settings.AzureAd.ClientId = Opts.AppId;
            Settings.AzureAd.ClientSecret = Opts.AppSecret;
            Settings.AzureAd.PostLogoutRedirectURI = Opts.ResourceUri ?? ConstantsAuthentication.GraphResourceId;
            Settings.AzureAd.MSALClientID = Opts.MSALClientId;
            Settings.AzureAd.MSALScopes = Opts.Scopes.ToArray();

            // Get back the Access Token and the Refresh Token
            TokenCache = new AzureADv2TokenCache(Settings, TraceLogger, useInteractiveLogin);
        }

        public override int OnRun()
        {
            // Write Tokens to Console
            var token = TokenCache.AccessTokenAsync(string.Empty).GetAwaiter().GetResult();
            Console.WriteLine(token);
            return 1;
        }
    }
}
