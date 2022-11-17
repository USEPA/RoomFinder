﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace EPA.Office365.Graph
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
    public static class GraphHttpHelper
    {
        public const string GraphBaseUrl = "https://graph.microsoft.com";
        public const string MicrosoftGraphV1BaseUri = GraphBaseUrl + "/v1.0/";
        public const string MicrosoftGraphBetaBaseUri = GraphBaseUrl + "/beta/";
        public const string JsonContentType = "application/json";

        /// <summary>
        /// Static readonly instance of HttpClient to improve performances
        /// </summary>
        /// <remarks>
        /// See https://docs.microsoft.com/en-us/azure/architecture/antipatterns/improper-instantiation/
        /// </remarks>
        private static readonly HttpClient httpClient =
            new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }, true);

        /// <summary>
        /// This helper method makes an HTTP GET request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accept">The value for the accept header in the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The String value of the result</returns>
        public static string MakeGetRequestForString(string requestUrl,
            string accessToken = null,
            string accept = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null,
            bool continueOnError = false)
        {
            return (MakeHttpRequest("GET",
                requestUrl,
                accessToken: accessToken,
                accept: accept,
                resultPredicate: r => r.Content.ReadAsStringAsync().Result,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent,
                continueOnError: continueOnError));
        }

        /// <summary>
        /// This helper method makes an HTTP GET request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="accept">The accept header for the response</param>
        /// <param name="referer">The URL Referer for the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The Stream  of the result</returns>
        public static System.IO.Stream MakeGetRequestForStream(string requestUrl,
            String accept,
            string accessToken = null,
            string referer = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            return (MakeHttpRequest("GET",
                requestUrl,
                accessToken,
                accept: accept,
                referer: referer,
                resultPredicate: r => r.Content.ReadAsStreamAsync().Result,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent));
        }

        /// <summary>
        /// This helper method makes an HTTP GET request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="responseHeaders">The response headers of the HTTP request (output argument)</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="accept">The accept header for the response</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The Stream  of the result</returns>
        public static System.IO.Stream MakeGetRequestForStreamWithResponseHeaders(string requestUrl,
            string accept,
            out HttpResponseHeaders responseHeaders,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            return (MakeHttpRequest("GET",
                requestUrl,
                out responseHeaders,
                accessToken,
                accept: accept,
                resultPredicate: r => r.Content.ReadAsStreamAsync().Result,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent
            ));
        }

        /// <summary>
        /// This helper method makes an HTTP POST request without a response
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content/type of the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        public static void MakePostRequest(string requestUrl,
            Object content = null,
            string contentType = null,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            MakeHttpRequest<string>("POST",
                requestUrl,
                accessToken: accessToken,
                content: content,
                contentType: contentType,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent);
        }

        /// <summary>
        /// This helper method makes an HTTP POST request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content/type of the request</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="accept">The value for the accept header in the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The String value of the result</returns>
        public static string MakePostRequestForString(string requestUrl,
            Object content = null,
            string contentType = null,
            string accessToken = null,
            string accept = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            return (MakeHttpRequest("POST",
                requestUrl,
                accessToken: accessToken,
                content: content,
                contentType: contentType,
                resultPredicate: r => r.Content.ReadAsStringAsync().Result,
                requestHeaders: requestHeaders,
                cookies: cookies,
                accept: accept,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent));
        }

        /// <summary>
        /// This helper method makes an HTTP GET request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content type for the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The Stream  of the result</returns>
        public static HttpResponseHeaders MakePostRequestForHeaders(string requestUrl,
            object content = null,
            string contentType = null,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            return MakeHttpRequest("POST",
                requestUrl,
                accessToken,
                content: content,
                contentType: contentType,
                resultPredicate: response => response.Headers,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent
            );
        }

        /// <summary>
        /// This helper method makes an HTTP PUT request without a response
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content type for the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        public static void MakePutRequest(string requestUrl,
            Object content = null,
            string contentType = null,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            MakeHttpRequest<string>("PUT",
                requestUrl,
                accessToken: accessToken,
                content: content,
                contentType: contentType,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent);
        }

        /// <summary>
        /// This helper method makes an HTTP PUT request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content/type of the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The String value of the result</returns>
        public static string MakePutRequestForString(string requestUrl,
            object content = null,
            string contentType = null,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            return (MakeHttpRequest("PUT",
                requestUrl,
                accessToken: accessToken,
                content: content,
                contentType: contentType,
                resultPredicate: r => r.Content.ReadAsStringAsync().Result,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent
            ));
        }

        /// <summary>
        /// This helper method makes an HTTP PATCH request and returns the result as a String
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content/type of the request</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The String value of the result</returns>
        public static String MakePatchRequestForString(String requestUrl,
            object content = null,
            string contentType = null,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            return (MakeHttpRequest("PATCH",
                requestUrl,
                accessToken: accessToken,
                content: content,
                contentType: contentType,
                resultPredicate: r => r.Content.ReadAsStringAsync().Result,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent));
        }

        /// <summary>
        /// This helper method makes an HTTP DELETE request
        /// </summary>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <returns>The String value of the result</returns>
        public static void MakeDeleteRequest(String requestUrl,
            string accessToken = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null)
        {
            MakeHttpRequest<string>("DELETE",
                requestUrl,
                accessToken,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent);
        }

        /// <summary>
        /// This helper method makes an HTTP request and eventually returns a result
        /// </summary>
        /// <param name="httpMethod">The HTTP method for the request</param>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="accept">The content type of the accepted response</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content  type of the request</param>
        /// <param name="referer">The URL Referer for the request</param>
        /// <param name="resultPredicate">The predicate to retrieve the result, if any</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <typeparam name="TResult">The type of the result, if any</typeparam>
        /// <returns>The value of the result, if any</returns>
        private static TResult MakeHttpRequest<TResult>(
            String httpMethod,
            string requestUrl,
            string accessToken = null,
            string accept = null,
            object content = null,
            string contentType = null,
            string referer = null,
            Func<HttpResponseMessage, TResult> resultPredicate = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null,
            bool continueOnError = true)
        {
            return (MakeHttpRequest(httpMethod,
                requestUrl,
                out HttpResponseHeaders responseHeaders,
                accessToken: accessToken,
                accept: accept,
                content: content,
                contentType: contentType,
                referer: referer,
                resultPredicate: resultPredicate,
                requestHeaders: requestHeaders,
                cookies: cookies,
                retryCount: retryCount,
                delay: delay,
                userAgent: userAgent,
                continueOnError: continueOnError
                ));
        }

        /// <summary>
        /// This helper method makes an HTTP request and eventually returns a result
        /// </summary>
        /// <param name="httpMethod">The HTTP method for the request</param>
        /// <param name="requestUrl">The URL of the request</param>
        /// <param name="responseHeaders">The response headers of the HTTP request (output argument)</param>
        /// <param name="accessToken">The OAuth 2.0 Access Token for the request, if authorization is required</param>
        /// <param name="accept">The content type of the accepted response</param>
        /// <param name="content">The content of the request</param>
        /// <param name="contentType">The content  type of the request</param>
        /// <param name="referer">The URL Referer for the request</param>
        /// <param name="resultPredicate">The predicate to retrieve the result, if any</param>
        /// <param name="requestHeaders">A collection of any custom request headers</param>
        /// <param name="cookies">Any request cookies values</param>
        /// <param name="retryCount">Number of times to retry the request</param>
        /// <param name="delay">Milliseconds to wait before retrying the request. The delay will be increased (doubled) every retry</param>
        /// <param name="userAgent">UserAgent string value to insert for this request.</param>
        /// <typeparam name="TResult">The type of the result, if any</typeparam>
        /// <returns>The value of the result, if any</returns>
        internal static TResult MakeHttpRequest<TResult>(
            string httpMethod,
            string requestUrl,
            out HttpResponseHeaders responseHeaders,
            string accessToken = null,
            string accept = null,
            object content = null,
            string contentType = null,
            string referer = null,
            Func<HttpResponseMessage, TResult> resultPredicate = null,
            Dictionary<string, string> requestHeaders = null,
            Dictionary<string, string> cookies = null,
            int retryCount = 1,
            int delay = 500,
            string userAgent = null,
            bool continueOnError = false)
        {
            HttpClient client = GraphHttpHelper.httpClient;

            // Define whether to use the default HttpClient object
            // or a custom one with retry logic and/or with custom request cookies
            // or a SharePoint client context to rely on
            if (retryCount >= 1 || (cookies != null && cookies.Count > 0))
            {
                // Let's create a custom HttpHandler
                var handler = new HttpClientHandler();

                if (requestHeaders == null)
                {
                    requestHeaders = new Dictionary<string, string>();
                }

                // Process any other request cookies
                if (cookies != null)
                {
                    foreach (var cookie in cookies)
                    {
                        handler.CookieContainer.Add(new System.Net.Cookie(cookie.Key, cookie.Value));
                    }
                }

                // And now create the customized HttpClient object
                client = new GraphHttpClient(handler, true, retryCount, delay, userAgent);
            }
            // Prepare the variable to hold the result, if any
            TResult result = default;
            responseHeaders = null;

            Uri requestUri = new Uri(requestUrl);

            // If we have the token, then handle the HTTP request

            // Set the Authorization Bearer token
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (!string.IsNullOrEmpty(referer))
            {
                client.DefaultRequestHeaders.Referrer = new Uri(referer);
            }

            // If there is an accept argument, set the corresponding HTTP header
            if (!string.IsNullOrEmpty(accept))
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue(accept));
            }

            // Process any additional custom request headers
            if (requestHeaders != null)
            {
                foreach (var requestHeader in requestHeaders)
                {
                    client.DefaultRequestHeaders.Add(requestHeader.Key, requestHeader.Value);
                }
            }

            // Prepare the content of the request, if any
            HttpContent requestContent = null;
            if (content is System.IO.Stream streamContent)
            {
                requestContent = new StreamContent(streamContent);
                requestContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
            else if (content != null)
            {
                var jsonString = content is string
                    ? content.ToString()
                    : JsonConvert.SerializeObject(content, Formatting.None, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ContractResolver = new ODataBindJsonResolver(),
                    });
                requestContent = new StringContent(jsonString, Encoding.UTF8, contentType);
            }

            // Prepare the HTTP request message with the proper HTTP method
            HttpRequestMessage request = new HttpRequestMessage(
                new HttpMethod(httpMethod), requestUrl);

            // Set the request content, if any
            if (requestContent != null)
            {
                request.Content = requestContent;
            }

            // Fire the HTTP request
            HttpResponseMessage response = client.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                // If the response is Success and there is a
                // predicate to retrieve the result, invoke it
                if (resultPredicate != null)
                {
                    result = resultPredicate(response);
                }

                // Get any response header and put it in the answer
                responseHeaders = response.Headers;
            }
            else
            {
                if (continueOnError)
                {
                    TraceLogger.Error(string.Format("Exception while invoking endpoint {0}.", requestUrl));

                }
                else
                {
                    throw new ApplicationException(
                        string.Format("Exception while invoking endpoint {0}.", requestUrl),
#if !NETSTANDARD2_1
                                        new System.Web.HttpException(
                                            (int)response.StatusCode,
                                            response.Content.ReadAsStringAsync().Result));
#else
                                        new Exception(response.Content.ReadAsStringAsync().Result));
#endif
                }
            }

            return result;
        }
    }
}
