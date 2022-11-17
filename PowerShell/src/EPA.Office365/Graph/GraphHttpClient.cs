using EPA.Office365.Extensions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EPA.Office365.Graph
{
    /// <summary>
    /// PnP http client which implements setting of User-Agent + retry mechanismn on throttling
    /// </summary>
    public class GraphHttpClient : HttpClient
    {
        readonly int retryCount;
        readonly int delay;
        private string UserAgent { get; }

        /// <summary>
        /// Constructor without HttpMessageHandler
        /// </summary>
        /// <param name="retryCount">Number of retries, defaults to 10</param>
        /// <param name="delay">Incremental delay increase in milliseconds</param>
        /// <param name="userAgent">User-Agent string to set</param>
        public GraphHttpClient(int retryCount = 10, int delay = 500, string userAgent = null)
            : this(new HttpClientHandler(), retryCount, delay, userAgent)
        {
        }

        /// <summary>
        /// Constructor with HttpMessageHandler
        /// </summary>
        /// <param name="innerHandler">HttpMessageHandler instance to pass along</param>
        /// <param name="retryCount">Number of retries, defaults to 10</param>
        /// <param name="delay">Incremental delay increase in milliseconds</param>
        /// <param name="userAgent">User-Agent string to set</param>
        public GraphHttpClient(HttpMessageHandler innerHandler, int retryCount = 10, int delay = 500, string userAgent = null)
            : this(innerHandler, false, retryCount, delay, userAgent)
        {
        }

        /// <summary>
        /// Constructor with HttpMessageHandler
        /// </summary>
        /// <param name="innerHandler">HttpMessageHandler instance to pass along</param>
        /// <param name="retryCount">Number of retries, defaults to 10</param>
        /// <param name="delay">Incremental delay increase in milliseconds</param>
        /// <param name="userAgent">User-Agent string to set</param>
        /// <param name="disposeHandler">Declares whether to automatically dispose the internal HttpHandler instance</param>
        public GraphHttpClient(HttpMessageHandler innerHandler, bool disposeHandler, int retryCount = 10, int delay = 500, string userAgent = null)
            : base(innerHandler, disposeHandler)
        {
            this.retryCount = retryCount;
            this.delay = delay;
            UserAgent = userAgent;

            // Use TLS 1.2 as default connection
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        /// <summary>
        /// Perform async http request
        /// </summary>
        /// <param name="request">Http request to execute</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Response object from http request</returns>
        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.InvokeSendAsync(request, UserAgent, this.retryCount, this.delay, cancellationToken);
        }
    }
}
