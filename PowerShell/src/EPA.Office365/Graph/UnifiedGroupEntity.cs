using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace EPA.Office365.Graph
{
    /// <summary>
    /// Defines a Unified Group
    /// </summary>
    public class UnifiedGroupEntity : Group
    {
        public UnifiedGroupEntity() : base()
        {
            IsSiteProvisioned = true;
        }

        public UnifiedGroupEntity(Group g) : this()
        {
            Id = g.Id;
            GroupId = g.Id;
            DisplayName = g.DisplayName;
            Description = g.Description;
            MailEnabled = g.MailEnabled;
            Mail = g.Mail;
            MailNickname = g.MailNickname;
            Visibility = g.Visibility;
            AllowExternalSenders = g.AllowExternalSenders;
            AutoSubscribeNewMembers = g.AutoSubscribeNewMembers;
            IsArchived = g.IsArchived;
            UnseenCount = g.UnseenCount;
            Classification = g.Classification;
            HasMembersWithLicenseErrors = g.HasMembersWithLicenseErrors;
            GroupTypes = g.GroupTypes;
            SecurityEnabled = g.SecurityEnabled;
            ProxyAddresses = g.ProxyAddresses;
            RenewedDateTime = g.RenewedDateTime;
            OnPremisesSyncEnabled = g.OnPremisesSyncEnabled;
            IsSubscribedByMail = g.IsSubscribedByMail;
            OnPremisesLastSyncDateTime = g.OnPremisesLastSyncDateTime;
            CreatedDateTime = g.CreatedDateTime;
            DeletedDateTime = g.DeletedDateTime;
        }

        /// <summary>
        /// Unified group id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// Unified Group Creation Date
        /// </summary>
        public DateTime? Created { get; set; }

        /// <summary>
        /// Url of site for the unified group
        /// </summary>
        public string SiteUrl { get; set; }

        /// <summary>
        /// Url of document library for the unified group
        /// </summary>
        public string DocumentsUrl { get; set; }

        /// <summary>
        /// Indication if the Office 365 Group has a Microsoft Team provisioned for it
        /// </summary>
        public bool? HasTeam { get; set; }

        /// <summary>
        /// The Primary Owner of the Group (Created on Behalf Of)
        /// </summary>
        public UnifiedGroupUser PrimaryOwner { get; set; }
        /// <summary>
        /// The Owners of the Group (membership in Owners group)
        /// </summary>
        public List<UnifiedGroupUser> GroupOwners { get; set; } = new List<UnifiedGroupUser>();
        public List<UnifiedGroupUser> GroupMembers { get; internal set; } = new List<UnifiedGroupUser>();

        public DateTime? ReportRefreshDate { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public long? MemberCount { get; set; }
        public long? ExchangeReceivedEmailCount { get; set; }
        public long? SharePointActiveFileCount { get; set; }
        public long? ExchangeMailboxTotalItemCount { get; set; }
        public long? ExchangeMailboxStorageUsed_Byte { get; set; }
        public long? SharePointTotalFileCount { get; set; }
        public long? SharePointSiteStorageUsed_Byte { get; set; }
        public string ReportPeriod { get; set; }

        // Teams Information
        /// <summary>
        /// The Team Display Name
        /// </summary>
        public string TeamDisplayName { get; set; }
        /// <summary>
        /// The Team Description
        /// </summary>
        public string TeamDescription { get; set; }
        /// <summary>
        /// The Internal Team ID
        /// </summary>
        public string TeamInternalId { get; set; }
        /// <summary>
        /// The Team Classification
        /// </summary>
        public string TeamClassification { get; set; }
        /// <summary>
        /// The Team Specialization
        /// </summary>
        public string TeamSpecialization { get; set; }
        /// <summary>
        /// The Team Visibility
        /// </summary>
        public string TeamVisibility { get; set; }
        /// <summary>
        /// The Team Discovery Settings
        /// </summary>
        public string TeamDiscoverySettings { get; set; }
        /// <summary>
        /// The Team Response Headers
        /// </summary>
        public string TeamResponseHeaders { get; set; }
        /// <summary>
        /// The Team Status Code
        /// </summary>
        public string TeamStatusCode { get; set; }
        /// <summary>
        /// Is the Team Archived
        /// </summary>
        public Nullable<bool> TeamIsArchived { get; set; }
        /// <summary>
        /// The Team Web URL
        /// </summary>
        public string TeamWebUrl { get; set; }
        /// <summary>
        /// Has the Team been provisioned by the Site Provisioner
        /// </summary>
        public Nullable<bool> IsSiteProvisioned { get; set; }
    }
}
