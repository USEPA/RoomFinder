namespace OutlookRoomFinder.Core
{
    public class AppSettingsGraph
    {
        /// <summary>
        /// Graph Client Id from AzureAD
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Graph Secret from AzureAD
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Represents the MS Graph Endpoint for Auth and API
        /// </summary>
        /// <remarks>
        /// graph.microsoft.com/beta/
        /// graph.microsoft.com/v1.0/
        /// graph.microsoft.us/beta/
        /// graph.microsoft.us/v1.0/
        /// </remarks>
        public string DefaultEndpoint { get; set; }

        /// <summary>
        /// Default MS Graph Scope for generic authentication
        /// </summary>
        public string DefaultScope { get; set; }
    }
}