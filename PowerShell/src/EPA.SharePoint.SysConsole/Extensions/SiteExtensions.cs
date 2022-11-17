using EPA.Office365.Extensions;
using EPA.SharePoint.SysConsole.Models;
using Microsoft.SharePoint.Client;
using System;

namespace EPA.SharePoint.SysConsole.Extensions
{
    /// <summary>
    /// Provides extension methods to inject capability into a [Site]
    /// </summary>
    public static class SiteExtensions
    {

        /// <summary>
        /// Extracts the Site Usage
        /// </summary>
        /// <remarks>
        /// If the Usage property has not be instantiated it will load the property into memory; 
        ///     Note: This will slow the processing
        /// </remarks>
        /// <param name="site"></param>
        /// <returns></returns>
        public static SPSiteUsageModel GetSiteUsageMetric(this Site site)
        {

            if (!site.IsPropertyAvailable(sctx => sctx.Usage))
            {
                site.EnsureProperty(sctx => sctx.Usage);
            }

            var byteAddressSpace = 1024;
            var mbAddressSpace = 1048576;

            UsageInfo _usageInfo = site.Usage;
            Double _storageBytes = _usageInfo.Storage;
            var _storage = (_storageBytes / mbAddressSpace);
            Double _storageQuotaBytes = (_usageInfo.StoragePercentageUsed > 0) ? _storageBytes / _usageInfo.StoragePercentageUsed : 0;
            var _storageQuota = (_storageQuotaBytes / mbAddressSpace);

            var _storageUsageMB = Math.Round(_storage, 4);
            var _storageAllocatedMB = Math.Round(_storageQuota, 4);

            var _storageUsageGB = Math.Round((_storage / byteAddressSpace), 4);
            var _storageAllocatedGB = Math.Round((_storageQuota / byteAddressSpace), 4);

            var _storagePercentUsed = Math.Round(_usageInfo.StoragePercentageUsed, 4);


            var storageModel = new SPSiteUsageModel()
            {
                Bandwidth = _usageInfo.Bandwidth,
                DiscussionStorage = _usageInfo.DiscussionStorage,
                Hits = _usageInfo.Hits,
                Visits = _usageInfo.Visits,
                StorageQuotaBytes = _storageQuotaBytes,
                AllocatedGb = _storageAllocatedGB,
                AllocatedGbDecimal = _storageAllocatedGB.TryParseDecimal(0),
                UsageGb = _storageUsageGB,
                UsageGbDecimal = _storageUsageGB.TryParseDecimal(0),
                AllocatedMb = _storageAllocatedMB,
                AllocatedMbDecimal = _storageAllocatedMB.TryParseDecimal(0),
                UsageMb = _storageUsageMB,
                UsageMbDecimal = _storageUsageMB.TryParseDecimal(0),
                StorageUsedPercentage = _storagePercentUsed,
                StorageUsedPercentageDecimal = _storagePercentUsed.TryParseDecimal(0)
            };

            return storageModel;
        }
    }
}
