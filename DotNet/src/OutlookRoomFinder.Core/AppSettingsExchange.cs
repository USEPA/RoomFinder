namespace OutlookRoomFinder.Core
{
    public class AppSettingsExchange
    {
        public int CacheLifetime { get; set; }

        public int DefaultRoomCapacity { get; set; }

        public int GetUserAvailabilityBatchSize { get; set; }

        public string JsonFilename { get; set; }
    }
}