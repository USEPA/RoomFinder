using System;
using System.Net;
using System.Net.Http;
#if !NETSTANDARD2_1
using System.Threading;
using System.Threading.Tasks;
#endif

namespace EPA.Office365.Extensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Framework dependent")]
    public static class HttpExtensions
    {
        private static readonly HttpMethod s_patchMethod = new HttpMethod("PATCH");
        private static readonly Version _defaultRequestVersion = HttpVersion.Version11;

        public static Uri CreateUri(this string uri) =>
           string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);

#if !NETSTANDARD2_1
        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
        {
            return PatchAsync(client, CreateUri(requestUri), content);
        }

        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent content)
        {
            return PatchAsync(client, requestUri, content, CancellationToken.None);
        }

        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            return PatchAsync(client, CreateUri(requestUri), content, cancellationToken);
        }

        public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, Uri requestUri, HttpContent content,
            CancellationToken cancellationToken)
        {
            HttpRequestMessage request = CreateRequestMessage(requestUri, s_patchMethod);
            request.Content = content;
            return client.SendAsync(request, cancellationToken);
        }
#endif

        public static HttpRequestMessage CreateRequestMessage(this Uri uri, HttpMethod method) =>
           new HttpRequestMessage(method, uri) { Version = _defaultRequestVersion };
    }
}
