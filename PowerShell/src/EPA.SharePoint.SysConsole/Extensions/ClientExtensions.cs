using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using System;
using System.Diagnostics;

namespace EPA.SharePoint.SysConsole.Extensions
{
    /// <summary>
    /// Extension Methods for ClientContext
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CSOM unhandled exceptions")]
    public static class ClientExtensions
    {
        /// <summary>
        /// Load the collection and execute retry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static T Load<T>(this T collection) where T : ClientObjectCollection
        {
            if (collection.ServerObjectIsNull == null || collection.ServerObjectIsNull == true)
            {
                collection.Context.Load(collection);
                collection.Context.ExecuteQueryRetry();
                return collection;
            }
            else
            {
                return collection;
            }
        }

        /// <summary>
        /// Take the URL and clean it
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string EnsureTrailingSlash(this string url)
        {
            var surl = url;
            if (!string.IsNullOrEmpty(surl))
            {
#if !NETSTANDARD2_0
                surl = TokenHelper.EnsureTrailingSlash(url.Trim());
#endif
            }
            return surl;
        }

        /// <summary>
        /// Take the URL and clean it
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string EnsureTrailingSlashLowered(this string url)
        {
            var surl = url;
            if (!string.IsNullOrEmpty(surl))
            {
#if !NETSTANDARD2_0
                surl = TokenHelper.EnsureTrailingSlash(url.Trim().ToLower());
#endif
            }
            return surl;
        }

        /// <summary>
        /// Moves a file from <paramref name="serverRelativeUrl"/> to <paramref name="newRelativeUrl"/>
        /// </summary>
        /// <param name="context">The Client Context containing the library/list where the item resides</param>
        /// <param name="itemUrl">The current path for the list item</param>
        /// <param name="newUrl">The target path for the list item</param>
        /// <returns></returns>
        public static bool MoveFileToFolder(this ClientContext context, string itemUrl, string newUrl, int retryCount = 10, int delay = 500, string userAgent = null)
        {
            try
            {
                var targetItem = context.Web.GetFileByServerRelativeUrl(itemUrl);
                context.Load(targetItem);
                context.ExecuteQueryRetry(retryCount, delay, userAgent: userAgent);

                targetItem.MoveTo(newUrl, MoveOperations.None);
                context.ExecuteQueryRetry(retryCount, delay, userAgent: userAgent);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Retreive Owner from the Context <see cref="Site"/>
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public static string GetSiteOwner(this ClientContext ctx)
        {
            string _userEmail = "";
            try
            {
                ctx.Site.EnsureProperties(esp => esp.Owner, esp => esp.Owner.Email);
                _userEmail = ctx.Site.Owner.Email;
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to retrieve site owners {0}", e.Message);
            }

            return _userEmail;
        }
    }
}
