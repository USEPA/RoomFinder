using Microsoft.AspNetCore.Builder;

namespace OutlookRoomFinder.Web.Extensions
{
    public static class TelemetryLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseTelemetryLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TelemetryLoggingMiddleware>();
        }
    }
}
