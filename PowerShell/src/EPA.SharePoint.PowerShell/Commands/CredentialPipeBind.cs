using System.Management.Automation;

namespace EPA.SharePoint.PowerShell.Commands
{
    public sealed class CredentialPipeBind
    {
        private readonly PSCredential _pscredential;
        private readonly string _storedcredential;

        public CredentialPipeBind(PSCredential pscredential)
        {
            _pscredential = pscredential;
        }

        public CredentialPipeBind(string id)
        {
            _storedcredential = id;
        }

        public PSCredential Credential
        {
            get
            {
                if (_pscredential != null)
                {
                    return _pscredential;
                }
                else if (_storedcredential != null)
                {
                    var storedcreds = CredentialManager.GetCredential(_storedcredential);
                    return storedcreds.GetPSCredentials();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
