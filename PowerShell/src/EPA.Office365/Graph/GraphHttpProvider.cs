using EPA.Office365.Diagnostics;
using EPA.Office365.Extensions;
using Microsoft.Graph;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EPA.Office365.Graph
{
    ///<summary>
    /// Class that deals with GraphHttpProvider methods
    ///</summary>  
    public class GraphHttpProvider : HttpProvider, IHttpProvider
    {
        private int RetryCount { get; }
        private int RetryDelay { get; }
        private string UserAgent { get; }

        /// <summary>
        /// Constructor for the GraphHttpProvider class
        /// </summary>
        /// <param name="retryCount">Maximum retry Count</param>
        /// <param name="delay">Delay Time</param>
        /// <param name="userAgent">User-Agent string to set</param>
        public GraphHttpProvider(Serilog.ILogger traceLogger, int retryCount = 10, int delay = 500, string userAgent = null) : base()
        {
            if (retryCount <= 0)
            {
                throw new ArgumentException("Provide a retry count greater than zero.");
            }

            if (delay <= 0)
            {
                throw new ArgumentException("Provide a delay greater than zero.");
            }

            Log.InitializeLogger(traceLogger);
            RetryCount = retryCount;
            RetryDelay = delay;
            UserAgent = userAgent;
        }

        /// <summary>
        /// Custom implementation of the IHttpProvider.SendAsync method to handle retry logic
        /// </summary>
        /// <param name="request">The HTTP Request Message</param>
        /// <param name="completionOption">The completion option</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The result of the asynchronous request</returns>
        /// <remarks>See here for further details: https://graph.microsoft.io/en-us/docs/overview/errors</remarks>
        Task<HttpResponseMessage> IHttpProvider.SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return this.InvokeSendAsync(request, completionOption, UserAgent, this.RetryCount, this.RetryDelay, cancellationToken);
        }
    }
}
