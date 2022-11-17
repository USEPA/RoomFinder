using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EPA.Office365.Database
{
    /// <summary>
    /// point to the class that inherit from DbConfiguration
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Extension method handles exceptions.")]
    public class AnalyticDbContext : DbContext
    {
        public AnalyticDbContext(DbContextOptions<AnalyticDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Contains the date/time of various report runs
        /// </summary>
        public DbSet<EntityTenantDates> EntitiesTenantDates { get; set; }

        /// <summary>
        /// Contains the site collection entities
        /// </summary>
        public DbSet<EntityTenantSiteAnalytics> EntitiesSiteCollection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<EntityTenantWebAddIn> EntitiesWebAddIn { get; set; }

        /// <summary>
        /// Contains the site entities
        /// </summary>
        public DbSet<EntityTenantWeb> EntitiesWebs { get; set; }

        /// <summary>
        /// Represents a SPWeb and its sharing status
        /// </summary>
        public DbSet<EntityTenantWebSharing> EntitiesWebSharing { get; set; }

        /// <summary>
        /// Contains the site entities pages
        /// </summary>
        public DbSet<EntityTenantSite> EntitiesSites { get; set; }

        /// <summary>
        /// Contains the site list entities
        /// </summary>
        public DbSet<EntityTenantSiteListing> EntitiesSiteListing { get; set; }

        /// <summary>
        /// Site mailboxes [governance rules]
        /// </summary>
        public DbSet<EntityTenantSiteMailboxes> EntitiesSiteMailboxes { get; set; }

        /// <summary>
        /// contains all the user profiles in the system
        /// </summary>
        public DbSet<EntityAnalyticsUserProfiles> EntitiesUserProfile { get; set; }

        /// <summary>
        /// contains the user profile subsites that should be removed
        /// </summary>
        public DbSet<EntityAnalyticsUserProfilesSubsites> EntitiesUserProfileSubsites { get; set; }

        /// <summary>
        /// contains all the external profiles in the system
        /// </summary>
        public DbSet<EntityAnalyticsExternalUsers> EntitiesExternalUsers { get; set; }

        /// <summary>
        /// contains all the external profiles in the site collections
        /// </summary>
        public DbSet<EntityAnalyticsExternalUsersSites> EntitiesExternalUsersSites { get; set; }

        /// <summary>
        /// contains all the O365 Groups in the system
        /// </summary>
        public DbSet<EntityAnalyticsUnifiedGroup> EntitiesUnifiedGroup { get; set; }

        /// <summary>
        /// The office/organizations of the EPA
        /// </summary>
        public DbSet<EntityOffice> OfficeEntities { get; set; }

        /// <summary>
        /// The eBusiness information for the EPA
        /// </summary>
        public DbSet<EntityEBusinessAccounts> EBusinessAccounts { get; set; }

        public DbSet<EntityGraphO365ActiveUserDetails> EntitiesPreviewActiveUserDetail { get; set; }
        public DbSet<EntityGraphO365ActiveUsers> EntitiesPreviewActiveUser { get; set; }
        public DbSet<EntityGraphO365ActiveUserService> EntitiesPreviewActiveUserService { get; set; }



        public DbSet<EntityO365ReportODFBDeployedMonthly> EntitiesODFBDeployedMonthly { get; set; }
        public DbSet<EntityO365ReportODFBDeployedWeekly> EntitiesODFBDeployedWeekly { get; set; }
        public DbSet<EntityO365ReportODFBStorageMonthly> EntitiesODFBStorageMonthly { get; set; }
        public DbSet<EntityO365ReportODFBStorageWeekly> EntitiesODFBStorageWeekly { get; set; }

        public DbSet<EntityO365OneDriveDates> EntitiesO365OneDriveDates { get; set; }
        public DbSet<EntityGraphOneDriveActivityDetail> EntitiesPreviewOneDriveActivityDetail { get; set; }
        public DbSet<EntityGraphOneDriveActivityFiles> EntitiesPreviewOneDriveActivityFiles { get; set; }
        public DbSet<EntityGraphOneDriveActivityUsers> EntitiesPreviewOneDriveActivityUsers { get; set; }
        public DbSet<EntityGraphOneDriveUsageAccount> EntitiesPreviewOneDriveUsageAccount { get; set; }
        public DbSet<EntityGraphOneDriveUsageDetail> EntitiesPreviewOneDriveUsageDetail { get; set; }
        public DbSet<EntityGraphOneDriveUsageStorage> EntitiesGraphOneDriveUsageStorage { get; set; }
        public DbSet<EntityGraphOneDriveUsageFileCounts> EntitiesGraphOneDriveUsageFileCounts { get; set; }



        public DbSet<EntityO365ReportSPOActiveUsersMonthly> EntitiesSPOActiveUsersMonthly { get; set; }
        public DbSet<EntityO365ReportSPOActiveUsersWeekly> EntitiesSPOActiveUsersWeekly { get; set; }
        public DbSet<EntityO365ReportSPOTenantStorageMonthly> EntitiesTenantStorageMonthly { get; set; }
        public DbSet<EntityO365ReportSPOTenantStorageWeekly> EntitiesTenantStorageWeekly { get; set; }
        public DbSet<EntityO365ReportSPOConferenceWeekly> EntitiesSPOConferenceWeekly { get; set; }
        public DbSet<EntityO365ReportSPOConnectionClientWeekly> EntitiesSPOConnectionClientWeekly { get; set; }
        public DbSet<EntityO365ReportSPOClientSoftwareBrowser> EntitiesSPOClientSoftwareBrowser { get; set; }

        public DbSet<EntityGraphSharePointSiteUsageDetail> EntitiesGraphSharePointSiteUsageDetail { get; set; }
        public DbSet<EntityGraphSharePointSiteUsageSiteCounts> EntitiesGraphSharePointSiteUsageSiteCounts { get; set; }
        public DbSet<EntityGraphSharePointSiteUsageStorage> EntitiesGraphSharePointSiteUsageStorage { get; set; }
        public DbSet<EntityGraphSharePointSiteUsageFileCounts> EntitiesGraphSharePointSiteUsageFileCounts { get; set; }
        public DbSet<EntityGraphSharePointSiteUsagePages> EntitiesGraphSharePointSiteUsagePages { get; set; }
        public DbSet<EntityGraphSharePointActivityFileCounts> EntitiesGraphSharePointActivityFileCounts { get; set; }
        public DbSet<EntityGraphSharePointActivityUserCounts> EntitiesGraphSharePointActivityUserCounts { get; set; }
        public DbSet<EntityGraphSharePointActivityPagesCounts> EntitiesGraphSharePointActivityPagesCounts { get; set; }
        public DbSet<EntityGraphSharePointActivityUserDetail> EntitiesGraphSharePointActivityUserDetail { get; set; }



        public DbSet<EntitySkypeForBusinessActivityActivityCounts> EntitiesEntitySkypeForBusinessActivityActivityCounts { get; set; }
        public DbSet<EntitySkypeForBusinessActivityUserCounts> EntitiesEntitySkypeForBusinessActivityUserCounts { get; set; }
        public DbSet<EntitySkypeForBusinessActivityUserDetail> EntitiesEntitySkypeForBusinessActivityUserDetail { get; set; }
        public DbSet<EntitySkypeForBusinessDeviceUsageDistributionUserCounts> EntitiesEntitySkypeForBusinessDeviceUsageDistributionUserCounts { get; set; }
        public DbSet<EntitySkypeForBusinessDeviceUsageUserCounts> EntitiesEntitySkypeForBusinessDeviceUsageUserCounts { get; set; }
        public DbSet<EntitySkypeForBusinessDeviceUsageUserDetail> EntitiesEntitySkypeForBusinessDeviceUsageUserDetail { get; set; }
        public DbSet<EntitySkypeForBusinessOrganizerActivityCounts> EntitiesEntitySkypeForBusinessOrganizerActivityCounts { get; set; }
        public DbSet<EntitySkypeForBusinessOrganizerActivityMinuteCounts> EntitiesEntitySkypeForBusinessOrganizerActivityMinuteCounts { get; set; }
        public DbSet<EntitySkypeForBusinessOrganizerActivityUserCounts> EntitiesEntitySkypeForBusinessOrganizerActivityUserCounts { get; set; }
        public DbSet<EntitySkypeForBusinessParticipantActivityCounts> EntitiesEntitySkypeForBusinessParticipantActivityCounts { get; set; }
        public DbSet<EntitySkypeForBusinessParticipantActivityMinuteCounts> EntitiesEntitySkypeForBusinessParticipantActivityMinuteCounts { get; set; }
        public DbSet<EntitySkypeForBusinessParticipantActivityUserCounts> EntitiesEntitySkypeForBusinessParticipantActivityUserCounts { get; set; }
        public DbSet<EntitySkypeForBusinessPeerToPeerActivityCounts> EntitiesEntitySkypeForBusinessPeerToPeerActivityCounts { get; set; }
        public DbSet<EntitySkypeForBusinessPeerToPeerActivityMinuteCounts> EntitiesEntitySkypeForBusinessPeerToPeerActivityMinuteCounts { get; set; }
        public DbSet<EntitySkypeForBusinessPeerToPeerActivityUserCounts> EntitiesEntitySkypeForBusinessPeerToPeerActivityUserCounts { get; set; }

        public DbSet<EntityMSTeamsActivityActivityCounts> EntitiesEntityMSTeamsActivityActivityCounts { get; set; }
        public DbSet<EntityMSTeamsActivityUserCounts> EntitiesEntityMSTeamsActivityUserCounts { get; set; }
        public DbSet<EntityMSTeamsActivityUserDetail> EntitiesEntityMSTeamsActivityUserDetail { get; set; }
        public DbSet<EntityMSTeamsDeviceUsageDistributionUserCounts> EntitiesEntityMSTeamsDeviceUsageDistributionUserCounts { get; set; }
        public DbSet<EntityMSTeamsDeviceUsageUserCounts> EntitiesEntityMSTeamsDeviceUsageUserCounts { get; set; }
        public DbSet<EntityMSTeamsDeviceUsageUserDetail> EntitiesEntityMSTeamsDeviceUsageUserDetail { get; set; }

        /// <summary>
        /// Fluent API configuration of the reporting database
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EntityTenantSiteAnalytics>().Property(x => x.Storage_Allocated_MB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityTenantSiteAnalytics>().Property(x => x.Storage_Allocated_GB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityTenantSiteAnalytics>().Property(x => x.Storage_Usage_MB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityTenantSiteAnalytics>().Property(x => x.Storage_Usage_GB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityTenantSiteAnalytics>().Property(x => x.Storage_Used_Perct).HasColumnType("decimal(18, 4)");
            modelBuilder.Entity<EntityTenantWeb>().Property(prop => prop.DocumentActivityStatus).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityTenantWeb>().Property(prop => prop.SiteActivity).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityAnalyticsUserProfiles>().Property(x => x.StorageAllocated).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityAnalyticsUserProfiles>().Property(x => x.StorageUsed).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityAnalyticsUserProfiles>().Property(x => x.StorageUsedPerct).HasColumnType("decimal(18, 4)");
            modelBuilder.Entity<EntityAnalyticsUnifiedGroup>().Property(x => x.StorageAllocatedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityAnalyticsUnifiedGroup>().Property(x => x.StorageUsedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityAnalyticsUnifiedGroup>().Property(x => x.StorageUsedPercent).HasColumnType("decimal(18, 4)");
            modelBuilder.Entity<EntityO365ReportODFBStorageMonthly>().Property(x => x.StorageAllocatedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageMonthly>().Property(x => x.StorageAllocatedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageMonthly>().Property(x => x.StorageAllocatedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageMonthly>().Property(x => x.StorageUsedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageMonthly>().Property(x => x.StorageUsedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageMonthly>().Property(x => x.StorageUsedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageWeekly>().Property(x => x.StorageAllocatedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageWeekly>().Property(x => x.StorageAllocatedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageWeekly>().Property(x => x.StorageAllocatedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageWeekly>().Property(x => x.StorageUsedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageWeekly>().Property(x => x.StorageUsedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportODFBStorageWeekly>().Property(x => x.StorageUsedTB).HasColumnType("decimal(18, 6)");


            modelBuilder.Entity<EntityGraphOneDriveActivityDetail>().HasKey(hk => new { hk.LastActivityDate, hk.UPN });
            modelBuilder.Entity<EntityGraphOneDriveActivityDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphOneDriveActivityFiles>().HasKey(hk => hk.LastActivityDate);
            modelBuilder.Entity<EntityGraphOneDriveActivityFiles>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphOneDriveActivityUsers>().HasKey(hk => hk.LastActivityDate);
            modelBuilder.Entity<EntityGraphOneDriveActivityUsers>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphOneDriveUsageAccount>().HasKey(hk => new { hk.LastActivityDate, hk.SiteType });
            modelBuilder.Entity<EntityGraphOneDriveUsageAccount>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphOneDriveUsageDetail>().HasKey(hk => new { hk.LastActivityDate, hk.SiteURL });
            modelBuilder.Entity<EntityGraphOneDriveUsageDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphOneDriveUsageStorage>().HasKey(hk => new { hk.LastActivityDate, hk.SiteType });
            modelBuilder.Entity<EntityGraphOneDriveUsageStorage>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphOneDriveUsageFileCounts>().HasKey(hk => new { hk.ReportDate, hk.SiteType });
            modelBuilder.Entity<EntityGraphOneDriveUsageFileCounts>().Property(x => x.ReportDate).HasColumnType("datetime");

            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageAllocatedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageAllocatedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageAllocatedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageUsedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageUsedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageUsedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageMonthly>().Property(x => x.StorageTotal).HasColumnType("decimal(18, 6)");

            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageAllocatedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageAllocatedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageAllocatedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageUsedMB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageUsedGB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageUsedTB).HasColumnType("decimal(18, 6)");
            modelBuilder.Entity<EntityO365ReportSPOTenantStorageWeekly>().Property(x => x.StorageTotal).HasColumnType("decimal(18, 6)");

            modelBuilder.Entity<EntityGraphSharePointSiteUsageDetail>().HasKey(hk => new { hk.LastActivityDate, hk.SiteURL });
            modelBuilder.Entity<EntityGraphSharePointSiteUsageDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointSiteUsageSiteCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityGraphSharePointSiteUsageSiteCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointSiteUsageStorage>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityGraphSharePointSiteUsageStorage>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointSiteUsageFileCounts>().HasKey(hk => new { hk.ReportDate, hk.SiteType });
            modelBuilder.Entity<EntityGraphSharePointSiteUsageFileCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointSiteUsagePages>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityGraphSharePointSiteUsagePages>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointActivityFileCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityGraphSharePointActivityFileCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointActivityUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityGraphSharePointActivityUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointActivityPagesCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityGraphSharePointActivityPagesCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityGraphSharePointActivityUserDetail>().HasKey(hk => new { hk.LastActivityDate, hk.UserPrincipalName });
            modelBuilder.Entity<EntityGraphSharePointActivityUserDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");

            modelBuilder.Entity<EntitySkypeForBusinessActivityActivityCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessActivityActivityCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessActivityUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessActivityUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessActivityUserDetail>().HasKey(hk => new { hk.UPN, hk.LastActivityDate });
            modelBuilder.Entity<EntitySkypeForBusinessActivityUserDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessDeviceUsageDistributionUserCounts>().HasKey(hk => new { hk.ReportRefreshDate });
            modelBuilder.Entity<EntitySkypeForBusinessDeviceUsageDistributionUserCounts>().Property(x => x.ReportRefreshDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessDeviceUsageUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessDeviceUsageUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessDeviceUsageUserDetail>().HasKey(hk => hk.UPN);
            modelBuilder.Entity<EntitySkypeForBusinessDeviceUsageUserDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessOrganizerActivityCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessOrganizerActivityCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessOrganizerActivityMinuteCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessOrganizerActivityMinuteCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessOrganizerActivityUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessOrganizerActivityUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessParticipantActivityCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessParticipantActivityCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessParticipantActivityMinuteCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessParticipantActivityMinuteCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessParticipantActivityUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessParticipantActivityUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessPeerToPeerActivityCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessPeerToPeerActivityCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessPeerToPeerActivityMinuteCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessPeerToPeerActivityMinuteCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntitySkypeForBusinessPeerToPeerActivityUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntitySkypeForBusinessPeerToPeerActivityUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");

            modelBuilder.Entity<EntityMSTeamsActivityActivityCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityMSTeamsActivityActivityCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityMSTeamsActivityUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityMSTeamsActivityUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityMSTeamsActivityUserDetail>().HasKey(hk => new { hk.UPN, hk.LastActivityDate });
            modelBuilder.Entity<EntityMSTeamsActivityUserDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityMSTeamsDeviceUsageDistributionUserCounts>().HasKey(hk => new { hk.ReportRefreshDate });
            modelBuilder.Entity<EntityMSTeamsDeviceUsageDistributionUserCounts>().Property(x => x.ReportRefreshDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityMSTeamsDeviceUsageUserCounts>().HasKey(hk => new { hk.ReportDate });
            modelBuilder.Entity<EntityMSTeamsDeviceUsageUserCounts>().Property(x => x.ReportDate).HasColumnType("datetime");
            modelBuilder.Entity<EntityMSTeamsDeviceUsageUserDetail>().HasKey(hk => new { hk.UPN });
            modelBuilder.Entity<EntityMSTeamsDeviceUsageUserDetail>().Property(x => x.LastActivityDate).HasColumnType("datetime");
        }

        /// <summary>
        /// Synchronous persistance of entities
        /// </summary>
        /// <returns></returns>
        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (Exception ex)
            {
                ex.HandleExceptionAndRethrow();
            }

            return 0;
        }

        /// <summary>
        /// Allows the system to async assert records into the data mart
        /// </summary>
        /// <returns></returns>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ex.HandleExceptionAndRethrow();
            }

            return Task.Run(() => { return 0; });
        }
    }

}
