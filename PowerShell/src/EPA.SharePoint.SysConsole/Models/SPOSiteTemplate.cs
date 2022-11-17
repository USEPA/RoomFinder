using EPA.SharePoint.SysConsole;
using EPA.SharePoint.SysConsole.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EPA.SharePoint.SysConsole.Models
{
    /// <summary>
    /// Site Template model
    /// </summary>
    public class SPOSiteTemplate
    {
        /// <summary>
        /// Blank template
        /// </summary>
        public SPOSiteTemplate()
        {
            Region = "USEPA";
            SiteType = "ORG";
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="region"></param>
        /// <param name="siteType"></param>
        public SPOSiteTemplate(string region, string siteType) : this()
        {
            this.Region = region;
            this.SiteType = siteType;
        }

        /// <summary>
        /// Typically the first portion after the managed path
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Typically the second portion after the managed path
        /// </summary>
        public string SiteType { get; set; }


        public const string AddInUriRegex = "^(ht|f)tp(s?)://(testusepa|usepa)-([0-9A-Fa-f]{14}).(sharepoint).(com)(.*)$";


        public const string SiteTemplateRegex = "^(ht|f)tp(s?)://(testusepa|usepa)((-[0-9A-Fa-f]{14})?).(sharepoint).(com)((/sites/)?)";

        /// <summary>
        /// Formatted string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0},{1}", Region, SiteType);
        }

        /// <summary>
        /// Take the URL and clean it
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        internal string UrlPattern(string url)
        {
            var surl = url.EnsureTrailingSlashLowered();
            return surl;
        }

        public void SeparateFormat(string url)
        {
            var _lowerSiteUrl = UrlPattern(url);
            var _isaddin = false;

            if (System.Text.RegularExpressions.Regex.IsMatch(_lowerSiteUrl, AddInUriRegex))
            {
                SiteType = "ADD-IN";
                _isaddin = true;
            }

            // replace standard URL strings 
            var siteRegex = new System.Text.RegularExpressions.Regex(SiteTemplateRegex);
            var replacedUrl = siteRegex.Replace(_lowerSiteUrl, "");

            // should appear [usepa,sitecollectionpath,additionalpaths]
            var _tmpSite = replacedUrl.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (_tmpSite.Length > 0)
            {
                // 2nd array element should be sitecollectionpath
                var _tmpSiteArr = _tmpSite[0].ToString().Split(new char[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (_tmpSiteArr.Length > 0)
                {
                    Region = _tmpSiteArr[0].ToUpper();
                }
                else
                {
                    // should return site collection string
                    Region = _tmpSite[0].ToUpper();
                }

                // override the SiteType only if this isn't an ADD-IN
                if (_tmpSiteArr.Length > 1 && !_isaddin)
                {
                    SiteType = _tmpSiteArr[1].ToUpper();
                }
            }
        }
    }
}
