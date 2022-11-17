using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    public abstract class EntitySkypeForBusinessBase
    {
        public DateTime ReportRefreshDate { get; set; }


        public int ReportPeriod { get; set; }
    }

    public abstract class EntitySkypeForBusinessCountBase : EntitySkypeForBusinessBase
    {
        public DateTime ReportDate { get; set; }
    }

    /// <summary>
    /// Get the trends on how many users organized and participated in conference sessions held in your organization through Skype for Business. The report also includes the number of peer-to-peer sessions.
    /// </summary>
    [Table("GraphSkypeForBusinessActivityActivityCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessActivityActivityCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> PeerToPeer { get; set; }

        public Nullable<Int64> Organized { get; set; }

        public Nullable<Int64> Participated { get; set; }
    }

    /// <summary>
    /// Get the trends on how many unique users organized and participated in conference sessions held in your organization through Skype for Business. The report also includes the number of peer-to-peer sessions.
    /// </summary>
    [Table("GraphSkypeForBusinessActivityUserCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessActivityUserCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> PeerToPeer { get; set; }

        public Nullable<Int64> Organized { get; set; }

        public Nullable<Int64> Participated { get; set; }
    }

    /// <summary>
    /// Get details about Skype for Business activity by user.
    /// </summary>
    [Table("GraphSkypeForBusinessActivityUserDetail", Schema = "dbo")]
    public class EntitySkypeForBusinessActivityUserDetail : EntitySkypeForBusinessBase
    {
        public string UPN { get; set; }

        public string Deleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        public DateTime LastActivityDate { get; set; }

        public Nullable<Int64> TotalPeerToPeerSessionCount { get; set; }

        public Nullable<Int64> TotalOrganizedConferenceCount { get; set; }

        public Nullable<Int64> TotalParticipatedConferenceCount { get; set; }

        public DateTime? PeerToPeerLastActivityDate { get; set; }

        public DateTime? OrganizedConferenceLastActivityDate { get; set; }

        public DateTime? ParticipatedConferenceLastActivityDate { get; set; }

        public Nullable<Int64> PeerToPeerIMCount { get; set; }

        public Nullable<Int64> PeerToPeerAudioCount { get; set; }

        public Nullable<Int64> PeerToPeerAudioMinutes { get; set; }

        public Nullable<Int64> PeerToPeerVideoCount { get; set; }

        public Nullable<Int64> PeerToPeerVideoMinutes { get; set; }

        public Nullable<Int64> PeerToPeerAppSharingCount { get; set; }

        public Nullable<Int64> PeerToPeerFileTransferCount { get; set; }

        public Nullable<Int64> OrganizedConferenceIMCount { get; set; }

        public Nullable<Int64> OrganizedConferenceAudioVideoCount { get; set; }

        public Nullable<Int64> OrganizedConferenceAudioVideoMinutes { get; set; }

        public Nullable<Int64> OrganizedConferenceAppSharingCount { get; set; }

        public Nullable<Int64> OrganizedConferenceWebCount { get; set; }

        public Nullable<Int64> OrganizedConferenceDialInOut3rdPartyCount { get; set; }

        public Nullable<Int64> OrganizedConferenceCloudDialInOutMicrosoftCount { get; set; }

        public Nullable<Int64> OrganizedConferenceCloudDialInMicrosoftMinutes { get; set; }

        public Nullable<Int64> OrganizedConferenceCloudDialOutMicrosoftMinutes { get; set; }

        public Nullable<Int64> ParticipatedConferenceIMCount { get; set; }

        public Nullable<Int64> ParticipatedConferenceAudioVideoCount { get; set; }

        public Nullable<Int64> ParticipatedConferenceAudioVideoMinutes { get; set; }

        public Nullable<Int64> ParticipatedConferenceAppSharingCount { get; set; }

        public Nullable<Int64> ParticipatedConferenceWebCount { get; set; }


        public Nullable<Int64> ParticipatedConferenceDialInOut3rdPartyCount { get; set; }

        public string ProductsAssigned { get; set; }
    }

    /// <summary>
    /// Get the number of users using unique devices in your organization. The report will show you the number of users per device including Windows, Windows phone, Android phone, iPhone, and iPad.
    /// </summary>
    [Table("GraphSkypeForBusinessDeviceUsageDistributionUserCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessDeviceUsageDistributionUserCounts : EntitySkypeForBusinessBase
    {
        public Nullable<Int64> Windows { get; set; }

        public Nullable<Int64> WindowsPhone { get; set; }

        public Nullable<Int64> AndroidPhone { get; set; }

        public Nullable<Int64> iPhone { get; set; }

        public Nullable<Int64> iPad { get; set; }
    }

    /// <summary>
    /// Get the usage trends on how many users in your organization have connected using the Skype for Business app. You will also get a breakdown by the type of device (Windows, Windows phone, Android phone, iPhone, or iPad) on which the Skype for Business client app is installed and used across your organization.
    /// </summary>
    [Table("GraphSkypeForBusinessDeviceUsageUserCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessDeviceUsageUserCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> Windows { get; set; }

        public Nullable<Int64> WindowsPhone { get; set; }

        public Nullable<Int64> AndroidPhone { get; set; }

        public Nullable<Int64> iPhone { get; set; }

        public Nullable<Int64> iPad { get; set; }
    }

    /// <summary>
    /// Get details about Skype for Business device usage by user.
    /// </summary>
    [Table("GraphSkypeForBusinessDeviceUsageUserDetail", Schema = "dbo")]
    public class EntitySkypeForBusinessDeviceUsageUserDetail : EntitySkypeForBusinessBase
    {
        public string UPN { get; set; }

        public DateTime? LastActivityDate { get; set; }

        public bool UsedWindows { get; set; }
        public DateTime? UsedWindowsLastDate { get; set; }

        public bool UsedWindowsPhone { get; set; }
        public DateTime? UsedWindowsPhoneLastDate { get; set; }

        public bool UsedAndroidPhone { get; set; }
        public DateTime? UsedAndroidPhoneLastDate { get; set; }

        public bool UsediPhone { get; set; }
        public DateTime? UsediPhoneLastDate { get; set; }

        public bool UsediPad { get; set; }
        public DateTime? UsediPadLastDate { get; set; }
    }

    /// <summary>
    /// Get usage trends on the number and type of conference sessions held and organized by users in your organization. Types of conference sessions include IM, audio/video, application sharing, web, dial-in/out - 3rd party, and Dial-in/out Microsoft.
    /// </summary>
    [Table("GraphSkypeForBusinessOrganizerActivityCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessOrganizerActivityCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> IM { get; set; }

        public Nullable<Int64> AudioVideo { get; set; }

        public Nullable<Int64> AppSharing { get; set; }

        public Nullable<Int64> Web { get; set; }

        public Nullable<Int64> DialInOut3rdParty { get; set; }

        public Nullable<Int64> DialInOutMicrosoft { get; set; }
    }

    /// <summary>
    /// Get usage trends on the length in minutes and type of conference sessions held and organized by users in your organization. Types of conference sessions include audio/video, and dial-in and dial-out - Microsoft.
    /// </summary>
    [Table("GraphSkypeForBusinessOrganizerActivityMinuteCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessOrganizerActivityMinuteCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> AudioVideo { get; set; }

        public Nullable<Int64> DialInMicrosoft { get; set; }

        public Nullable<Int64> DialOutMicrosoft { get; set; }
    }

    /// <summary>
    /// Get usage trends on the number of unique users and type of conference sessions held and organized by users in your organization. Types of conference sessions include IM, audio/video, application sharing, web, dial-in/out - 3rd party, and dial-in/out Microsoft.
    /// </summary>
    [Table("GraphSkypeForBusinessOrganizerActivityUserCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessOrganizerActivityUserCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> IM { get; set; }

        public Nullable<Int64> AudioVideo { get; set; }

        public Nullable<Int64> AppSharing { get; set; }

        public Nullable<Int64> Web { get; set; }

        public Nullable<Int64> DialInOut3rdParty { get; set; }

        public Nullable<Int64> DialInOutMicrosoft { get; set; }
    }

    /// <summary>
    /// Get usage trends on the number and type of conference sessions that users from your organization participated in. Types of conference sessions include IM, audio/video, application sharing, web, and dial-in/out - 3rd party.
    /// </summary>
    [Table("GraphSkypeForBusinessParticipantActivityCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessParticipantActivityCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> IM { get; set; }

        public Nullable<Int64> AudioVideo { get; set; }

        public Nullable<Int64> AppSharing { get; set; }

        public Nullable<Int64> Web { get; set; }

        public Nullable<Int64> DialInOut3rdParty { get; set; }
    }

    /// <summary>
    /// Get usage trends on the length in minutes and type of conference sessions that users from your organization participated in. Types of conference sessions include audio/video.
    /// </summary>
    [Table("GraphSkypeForBusinessParticipantActivityMinuteCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessParticipantActivityMinuteCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> AudioVideo { get; set; }
    }

    /// <summary>
    /// Get usage trends on the number of unique users and type of conference sessions that users from your organization participated in. Types of conference sessions include IM, audio/video, application sharing, web, and dial-in/out - 3rd party
    /// </summary>
    [Table("GraphSkypeForBusinessParticipantActivityUserCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessParticipantActivityUserCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> IM { get; set; }

        public Nullable<Int64> AudioVideo { get; set; }

        public Nullable<Int64> AppSharing { get; set; }

        public Nullable<Int64> Web { get; set; }

        public Nullable<Int64> DialInOut3rdParty { get; set; }
    }

    /// <summary>
    /// Get usage trends on the number and type of sessions held in your organization. Types of sessions include IM, audio, video, application sharing, and file transfer.
    /// </summary>
    [Table("GraphSkypeForBusinessPeerToPeerActivityCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessPeerToPeerActivityCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> IM { get; set; }

        public Nullable<Int64> Audio { get; set; }

        public Nullable<Int64> Video { get; set; }

        public Nullable<Int64> AppSharing { get; set; }

        public Nullable<Int64> FileTransfer { get; set; }

    }

    /// <summary>
    /// Get usage trends on the length in minutes and type of peer-to-peer sessions held in your organization. Types of sessions include audio and video.
    /// </summary>
    [Table("GraphSkypeForBusinessPeerToPeerActivityMinuteCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessPeerToPeerActivityMinuteCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> Audio { get; set; }

        public Nullable<Int64> Video { get; set; }
    }

    /// <summary>
    /// Get usage trends on the number of unique users and type of peer-to-peer sessions held in your organization. Types of sessions include IM, audio, video, application sharing, and file transfers in peer-to-peer sessions.
    /// </summary>
    [Table("GraphSkypeForBusinessPeerToPeerActivityUserCounts", Schema = "dbo")]
    public class EntitySkypeForBusinessPeerToPeerActivityUserCounts : EntitySkypeForBusinessCountBase
    {
        public Nullable<Int64> IM { get; set; }

        public Nullable<Int64> Audio { get; set; }

        public Nullable<Int64> Video { get; set; }

        public Nullable<Int64> AppSharing { get; set; }

        public Nullable<Int64> FileTransfer { get; set; }
    }
}
