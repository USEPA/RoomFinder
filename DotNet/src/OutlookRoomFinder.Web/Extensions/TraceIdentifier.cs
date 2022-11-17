namespace OutlookRoomFinder.Web.Extensions
{
    /// <summary>
    /// This constant class is used to store trace ids that are returned to the client when an error is handled by <see cref="ExceptionHandlerMiddleware"/>.
    /// The id will be used to associate an error with a code block.
    /// </summary>
    public static class TraceIdentifier
    {
        public const string GraphException = "e88a7351-f118-4cd1-8ff6-752b4aa47904";
        public const string UserNotFoundException = "91D6CA8C-8006-47C4-BB76-1BF433FF8352";
    }
}
