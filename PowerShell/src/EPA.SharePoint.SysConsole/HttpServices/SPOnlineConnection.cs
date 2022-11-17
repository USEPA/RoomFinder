using EPA.SharePoint.SysConsole.Models;
using Microsoft.SharePoint.Client;
using System;

namespace EPA.SharePoint.SysConsole.HttpServices
{
    /// <summary>
    /// Provides a connection object for operating on SharePoint instance
    /// </summary>
    public class SPOnlineConnection
    {
        private ClientContext _initialContext;

        public static SPOnlineConnection CurrentConnection { get; set; }

        public ConnectionType ConnectionType { get; protected set; }

        public int MinimalHealthScore { get; protected set; }

        public int RetryCount { get; protected set; }

        public int RetryWait { get; protected set; }

        public System.Net.NetworkCredential PSCredential { get; protected set; }

        public SPOAddInKeys AddInCredentials { get; protected set; }

        public string Url { get; protected set; }

        public ClientContext Context { get; set; }

        /// <summary>
        /// Initializes the OnlineConnection for connecting via Federation/Integrated/OAuth
        /// </summary>
        /// <param name="context"></param>
        /// <param name="connectionType"></param>
        /// <param name="minimalHealthScore"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryWait"></param>
        /// <param name="credential"></param>
        /// <param name="url"></param>
        public SPOnlineConnection(ClientContext context, ConnectionType connectionType, int minimalHealthScore, int retryCount, int retryWait, System.Net.NetworkCredential credential, string url)
        {
            Context = context ?? throw new ArgumentNullException("context");
            _initialContext = context;
            ConnectionType = connectionType;
            MinimalHealthScore = minimalHealthScore;
            RetryCount = retryCount;
            RetryWait = retryWait;
            PSCredential = credential;
            Url = url;
        }

        /// <summary>
        /// Initializes the OnlineConnection for connecting via Federation/Integrated/OAuth
        /// </summary>
        /// <param name="context"></param>
        /// <param name="connectionType"></param>
        /// <param name="minimalHealthScore"></param>
        /// <param name="retryCount"></param>
        /// <param name="retryWait"></param>
        /// <param name="credential"></param>
        /// <param name="url"></param>
        public SPOnlineConnection(ClientContext context, ConnectionType connectionType, int minimalHealthScore, int retryCount, int retryWait, SPOAddInKeys credential, string url)
        {
            Context = context ?? throw new ArgumentNullException("context");
            _initialContext = context;
            ConnectionType = connectionType;
            MinimalHealthScore = minimalHealthScore;
            RetryCount = retryCount;
            RetryWait = retryWait;
            AddInCredentials = credential;
            Url = url;
        }

        public void RestoreCachedContext()
        {
            Context = _initialContext;
        }

        public void CacheContext()
        {
            _initialContext = Context;
        }

        /// <summary>
        /// Returns the user who initiated the connection
        /// </summary>
        /// <returns></returns>
        public string GetActiveUsername()
        {
            return CurrentConnection?.PSCredential?.UserName ?? string.Empty;
        }

        /// <summary>
        /// Returns the active credentials for the SPO connection
        /// </summary>
        /// <returns></returns>
        public System.Net.NetworkCredential GetActiveCredentials()
        {
            return CurrentConnection?.PSCredential;
        }

        public bool IsAddInCredentials
        {
            get
            {
                return !string.IsNullOrEmpty(AddInCredentials?.AppId);
            }
        }
    }
}
