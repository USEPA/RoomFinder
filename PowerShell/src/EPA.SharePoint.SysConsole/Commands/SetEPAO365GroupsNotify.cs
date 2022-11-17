using CommandLine;
using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    [Verb("SetEPAO365GroupsNotify", HelpText = "The function query the sharepoint list, any group which has missing metadata or hasn't been processed lets email the user.")]
    public class SetEPAO365GroupsNotifyOptions : CommonOptions
    {
        /// <summary>
        /// The site
        /// </summary>
        [Option("site-url", Required = true)]
        public string SiteUrl { get; set; }
    }

    public static class SetEPAO365GroupsNotifyExtension
    {
        public static int RunGenerateAndReturnExitCode(this SetEPAO365GroupsNotifyOptions opts, IAppSettings appSettings)
        {
            var cmd = new SetEPAO365GroupsNotify(opts, appSettings);
            var result = cmd.Run();
            return result;
        }
    }

    /// <summary>
    /// Query the sharepoint list, any group which has missing metadata or hasn't been processed lets email the user
    /// </summary>
    public class SetEPAO365GroupsNotify : BaseSpoCommand<SetEPAO365GroupsNotifyOptions>
    {
        public SetEPAO365GroupsNotify(SetEPAO365GroupsNotifyOptions opts, IAppSettings settings)
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
            var groupListName = Settings.Commands.GroupRequestsListName;
            var list = this.ClientContext.Web.GetListByTitle(groupListName);

            var notificationListName = Settings.Commands.GroupEmailNotificationListName;
            var notificationList = this.ClientContext.Web.GetListByTitle(notificationListName);


            var itemsArray = new List<string>();
            LogVerbose("getGroupfromList ...");
            var viewFields = CAML.ViewFields((new string[] {
                "AAShipRegionOffice",
                "Topics",
                "GroupOwner",
                "GroupOwnerName",
                "EmailAddress"
            }).Select(s => CAML.FieldRef(s)).ToArray());

            var countMe = 0;
            var spQuery = new CamlQuery
            {
                ViewXml = CAML.ViewQuery(ViewScope.RecursiveAll, string.Empty, string.Empty, viewFields, 100)
            };

            while (true)
            {
                var spListItems = list.GetItems(spQuery);
                this.ClientContext.Load(spListItems);
                this.ClientContext.ExecuteQuery();


                foreach (var listItem in spListItems)
                {
                    var listAlpha = listItem.RetrieveListItemValue("AAShipRegionOffice");
                    var topic = listItem.RetrieveListItemValue("Topics");
                    var groupOwners = listItem.RetrieveListItemValue("GroupOwner");
                    var emailAddress = listItem.RetrieveListItemValue("EmailAddress");

                    if (!string.IsNullOrEmpty(listAlpha))
                    {
                        LogVerbose($">>>> {listItem.Id}");
                    }
                    else
                    {
                        LogVerbose($">>>> {listItem.Id}");
                        itemsArray.Add(listItem.Id.ToString());

                        if (!string.IsNullOrEmpty(topic))
                        {
                            countMe++;



                            if (string.IsNullOrEmpty(emailAddress))
                            {

                                LogVerbose($">>>> {listItem.Id} and Owners {groupOwners}");
                                var userValueCollection = new List<FieldUserValue>();
                                var groupOwnersArr = groupOwners.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var ownerString in groupOwnersArr)
                                {

                                    LogVerbose($"Ownerstring {ownerString}");
                                    var _user = this.ClientContext.Web.EnsureUser("i:0#.f|membership|" + ownerString);
                                    this.ClientContext.Load(_user);
                                    this.ClientContext.ExecuteQuery();

                                    LogVerbose($"{_user.LoginName}  -- {_user.Id}");

                                    var fielduser = new FieldUserValue() { LookupId = _user.Id };

                                    userValueCollection.Add(fielduser);
                                }


                                var itemCreateInfo = new ListItemCreationInformation();
                                var spListItem = notificationList.AddItem(itemCreateInfo);
                                spListItem["Title"] = listItem.Id;
                                spListItem["ItemId"] = listItem.Id;
                                spListItem["SendTo"] = userValueCollection;
                                spListItem.Update();
                                this.ClientContext.ExecuteQuery();
                            }
                        }
                    }

                    if (spListItems.ListItemCollectionPosition == null)
                    {
                        break;
                    }
                }

            }
        }
    }
}
