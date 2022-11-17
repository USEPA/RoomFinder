using Microsoft.Identity.Client;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Core.Services
{
    /// <summary>
    /// This class encapsulates the details of getting a token from MSAL and exposes it via the 
    /// IAuthenticationProvider interface so that GraphServiceClient or AuthHandler can use it.
    /// </summary>
    /// A significantly enhanced version of this class will in the future be available from
    /// the GraphSDK team. It will support all the types of Client Application as defined by MSAL.
    public class GraphMsalAuthenticationProvider : Microsoft.Graph.IAuthenticationProvider
    {
        protected ILogger Logger { get; }
        private readonly IClientApplicationBase _clientApplication;
        private readonly string[] _scopes;

        public GraphMsalAuthenticationProvider(IClientApplicationBase clientApplication, string[] scopes)
        {
            _clientApplication = clientApplication;
            _scopes = scopes;
        }

        public GraphMsalAuthenticationProvider(ILogger logger, IClientApplicationBase clientApplication, string[] scopes)
            : this(clientApplication, scopes)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Update HttpRequestMessage with credentials
        /// </summary>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            AuthenticationResult authentication = await TryGetAccessTokenResultAsync().ConfigureAwait(false);
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authentication.CreateAuthorizationHeader());
        }

        /// <summary>
        /// Update HttpClient with credentials
        /// </summary>
        public async Task AuthenticateRequestAsync(HttpClient request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            AuthenticationResult authentication = await TryGetAccessTokenResultAsync().ConfigureAwait(false);
            request.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authentication.CreateAuthorizationHeader());
        }

        private async Task<AuthenticationResult> TryGetAccessTokenResultAsync()
        {
            AuthenticationResult authentication = _clientApplication is PublicClientApplication
                ? await GetAuthenticationAsync().ConfigureAwait(false)
                : await GetAuthenticationDaemonAsync().ConfigureAwait(false);
            return authentication;
        }

        /// <summary>
        /// Acquire Token for user
        /// </summary>
        private async Task<AuthenticationResult> GetAuthenticationAsync()
        {
            AuthenticationResult authResult;
            var application = _clientApplication as PublicClientApplication;

            try
            {
                var accounts = await application.GetAccountsAsync().ConfigureAwait(false);
                authResult = await application.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                Logger.Logging(LogEventLevel.Error, $"MSAL Error {ex}", new[] { "MsalUiRequiredException" });
                try
                {
                    authResult = await application.AcquireTokenByIntegratedWindowsAuth(_scopes)
                        .ExecuteAsync()
                        .ConfigureAwait(false);
                }
                catch (MsalException aex)
                {
                    Logger.Logging(LogEventLevel.Error, $"MSAL AcquireTokenByIntegratedWindowsAuth {aex}", new[] { "MsalException" });
                    throw;
                }
            }

            return authResult;
        }

        /// <summary>
        /// Acquire Token for confidential client [daemon]
        /// </summary>
        private async Task<AuthenticationResult> GetAuthenticationDaemonAsync()
        {
            var application = _clientApplication as ConfidentialClientApplication;

            try
            {
                var authResult = await application.AcquireTokenForClient(_scopes)
                    .WithForceRefresh(false)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                return authResult;
            }
            catch (MsalException ex)
            {
                Logger.Logging(LogEventLevel.Error, $"MSAL Error {ex}", new[] { "MsalException" });
                throw;
            }
        }

    }
}
