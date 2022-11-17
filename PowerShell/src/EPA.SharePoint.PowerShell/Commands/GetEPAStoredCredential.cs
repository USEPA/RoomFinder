using System.Management.Automation;

namespace EPA.SharePoint.PowerShell.Commands
{
    /*
    Example:
    Returns the credential associated with the specified identifier
    Get-SPOnlineStoredCredential -Name O365
    */
    [Cmdlet("Get", "EPAStoredCredential")]
    public class GetEPAStoredCredential : ExtendedPSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "The credential to retrieve.")]
        public string Name { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "The object type of the credential to return from the Credential Manager. Possible valus are 'O365', 'OnPrem' or 'PSCredential'")]
        public CredentialType Type { get; set; }


        protected override void ProcessRecord()
        {
            switch (Type)
            {
                case CredentialType.OnPrem:
                    {
                        var onpremcreds = CredentialManager.GetCredential(Name);
                        WriteObject(onpremcreds);
                        break;
                    }
                default:
                    {
                        var networkcreds = CredentialManager.GetCredential(Name);
                        WriteObject(networkcreds.GetPSCredentials());
                        break;
                    }
            }
        }
    }
}
