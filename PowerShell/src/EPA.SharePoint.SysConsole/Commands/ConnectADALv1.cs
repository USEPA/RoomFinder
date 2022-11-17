using CommandLine;
using EPA.Office365;
using EPA.Office365.oAuth;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb(name: "connectadalv1", HelpText = "Test connectivity to Azure AD via V1 config")]
    public class ConnectADALv1Options : CommonOptions
    {
        [Option("app-id", Required = true, HelpText = "The client id of the app which gives you access to the Microsoft Graph API.", SetName = "AAD")]
        public string AppId { get; set; }

        [Option("app-secret", Required = true, HelpText = "The app key of the app which gives you access to the Microsoft Graph API.", SetName = "AAD")]
        public string AppSecret { get; set; }

        [Option("resource-uri", Required = false, HelpText = "The URI of the resource to query", SetName = "AAD")]
        public string ResourceUri { get; set; }
    }

    public static class ConnectADALv1Extension
    {
        public static int RunGenerateAndReturnExitCode(this ConnectADALv1Options opts, IAppSettings appSettings, Serilog.ILogger logger)
        {
            var cmd = new ConnectADALv1(opts, appSettings, logger);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Connects to Azure AD to retreive a token via the V1 endpoints
    /// </summary>
    public class ConnectADALv1 : BaseAdalCommand<ConnectADALv1Options>
    {
        public ConnectADALv1(ConnectADALv1Options opts, IAppSettings settings, Serilog.ILogger traceLogger)
            : base(opts, settings, traceLogger)
        {
        }

        public override void OnBeginInit()
        {
            Settings.AzureAd.ClientId = Opts.AppId;
            Settings.AzureAd.ClientSecret = Opts.AppSecret;
            Settings.AzureAd.PostLogoutRedirectURI = Opts.ResourceUri ?? ConstantsAuthentication.GraphResourceId;
            TokenCache = new AzureADv1TokenCache(Settings, TraceLogger);
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
