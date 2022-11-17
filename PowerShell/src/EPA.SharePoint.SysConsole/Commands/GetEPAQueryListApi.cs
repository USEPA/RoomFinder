using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.REST;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.IO;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("GetEPAQueryListApi", HelpText = "The function cmdlet will serialize the mappings and push them to sharepoint.")]
    public class GetEPAQueryListApiOptions : CommonOptions
    {
        /// <summary>
        /// The site
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }

        /// <summary>
        /// The display name for the list or library to query
        /// </summary>
        [Option("library-name", Required = true)]
        public string LibraryName { get; set; }

        /// <summary>
        /// A collection of internal names to retreive and dump to a txt file
        /// </summary>
        [Option("throttle", Required = false)]
        public int? Throttle { get; set; }
    }

    public static class GetEPAQueryListApiOptionsExtension
    {
        public static int RunGenerateAndReturnExitCode(this GetEPAQueryListApiOptions opts, IAppSettings appSettings)
        {
            var cmd = new GetEPAQueryListApi(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Opens a web request using the ClientContext credentials
    ///     Queries the specified list via the REST API
    /// </summary>
    public class GetEPAQueryListApi : BaseSpoCommand<GetEPAQueryListApiOptions>
    {
        public GetEPAQueryListApi(GetEPAQueryListApiOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Opts.SiteUrl), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            if (!Opts.Throttle.HasValue)
            {
                Opts.Throttle = 200;
            }

            try
            {
                var contextWeb = ClientContext.Web;
                contextWeb.EnsureProperties(ctw => ctw.Url, ctw => ctw.ServerRelativeUrl);
                var absoluteWebUrl = TokenHelper.EnsureTrailingSlash(contextWeb.Url);

                // region Consume the web service
                var absoluteListUrl = $"{absoluteWebUrl}_api/web/lists/getByTitle('{Opts.LibraryName}')";


                var creds = SPOnlineConnection.CurrentConnection.GetActiveCredentials();
                var spourl = new Uri(absoluteWebUrl);
                var spocreds = new SharePointOnlineCredentials(creds.UserName, creds.SecurePassword);
                var spocookies = spocreds.GetAuthenticationCookie(spourl);
                var spocontainer = new System.Net.CookieContainer();
                spocontainer.SetCookies(spourl, spocookies);


                var ListService = $"{absoluteListUrl}/ItemCount";
                var webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(ListService);
                webRequest.Credentials = spocreds;
                webRequest.Method = "GET";
                webRequest.Accept = "application/json;odata=verbose";
                webRequest.CookieContainer = spocontainer;

                var webResponse = webRequest.GetResponse();
                using Stream itemWebStream = webResponse.GetResponseStream();
                using StreamReader itemResponseReader = new StreamReader(itemWebStream);
                var itemResponse = itemResponseReader.ReadToEnd();
                var jobj = JObject.Parse(itemResponse);
                var itemCount = jobj["d"]["ItemCount"];
                LogVerbose("ItemCount:{0}", itemCount);

                var successFlag = true;
                ListService = $"{absoluteListUrl}/items?$top={Opts.Throttle}";
                while (successFlag)
                {
                    LogVerbose("Paging:{0}", ListService);
                    successFlag = false;
                    webRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(ListService);
                    webRequest.Credentials = spocreds;
                    webRequest.Method = "GET";
                    webRequest.Accept = "application/json;odata=minimalmetadata";
                    webRequest.ContentType = "application/json;odata=minimalmetadata";
                    webRequest.CookieContainer = spocontainer;

                    webResponse = webRequest.GetResponse();
                    using Stream webStream = webResponse.GetResponseStream();
                    using StreamReader responseReader = new StreamReader(webStream);
                    var response = responseReader.ReadToEnd();

                    var restobj = JsonConvert.DeserializeObject<ApiMinimalObject>(response);
                    foreach (var minj in restobj.value)
                    {
                        LogVerbose("ID:{0} #|# Type:{1} #|# Modified:{2}", minj.Id, minj.FileSystemObjectType, minj.Modified);
                    }

                    if (!string.IsNullOrEmpty(restobj.NextLink))
                    {
                        successFlag = true;
                        ListService = restobj.NextLink;
                    }

                    //if (!string.IsNullOrEmpty(restobj.d.__next))
                    //{
                    //    successFlag = true;
                    //    ListService = restobj.d.__next; // Translate an encoded string into a proper URI
                    //}
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Failed in Get-EPAQueryListApi for Library {0}", Opts.LibraryName);
            }

            return 1;
        }

        /// <summary>
        /// Retreives the internal column name value for the list item
        /// </summary>
        /// <param name="j"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private string GetColumnValue(JObject j, string columnName)
        {
            var rval = string.Empty;
            if (j.TryGetValue(columnName, out JToken rtypeval))
            {
                rval = rtypeval.ToString();
            }
            return rval;
        }

        internal class ApiVerboseObject
        {
            [JsonProperty("d")]
            public RestListItemResponseVerboseObject<CustomRestItemObj> Data { get; set; }
        }

        internal class ApiMinimalObject : RestListItemResponseMinimalObject<CustomRestMinimalItemObj>
        {

        }

        internal class CustomRestItemObj : RestListItemObj
        {
            public CustomRestItemObj() : base() { }

            public string Request_x0020_Status { get; set; }
        }

        internal class CustomRestMinimalItemObj : RestListItemMinimalObj
        {
            public CustomRestMinimalItemObj() : base() { }

            public string Request_x0020_Status { get; set; }
        }
    }
}
