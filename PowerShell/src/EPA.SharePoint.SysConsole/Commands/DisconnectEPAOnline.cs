using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    /*
        Examples:
        DisconnectEPAOnline
    */
    [Verb("DisconnectEPAOnline", HelpText = "This will clear the SPOnlineConnection")]
    public class DisconnectEPAOnlineOptions : CommonOptions
    {
    }

    public static class DisconnectEPAOnlineExtension
    {
        public static int RunGenerateAndReturnExitCode(this DisconnectEPAOnlineOptions opts, IAppSettings appSettings)
        {
            var cmd = new DisconnectEPAOnline(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    public class DisconnectEPAOnline : BaseSpoCommand<DisconnectEPAOnlineOptions>
    {
        public DisconnectEPAOnline(DisconnectEPAOnlineOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
        }

        public override int OnRun()
        {
            if (!DisconnectCurrentService())
                throw new InvalidOperationException(Office365.CoreResources.NoConnectionToDisconnect);

            return 1;
        }
    }
}
