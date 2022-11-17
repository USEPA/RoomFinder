using EPA.Office365.oAuth;
using EPA.SharePoint.SysConsole.Extensions;
using EPA.SharePoint.SysConsole.HttpServices;
using EPA.SharePoint.SysConsole.Models.EzForms;
using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Commands
{
    public abstract class BaseEZFormsRecertification<T> : BaseSpoCommand<T> where T : ICommonOptions
    {
        protected BaseEZFormsRecertification(T opts, IAppSettings settings)
            : base(opts, settings)
        {
        }

        public override void OnInit()
        {
            var Url = Settings.Commands.EZFormsUrl;
            var ClientId = Settings.SpoAddInEZFormAdmin.ClientId;
            var ClientSecret = Settings.SpoAddInEZFormAdmin.ClientSecret;

            Settings.AzureAd.SPClientID = ClientId;
            Settings.AzureAd.SPClientSecret = ClientSecret;

            LogVerbose($"AppCredential connecting and setting Add-In keys");
            SPOnlineConnection.CurrentConnection = SPOnlineConnectionHelper.InstantiateSPOnlineConnection(new Uri(Url), null, ClientId, ClientSecret, Opts.MinimalHealthScore, Opts.RetryCount, Opts.RetryWait, Opts.RequestTimeout);
        }

        /// <summary>
        /// Initializes properties and collections
        /// </summary>
        public override void OnBeforeRun()
        {
            Users = new List<EzFormsUserProfileModel>();

            var BCCUsers = Settings.Commands.EZFormsBCCUsers;
            if (!string.IsNullOrEmpty(BCCUsers))
            {
                EmailBCC = BCCUsers.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                EmailBCC = new string[] { "sharepointadmin@usepa.onmicrosoft.com" };
            }

            // SCHEDULES: 4380 (6 months) -- DEV/TEST default ===> 2 days
            var SixMonths = Settings.Commands.EZFormsSchedulesSixMonths.ToInt32(48);
            SixMonthsSpan = new TimeSpan(SixMonths, 0, 0);

            // SCHEDULES: 720 (30 days) -- DEV/TEST default ===> 1 1/2 days
            var ThirtyDays = Settings.Commands.EZFormsSchedulesThirtyDays.ToInt32(31);
            ThirtyDaysSpan = new TimeSpan(ThirtyDays, 0, 0);

            // SCHEDULES: 168 (7 days) -- DEV/TEST default ===> 8 hours
            var SevenDays = Settings.Commands.EZFormsSchedulesSevenDays.ToInt32(8);
            SevenDaysSpan = new TimeSpan(SevenDays, 0, 0);

            // SCHEDULES: 24 (1 day) -- DEV/TEST default ===> 2 hours
            var OneDay = Settings.Commands.EZFormsSchedulesOneDay.ToInt32(2);
            OneDaySpan = new TimeSpan(OneDay, 0, 0);

            // Get UTC Time
            UtcIsoDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Holds user profile objects to avoid lookup for existing collection users
        /// </summary>
        internal protected List<EzFormsUserProfileModel> Users { get; set; }


        internal protected List<string> AccessRequestCamlFieldRefs
        {

            get
            {
                return new List<string>() {
                   EzForms_AccessRequest.Field_Request_x0020_Type,
                   EzForms_AccessRequest.Field_Request_x0020_Status,
                   EzForms_AccessRequest.Field_Routing_x0020_Phase,
                   EzForms_AccessRequest.Field_Employee,
                   EzForms_AccessRequest.Field_Office,
                   EzForms_AccessRequest.Field_Office_x0020_Acronym,
                   EzForms_AccessRequest.Field_ezLanIdText,
                   EzForms_AccessRequest.Field_ezFormsADAccount,
                   EzForms_AccessRequest.Field_ezformsWorkforceID,
                   EzForms_AccessRequest.Field_Division_x0020_Director_x0020_or,
                   EzForms_AccessRequest.Field_ezformsNextCertifyDate,
                   EzForms_AccessRequest.Field_Computer_x0020_Name,
                   EzForms_AccessRequest.Field_Request_x0020_Date,
                   EzForms_AccessRequest.Field_ezPersonnelBool,
                   EzForms_AccessRequest.Field_Activity_x0020_Log
                };
            }
        }

        /// <summary>
        /// BCC email addresses for administrative purposes
        /// </summary>
        internal protected IList<string> EmailBCC
        {
            get; private set;
        }

        /// <summary>
        /// The total time to advance the clock for queries
        /// </summary>
        internal protected TimeSpan SixMonthsSpan { get; private set; }

        internal protected TimeSpan ThirtyDaysSpan { get; private set; }

        internal protected TimeSpan SevenDaysSpan { get; private set; }

        internal protected TimeSpan OneDaySpan { get; private set; }

        /// <summary>
        /// yyyy-MM-ddT12:00:00Z -- ISO on-set timestamp
        /// </summary>
        internal protected string ZeroHourFormatString
        {
            get
            {
                return "yyyy-MM-ddT00:00:00Z";
            }
        }

        /// <summary>
        /// yyyy-MM-ddTHH:mm:ssZ -- ISO current timestamp
        /// </summary>
        internal protected string HourFormatString
        {
            get
            {
                return "yyyy-MM-ddTHH:mm:ssZ";
            }
        }

        /// <summary>
        /// Contains the UTCNow ISO Standard Date format
        /// </summary>
        internal protected DateTime UtcIsoDate { get; set; }


        internal protected EzFormsUserProfileModel GetUserInformation(Microsoft.SharePoint.Client.UserProfiles.PeopleManager peopleContext, string upn)
        {
            var model = new EzFormsUserProfileModel();

            if (string.IsNullOrEmpty(upn))
            {
                return model;
            }


            if (Users.Any(u => u.UPN.Equals(upn, StringComparison.CurrentCultureIgnoreCase)))
            {
                return Users.FirstOrDefault(f => f.UPN.Equals(upn, StringComparison.CurrentCultureIgnoreCase));
            }

            try
            {
                var userPrincipalName = $"{ClaimIdentifier}|{upn}";
                var personProperties = peopleContext.GetPropertiesFor(userPrincipalName);
                ClientContext.Load(personProperties);
                ClientContext.ExecuteQueryRetry();

                if (personProperties != null && personProperties.UserProfileProperties != null)
                {
                    var profileProperties = personProperties.UserProfileProperties.ToList();
                    model.UPN = userPrincipalName;
                    model.FirstName = GetPropertyValue(profileProperties, "FirstName");
                    model.LastName = GetPropertyValue(profileProperties, "LastName");
                    model.DisplayName = GetPropertyValue(profileProperties, "PreferredName");
                    model.Email = GetPropertyValue(profileProperties, "WorkEmail");
                    model.AD_GUID = GetPropertyValue(profileProperties, "ADGuid");
                    model.UserProfile_GUID = GetPropertyValue(profileProperties, "UserProfile_GUID");
                    model.UserName = GetPropertyValue(profileProperties, "UserName");
                    model.SPS_DistinguishedName = GetPropertyValue(profileProperties, "SPS-DistinguishedName");
                    model.SID = GetPropertyValue(profileProperties, "SID");
                    model.SPS_ClaimID = GetPropertyValue(profileProperties, "SPS-ClaimID");
                    model.msOnline_ObjectId = GetPropertyValue(profileProperties, "msOnline-ObjectId");
                    model.Department = GetPropertyValue(profileProperties, "Department");
                    model.SPS_Department = GetPropertyValue(profileProperties, "SPS-Department");
                    model.SPS_JobTitle = GetPropertyValue(profileProperties, "SPS-JobTitle");
                    model.WorkPhone = GetPropertyValue(profileProperties, "WorkPhone");
                    model.Manager = GetPropertyValue(profileProperties, "Manager");
                }
            }
            catch (Exception ex)
            {
                LogWarning("User {0} could not be found in profile store {1}.", upn, ex.Message);
            }
            return model;
        }

        /// <summary>
        /// Pull property value from collection of KeyValuePair properties
        /// </summary>
        /// <param name="profileProperties"></param>
        /// <param name="propertyName"></param>
        /// <param name="valueType"></param>
        /// <returns></returns>
        internal protected string GetPropertyValue(List<KeyValuePair<string, string>> profileProperties, string propertyName, Type valueType = null)
        {
            var property = profileProperties.FirstOrDefault(f => f.Key.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase));
            if (!property.Equals(default(KeyValuePair<string, string>)))
            {
                if (valueType != null && valueType == typeof(System.Guid))
                {
                    var propValue = new Guid(property.Value);
                    return propValue.ToString("D");
                }
                return property.Value;
            }
            return string.Empty;
        }

        /// <summary>
        /// Parses the Activity Log into objects
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="requestId"></param>
        /// <returns></returns>
        internal List<EZFormsActivityLogModel> RetrieveActivityLog(ListItem requestItem, int requestId)
        {
            // NEW: Routing Phase#|#Approved#|#Approver#|#Next Approver#|#Date#|#--#|#-- 
            // OLD: Routing-Phase - Action by Approver on Date/n
            var activities = new List<EZFormsActivityLogModel>();

            var activityLog = requestItem.RetrieveListItemValue(EzForms_AccessRequest.Field_Activity_x0020_Log);
            if (string.IsNullOrEmpty(activityLog))
            {
                LogWarning("no activity log for id:{0}", requestId);
                return activities;
            }


            if (activityLog.IndexOf(@"#|#", StringComparison.CurrentCultureIgnoreCase) > 0)
            {
                var newrows = activityLog.Split(new string[] { "--#|#--" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var newitem in newrows)
                {
                    var _lineApproval = newitem.Split(new string[] { "#|#" }, StringSplitOptions.None);
                    if (_lineApproval.Count() > 4)
                    {
                        /*
                        0:Phase  1:Status  2:Approver  3:Next Approver  4:Date
                        */
                        var _phase = _lineApproval[0];
                        var _status = _lineApproval[1];
                        var _approver = _lineApproval[2];
                        var _rerouted = _lineApproval[3];
                        var _date = _lineApproval[4];
                        var _mod = new EZFormsActivityLogModel()
                        {
                            RoutingPhase = _phase,
                            ApproverAction = _status,
                            UserTitle = _approver,
                            AlternateTitle = _rerouted,
                            ActivityDate = _date
                        };

                        activities.Add(_mod);
                    }
                    else
                    {
                        LogWarning("Invalid string for row {0}", newitem);
                    }
                }
            }
            else
            {
                var oldrows = activityLog.Trim().Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var olditem in oldrows)
                {
                    /* 
                    before ' - ' Phase 
                    before ' by ': Status   
                    before ' on ': Approver  
                    before line break: Date

    Supervisor/Director - Rerouted to Brzezinski, David; by Zarczynski, Richard on 02/26/2016 
    Supervisor/Director - Approved by Brzezinski, David on 02/26/2016 
    Information Security Officer - Approved by Brzezinski, David onbehalf of Galano, Fidel on 02/26/2016 
    Information Management Officer - Denied by Dollison, Lawrence A. on 02/29/2016 
                    Business Relationship Manager - Approved by Oldham, William onbehalf of Green, Tia on 03/18/2016  */
                    var _phase = string.Empty;
                    var _status = string.Empty;
                    var _approver = string.Empty;
                    var _reroutedTo = string.Empty; // holds the user
                    var _date = string.Empty;

                    var _lineApproval = olditem.Split(new string[] { " - " }, StringSplitOptions.None);
                    if (_lineApproval.Length > 0)
                    {
                        // construct the model
                        var _mod = new EZFormsActivityLogModel();

                        _phase = _lineApproval[0];

                        var _dateSplit = _lineApproval[1].Split(new string[] { " on " }, StringSplitOptions.None);
                        if (_dateSplit.Length > 1)
                        {
                            _date = _dateSplit[1].Trim();
                            var _phaseAfter = _dateSplit[0].Trim();

                            var _phaseBy = _phaseAfter.Split(new string[] { " by ", " to " }, StringSplitOptions.None);
                            if (_phaseBy.Length > 0)
                            {
                                _status = _phaseBy[0].Trim();

                                var reroutedtext = _phaseAfter;
                                var byActionSplit = _phaseAfter.Split(new string[] { " by " }, StringSplitOptions.None);
                                if (byActionSplit.Length > 0)
                                {
                                    reroutedtext = byActionSplit[0].Trim();
                                    if (byActionSplit.Length > 1)
                                    {
                                        _approver = byActionSplit[1].Trim();
                                    }
                                }

                                var toActionSplit = reroutedtext.Split(new string[] { " to " }, StringSplitOptions.None);
                                if (toActionSplit.Length > 1)
                                {
                                    _reroutedTo = toActionSplit[1].Trim();
                                }
                            }
                        }
                        else
                        {
                            LogWarning("no date found for {0}", olditem);
                        }

                        _mod.RoutingPhase = _phase;
                        _mod.ApproverAction = _status;
                        _mod.UserTitle = _approver;
                        _mod.AlternateTitle = _reroutedTo;
                        _mod.ActivityDate = _date;
                        activities.Add(_mod);
                    }
                    else
                    {
                        LogVerbose("Invalid string for row {0}", activityLog);
                    }
                }
            }

            return activities;
        }

    }
}
