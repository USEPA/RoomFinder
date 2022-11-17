using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Serilog;
using Serilog.Context;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Extensions
{
    [ExcludeFromCodeCoverage]
    public class TelemetryLoggingMiddleware
    {
        private readonly RequestDelegate next;

        public TelemetryLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        [SuppressMessage("Major Code Smell", "S4457:Parameter validation in \"async\"/\"await\" methods should be wrapped", Justification = "Can't be split to resolve coverage.")]
        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            var displayUrl = context.Request.GetDisplayUrl().Split('?')?[0];
            using (LogContext.PushProperty("Telemetry", true))
            using (LogContext.PushProperty("Method", context.Request.Method))
            using (LogContext.PushProperty("QueryString", context.Request.QueryString))
            using (LogContext.PushProperty("DisplayUrl", displayUrl))
            {
                Log.Information(displayUrl);
            }

            // Call the next delegate/middleware in the pipeline
            await next(context);
        }
    }
}
