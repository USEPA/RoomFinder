using EPA.Office365.Diagnostics;
using EPA.Office365.Exceptions;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EPA.Office365.Extensions
{
    /// <summary>
    /// Provides Graph HttpClient implementation helpers
    /// </summary>
    public static class GraphExtensions
    {
        /// <summary>
        /// Enables throttling support
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceFullUrl"></param>
        /// <param name="clientAsync"></param>
        /// <param name="maxAttempts"></param>
        /// <param name="backoffIntervalInSeconds"></param>
        /// <returns></returns>
        public static async Task<T> InvokePostAsync<T>(this Uri serviceFullUrl, Func<Task<T>> clientAsync, int maxAttempts = 3, int backoffIntervalInSeconds = 6)
        {
            var result = default(T);
            var retryAttempts = 0;
            bool retry = true;
            while (retry)
            {
                try
                {
                    retry = false;
                    result = await clientAsync().ConfigureAwait(false);
                }
                catch (WebException wex)
                {
                    // Check if request was throttled - http status code 429
                    // Check is request failed due to server unavailable - http status code 503
                    if (wex.Response is HttpWebResponse response &&
                        (response.StatusCode == (HttpStatusCode)429 // Service throttling [use retry logic]
                            || response.StatusCode == (HttpStatusCode)503 // Service unavailable [Azure API - unavailable || use retry logic]
                            || response.StatusCode == (HttpStatusCode)504 // Gateway Timeout [Azure API - timeout on response || use retry logic]
                            ))
                    {
                        TimeSpan backoffSpan = ExtractBackoffTimeSpan(response, backoffIntervalInSeconds);
                        Log.LogWarning($"Microsoft Graph API => exceeded usage limits. Iteration => {backoffSpan.Seconds} Sleeping for {retryAttempts} seconds before retrying..");

                        //Add delay for retry
                        await Task.Delay(backoffSpan).ConfigureAwait(false);

                        //Add to retry count and check max attempts.
                        retryAttempts++;
                        retry = (retryAttempts < maxAttempts);
                    }
                    else
                    {
                        Log.LogError(wex, $"HTTP Failed to query URI {serviceFullUrl} exception: {wex}");
                        throw;
                    }
                }
                catch (AggregateException agex)
                {
                    agex.InnerExceptions.ToList().ForEach(exception =>
                    {
                        Log.LogWarning($"AggregateException URI {serviceFullUrl} => {exception.Message}");
                    });
                    throw new Exception($"Multiple errors occurred, check logs and assert {serviceFullUrl}");
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Generic Failed to query URI {serviceFullUrl} => {ex.Message}");
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Enables throttling support
        /// </summary>
        /// <param name="client">The instantiated <seealso cref="HttpClient"/> to issue requests</param>
        /// <param name="request">The request to issue against the configured HttpClient</param>
        /// <param name="userAgent">User agent for Throttling control at Tenant Level</param>
        /// <param name="cancellationToken">Threading cancellation token to issue cancel/kill if thread dies</param>
        /// <returns>The result of the asynchronous request</returns>
        /// <remarks>See here for further details: https://graph.microsoft.io/en-us/docs/overview/errors</remarks>
        /// <exception cref="GraphWebException">Unknown service exception.</exception>
        /// <exception cref="MaximumRetryAttemptedException">Too many attempts before result was handled.</exception>
        public static async Task<HttpResponseMessage> InvokeSendAsync(this HttpClient client, HttpRequestMessage request, string userAgent, CancellationToken cancellationToken)
         => await InvokeSendAsync(client, request, userAgent, 3, 6, cancellationToken);

        /// <summary>
        /// Enables throttling support
        /// </summary>
        /// <param name="client">The instantiated <seealso cref="HttpClient"/> to issue requests</param>
        /// <param name="request">The request to issue against the configured HttpClient</param>
        /// <param name="userAgent">User agent for Throttling control at Tenant Level</param>
        /// <param name="maxAttempts">Max attempt before backup/giveup is exected</param>
        /// <param name="backoffIntervalInSeconds">Initial backoff in seconds, controlled by Graph header in response.</param>
        /// <param name="cancellationToken">Threading cancellation token to issue cancel/kill if thread dies</param>
        /// <returns>The result of the asynchronous request</returns>
        /// <remarks>See here for further details: https://graph.microsoft.io/en-us/docs/overview/errors</remarks>
        /// <exception cref="GraphWebException">Unknown service exception.</exception>
        /// <exception cref="MaximumRetryAttemptedException">Too many attempts before result was handled.</exception>
        public static async Task<HttpResponseMessage> InvokeSendAsync(this HttpClient client, HttpRequestMessage request, string userAgent, int maxAttempts, int backoffIntervalInSeconds, CancellationToken cancellationToken)
        {
            var retryAttempts = 0;
            bool retry = true;
            var serviceFullUrl = request.RequestUri;

            // Add the User Agent string
            request.Headers.UserAgent.TryParseAdd(string.IsNullOrEmpty(userAgent) ? $"{CoreResources.USEPA_UserAgent}" : userAgent);

            while (retry)
            {
                try
                {
                    retry = false;
                    // Make the request
                    var result = await client.SendAsync(request, cancellationToken);
                    return (result);
                }
                catch (System.IO.IOException sioex)
                {
                    var serviceMessage = $"HTTP Failed to query URI {serviceFullUrl} with redirect file exception: {sioex}";
                    Log.LogError(sioex, serviceMessage);
                    throw new GraphWebException(serviceFullUrl, serviceMessage, sioex);
                }
                catch (WebException wex)
                {
                    // Check if request was throttled - http status code 429
                    // Check is request failed due to server unavailable - http status code 503
                    if (wex.Response is HttpWebResponse response &&
                        (response.StatusCode == (HttpStatusCode)429 // Service throttling [use retry logic]
                            || response.StatusCode == (HttpStatusCode)503 // Service unavailable [Azure API - unavailable || use retry logic]
                            || response.StatusCode == (HttpStatusCode)504 // Gateway Timeout [Azure API - timeout on response || use retry logic]
                            ))
                    {
                        TimeSpan backoffSpan = ExtractBackoffTimeSpan(response, backoffIntervalInSeconds);
                        Log.LogWarning($"Microsoft Graph API => exceeded usage limits. Iteration => {backoffSpan.Seconds} Sleeping for {retryAttempts} seconds before retrying..");

                        //Add delay for retry
                        await Task.Delay(backoffSpan).ConfigureAwait(false);

                        //Add to retry count and check max attempts.
                        retryAttempts++;
                        retry = (retryAttempts < maxAttempts);
                    }
                    else
                    {
                        Log.LogError(wex, $"HTTP Failed to query URI {serviceFullUrl} exception: {wex}");
                        LogWebException(wex);
                        throw new GraphWebException(serviceFullUrl, $"Failed: {wex.Message}", wex);
                    }
                }
                catch (AggregateException agex)
                {
                    agex.InnerExceptions.ToList().ForEach(exception =>
                    {
                        Log.LogWarning($"AggregateException URI {serviceFullUrl} => {exception.Message}");
                    });
                    throw new Exception($"Multiple errors occurred, check logs and assert {serviceFullUrl}");
                }
                // Or handle any ServiceException
                catch (Exception ex)
                {
                    var serviceMessage = string.Format(CoreResources.GraphExtensions_SendAsyncRetryException, ex.ToString());
                    Log.LogError(serviceMessage);
                    throw new GraphWebException(serviceFullUrl, serviceMessage, ex);
                }
            }

            throw new MaximumRetryAttemptedException($"Maximum retry attempts {retryAttempts}, has be attempted.");
        }

        /// <summary>
        /// Enables throttling support
        /// </summary>
        /// <param name="provider">The instantiated <seealso cref="HttpProvider"/> to issue requests</param>
        /// <param name="request">The request to issue against the configured HttpClient</param>
        /// <param name="completionOption">HttpClient processing instructions</param>
        /// <param name="userAgent">User agent for Throttling control at Tenant Level</param>
        /// <param name="cancellationToken">Threading cancellation token to issue cancel/kill if thread dies</param>
        /// <returns>The result of the asynchronous request</returns>
        /// <remarks>See here for further details: https://graph.microsoft.io/en-us/docs/overview/errors</remarks>
        /// <exception cref="GraphWebException">Unknown service exception.</exception>
        /// <exception cref="MaximumRetryAttemptedException">Too many attempts before result was handled.</exception>
        public static async Task<HttpResponseMessage> InvokeSendAsync(this HttpProvider provider, HttpRequestMessage request, HttpCompletionOption completionOption, string userAgent, CancellationToken cancellationToken)
         => await InvokeSendAsync(provider, request, completionOption, userAgent, 3, 6, cancellationToken);

        /// <summary>
        /// Enables throttling support
        /// </summary>
        /// <param name="provider">The instantiated <seealso cref="Microsoft.Graph.HttpProvider"/> to issue requests</param>
        /// <param name="request">The request to issue against the configured HttpClient</param>
        /// <param name="completionOption">HttpClient processing instructions</param>
        /// <param name="userAgent">User agent for Throttling control at Tenant Level</param>
        /// <param name="maxAttempts">Max attempt before backup/giveup is exected</param>
        /// <param name="backoffIntervalInSeconds">Initial backoff in seconds, controlled by Graph header in response.</param>
        /// <param name="cancellationToken">Threading cancellation token to issue cancel/kill if thread dies</param>
        /// <returns>The result of the asynchronous request</returns>
        /// <remarks>See here for further details: https://graph.microsoft.io/en-us/docs/overview/errors</remarks>
        /// <exception cref="GraphWebException">Unknown service exception.</exception>
        /// <exception cref="MaximumRetryAttemptedException">Too many attempts before result was handled.</exception>
        public static async Task<HttpResponseMessage> InvokeSendAsync(this HttpProvider provider, HttpRequestMessage request, HttpCompletionOption completionOption, string userAgent, int maxAttempts, int backoffIntervalInSeconds, CancellationToken cancellationToken)
        {
            var retryAttempts = 0;
            bool retry = true;
            var serviceFullUrl = request.RequestUri;

            // Add the User Agent string
            request.Headers.UserAgent.TryParseAdd(string.IsNullOrEmpty(userAgent) ? $"{CoreResources.USEPA_UserAgent}" : userAgent);

            while (retry)
            {
                try
                {
                    retry = false;

                    // Make the request
                    var result = await provider.SendAsync(request, completionOption, cancellationToken);
                    return (result);
                }
                catch (WebException wex)
                {
                    // Check if request was throttled - http status code 429
                    // Check is request failed due to server unavailable - http status code 503
                    if (wex.Response is HttpWebResponse response &&
                        (response.StatusCode == (HttpStatusCode)429 // Service throttling [use retry logic]
                            || response.StatusCode == (HttpStatusCode)503 // Service unavailable [Azure API - unavailable || use retry logic]
                            || response.StatusCode == (HttpStatusCode)504 // Gateway Timeout [Azure API - timeout on response || use retry logic]
                            ))
                    {
                        TimeSpan backoffSpan = ExtractBackoffTimeSpan(response, backoffIntervalInSeconds);
                        Log.LogWarning($"Microsoft Graph API => exceeded usage limits. Iteration => {backoffSpan.Seconds} Sleeping for {retryAttempts} seconds before retrying..");

                        //Add delay for retry
                        await Task.Delay(backoffSpan).ConfigureAwait(false);

                        //Add to retry count and check max attempts.
                        retryAttempts++;
                        retry = (retryAttempts < maxAttempts);
                    }
                    else
                    {
                        Log.LogError(wex, $"HTTP Failed to query URI {serviceFullUrl} exception: {wex}");
                        throw new GraphWebException(serviceFullUrl, $"Failed: {wex.Message}", wex);
                    }
                }
                catch (AggregateException agex)
                {
                    agex.InnerExceptions.ToList().ForEach(exception =>
                    {
                        Log.LogWarning($"AggregateException URI {serviceFullUrl} => {exception.Message}");
                    });
                    throw new Exception($"Multiple errors occurred, check logs and assert {serviceFullUrl}");
                }
                // Or handle any ServiceException
                catch (Exception ex)
                {
                    var serviceMessage = string.Format(CoreResources.GraphExtensions_SendAsyncRetryException, ex.ToString());
                    Log.LogError(serviceMessage);
                    throw new GraphWebException(serviceFullUrl, serviceMessage, ex);
                }
            }

            throw new MaximumRetryAttemptedException($"Maximum retry attempts {retryAttempts}, has be attempted.");
        }

        /// <summary>
        /// Extract the Retry-After throttling suggestion
        /// </summary>
        /// <param name="response"></param>
        /// <param name="backoffIntervalInSeconds"></param>
        /// <returns></returns>
        private static TimeSpan ExtractBackoffTimeSpan(HttpWebResponse response, int backoffIntervalInSeconds = 6)
        {
            var graphBackoffInterval = backoffIntervalInSeconds;
            var graphApiRetrySeconds = response.GetResponseHeader("Retry-After");
            if (!string.IsNullOrEmpty(graphApiRetrySeconds)
                && int.TryParse(graphApiRetrySeconds, out int headergraphBackoffInterval))
            {
                graphBackoffInterval = headergraphBackoffInterval <= 0 ? backoffIntervalInSeconds : headergraphBackoffInterval;
            }
            var backoffSpan = TimeSpan.FromSeconds(graphBackoffInterval);
            return backoffSpan;
        }


        [SuppressMessage("Minor Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Passing parameters to avoid requires sloppy code.")]
        public static async Task<T> DeserializeResult<T>(this HttpResponseMessage response)
        {
            if (response == null)
            {
                Log.LogDebug($"Failed to retreive HttpResponse.");
                return default;
            }

            if (response.IsSuccessStatusCode)
            {
                Log.LogDebug($"Http status code is {response.StatusCode}");
                var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            else
            {
                Log.LogWarning($"Failed to call the Web Api: {response.StatusCode}");
                string responsecontent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.LogWarning($"Failed to call the Web Api: {responsecontent}");
                var errorModel = JsonConvert.DeserializeObject<Microsoft.Graph.Error>(responsecontent);
                Log.LogWarning($"Error message {response.ReasonPhrase}.  Error details {errorModel.Message}.");
            }

            return default;
        }

        private static void LogWebException(WebException wex)
        {
            if (wex.Response != null && wex.Response is HttpWebResponse)
            {
                using var strm = new System.IO.StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8);
                var response = strm.ReadToEnd();
                Log.LogWarning($"WebEx Response Handler {response} from Graph Request");
            }

            if (wex.InnerException is WebException webEx
                && (webEx != null && webEx.Response is HttpWebResponse myResponse))
            {
                using var strm = new System.IO.StreamReader(myResponse.GetResponseStream(), Encoding.UTF8);
                var response = strm.ReadToEnd();
                Log.LogWarning($"Inner WebEx Handler {response} from Graph Request");
            }
        }
    }
}
