using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("scanEPASiteGroupAssociation")]
    public class ScanEPASiteGroupAssociationOptions : TenantCommandOptions
    {
        [Option('s', "site-url", Required = false, SetName = "SiteSearch")]
        public string SiteUrl { get; set; }

        /// <summary>
        /// The action to take
        /// </summary>
        [Option("site-action", Required = false, Default = ScanEPASiteGroupAssociationActions.Repair, HelpText = "Provides option switch for action in the processing.")]
        public ScanEPASiteGroupAssociationActions SiteAction { get; set; }
    }

    public enum ScanEPASiteGroupAssociationActions
    {
        Repair,

        CopyFiles
    }

    public static class ScanEPASiteGroupAssociationExtensions
    {
        public static int RunGenerateAndReturnExitCode(this ScanEPASiteGroupAssociationOptions opts, IAppSettings appSettings)
        {
            var cmd = new ScanEPASiteGroupAssociation(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// This will search the tenant looking for sites that have invalid Ownership groups
    /// </summary>
    /// <remarks>
    ///     ScanEPASiteGroupAssociation --site-url "https://<tenant>.sharepoint.com/sites/<sitename>
    /// </remarks>
    public class ScanEPASiteGroupAssociation : BaseSpoTenantCommand<ScanEPASiteGroupAssociationOptions>
    {
        public ScanEPASiteGroupAssociation(ScanEPASiteGroupAssociationOptions opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var Url = Settings.Commands.TenantAdminUrl;
            var Username = Settings.SpoEpaCredentials.Username;
            var UserSecret = Settings.SpoEpaCredentials.UserSecret;

            LogVerbose($"Username/Password connecting");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Url), Username, UserSecret, false, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        public override int OnRun()
        {
            var _listSites = new List<SPOSiteCollectionModel>();
            if (!string.IsNullOrEmpty(Opts.SiteUrl))
            {
                _listSites.Add(new SPOSiteCollectionModel() { Url = Opts.SiteUrl });
            }
            else
            {
                _listSites = GetSiteCollections();
            }

            foreach (var site in _listSites)
            {
                var siteurl = site.Url;
                ProcessSiteCollection(siteurl, true);
            }

            return 1;
        }


        private void ProcessSiteCollection(string _siteUrl, bool _isRootSite)
        {
            try
            {
                SetSiteAdmin(_siteUrl, CurrentUserName, true);

                using var ctx = this.ClientContext.Clone(_siteUrl);
                Web _web = ctx.Web;
                ctx.Load(_web);
                ctx.Load(_web.Webs);
                ctx.ExecuteQuery();

                if (_isRootSite == false)
                {
                    GetSiteGroupAssociation(ctx);
                }

                foreach (Web _inWeb in _web.Webs)
                {
                    //check if site is app web
                    if (_inWeb.Url.ToLower().IndexOf("https://usepa.sharepoint.com/sites/") > -1)
                    {
                        ProcessSiteCollection(_inWeb.Url, false);
                    }
                }

            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSiteCollection {0}", e.Message);
            }
        }

        private void GetSiteGroupAssociation(ClientContext ctx)
        {
            try
            {
                Web _web = ctx.Web;
                ctx.Load(_web);
                ctx.ExecuteQuery();
                ProcessSiteAssociatedGroups(ctx, _web);
            }
            catch (Exception e)
            {
                LogError(e, "Failed in getSiteGroupAssociation {0}", e.Message);
            }
        }

        /// <summary>
        /// Get the site's group association
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="_web"></param>
        private void ProcessSiteAssociatedGroups(ClientContext ctx, Web _web)
        {
            try
            {

                ctx.Load(_web, i => i.HasUniqueRoleAssignments);
                ctx.ExecuteQuery();

                string _oGroupStr = "";
                string _mGroupStr = "";
                string _vGroupStr = "";

                //Owner Group
                Group _oGroup = _web.AssociatedOwnerGroup;
                Group _mGroup = _web.AssociatedMemberGroup;
                Group _vGroup = _web.AssociatedVisitorGroup;

                ctx.Load(_oGroup);
                ctx.Load(_mGroup);
                ctx.Load(_vGroup);
                ctx.ExecuteQuery();

                try
                {
                    _oGroupStr = _oGroup.Title;
                }
                catch (Exception e)
                {
                    LogError(e, "Failed in processSiteAssociatedGroups Owner=>{0}", e.Message);
                }

                try
                {
                    _mGroupStr = _mGroup.Title;
                }
                catch (Exception e)
                {
                    LogError(e, "Failed in processSiteAssociatedGroups Member=>{0}", e.Message);
                }

                try
                {
                    _vGroupStr = _vGroup.Title;
                }
                catch (Exception e)
                {
                    LogError(e, "Failed in processSiteAssociatedGroups Visitor=>{0}", e.Message);
                }

                string _siteOwners = GetOwners(ctx);

                //if option to set aossication is on
                if (Opts.SiteAction == ScanEPASiteGroupAssociationActions.Repair)
                {
                    if (!_web.HasUniqueRoleAssignments == false)
                    {
                        if ((_oGroupStr == "") || (_mGroupStr == "") || (_vGroupStr == ""))
                        {
                            setSiteGroupAssociation(ctx, _web, _oGroup, _mGroup, _vGroup, _siteOwners);
                        }
                    }
                }
                else
                {
                    LogVerbose("{0},{1},{2},{3},{4},{5},{6}", _web.Url, _web.Title.Replace(",", ""), _oGroupStr, _mGroupStr, _vGroupStr, _web.HasUniqueRoleAssignments, _siteOwners);
                }

            }
            catch (Exception e)
            {
                LogError(e, "Failed in processSiteAssociatedGroups Pulling Membership=>{0}", e.Message);
            }
        }

        /// <summary>
        /// Set the site's group association
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="_web"></param>
        /// <param name="_oGroup"></param>
        /// <param name="_mGroup"></param>
        /// <param name="_vGroup"></param>
        /// <param name="_siteOwners"></param>
        private void setSiteGroupAssociation(ClientContext ctx, Web _web, Group _oGroup, Group _mGroup, Group _vGroup, string _siteOwners)
        {

            string _siteUrlNode = GetSiteUrlEnd(_web.Url);

            string _oGroupStr = "";
            string _mGroupStr = "";
            string _vGroupStr = "";

            try
            {
                _oGroupStr = _oGroup.Title;
            }
            catch (Exception e)
            {
                LogError(e, "Failed in setSiteGroupAssociation Owner=>{0}", e.Message);
            }

            try
            {
                _mGroupStr = _mGroup.Title;
            }
            catch (Exception e)
            {
                LogError(e, "Failed in setSiteGroupAssociation Member=>{0}", e.Message);
            }

            try
            {
                _vGroupStr = _vGroup.Title;
            }
            catch (Exception e)
            {
                LogError(e, "Failed in setSiteGroupAssociation Visitor=>{0}", e.Message);
            }

            GroupCollection _groups = _web.SiteGroups;
            ctx.Load(_groups);
            ctx.ExecuteQuery();

            foreach (Group _group in _groups)
            {
                if ((_group.Title.ToLower().IndexOf(_web.Title.ToLower().Replace(",", " ")) > -1) || (_group.Title.ToLower().IndexOf(_siteUrlNode) > -1))
                {
                    if ((_oGroupStr == "") && (_group.Title.ToLower().IndexOf("owner") > -1))
                    {
                        if (_group.Title.ToLower().IndexOf("owner") > -1)
                        {
                            _web.AssociatedOwnerGroup = _group;
                            _oGroupStr = _group.Title;
                            _web.Update();
                            ctx.ExecuteQuery();
                        }
                    }

                    if ((_mGroupStr == "") && (_group.Title.ToLower().IndexOf("member") > -1))
                    {
                        if (_group.Title.ToLower().IndexOf("member") > -1)
                        {
                            _web.AssociatedMemberGroup = _group;
                            _mGroupStr = _group.Title;
                            _web.Update();
                            ctx.ExecuteQuery();
                        }
                    }

                    if ((_vGroupStr == "") && (_group.Title.ToLower().IndexOf("visitor") > -1))
                    {
                        if (_group.Title.ToLower().IndexOf("visitor") > -1)
                        {
                            _web.AssociatedVisitorGroup = _group;
                            _vGroupStr = _group.Title;
                            _web.Update();
                            ctx.ExecuteQuery();
                        }
                    }
                }

                _web.Update();
                ctx.ExecuteQuery();

                LogVerbose("{0},{1},{2},{3},{4},{5},{6}", _web.Url, _web.Title.Replace(",", ""), _oGroupStr, _mGroupStr, _vGroupStr, _web.HasUniqueRoleAssignments, _siteOwners);
            }

        }

        /// <summary>
        /// Get site owners.. return semi-column seperated list
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private string GetOwners(ClientContext ctx)
        {
            string _ownerStr = "";
            string[] _exceptionList = { "system" };

            try
            {
                Web _web = ctx.Web;
                ctx.Load(_web);
                ctx.ExecuteQuery();

                try
                {
                    Group _oGroup = _web.AssociatedOwnerGroup;
                    UserCollection _owners = _oGroup.Users;

                    ctx.Load(_oGroup);
                    ctx.Load(_owners);
                    ctx.ExecuteQuery();

                    foreach (User _user in _owners)
                    {
                        if ((!_exceptionList.Contains(_user.LoginName.ToLower())) && (_user.LoginName.ToLower().IndexOf("epa") > -1))
                        {
                            _ownerStr += RemoveClaimIdentifier(_user.LoginName) + ";";
                        }
                    }
                }
                catch
                {
                    User _siteOwner = ctx.Site.Owner;
                    ctx.Load(_siteOwner);
                    ctx.ExecuteQuery();
                    _ownerStr = _siteOwner.LoginName + ";";
                }
            }
            catch (Exception e)
            {
                LogError(e, "Failed in getOwners=>{0}", e.Message);
            }
            return _ownerStr;

        }

        /// <summary>
        /// Get the sites' relative url.. get only last node
        /// </summary>
        /// <param name="_siteUrl"></param>
        /// <returns></returns>
        private string GetSiteUrlEnd(string _siteUrl)
        {
            string[] _tmpSite = _siteUrl.ToLower().Replace("https://usepa.sharepoint.com/sites/", "").Split(new char[] { '/' });
            return _tmpSite[_tmpSite.Length - 1];
        }
    }
}
