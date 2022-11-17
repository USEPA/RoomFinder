using OfficeDevPnP.Core.Utilities;
using System.Net;

namespace EPA.SharePoint.SysConsole.PipeBinds
{
    public sealed class CredentialPipeBind
    {
        private NetworkCredential Pscredential { get; set; }
        private string Storedcredential { get; }

        public CredentialPipeBind(NetworkCredential pscredential)
        {
            Pscredential = pscredential;
        }

        public CredentialPipeBind(string id)
        {
            Storedcredential = id;
        }

        public NetworkCredential Credential
        {
            get
            {
                if (Pscredential != null)
                {
                    return Pscredential;
                }
                else if (Storedcredential != null)
                {
                    Pscredential = CredentialManager.GetCredential(Storedcredential);
                    return Pscredential;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
