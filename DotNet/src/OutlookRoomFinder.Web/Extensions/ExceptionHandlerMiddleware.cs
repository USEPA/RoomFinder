using Microsoft.AspNetCore.Http;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Exceptions;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Extensions
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Suppressing as this is the Exception handler.")]
    /// <summary>
    /// This exception handler middleware is used to translate unhanbled exception (500 internal server error) to more useful status codes for the client
    /// and developers to troubleshoot.  This is an initial list of handled errors.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ExceptionHandlerMiddleware
    {
        protected ILogger Logger { get; }
        private readonly RequestDelegate next;

        public ExceptionHandlerMiddleware(RequestDelegate next, ILogger logger)
        {
            this.next = next;
            this.Logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await next(httpContext);
            }
            catch (Newtonsoft.Json.JsonSerializationException serialization)
            {
                string message = string.Empty;

                if (!string.IsNullOrEmpty(serialization.Message))
                {
                    message = $"trace ID: '{serialization.Message}'";
                }

                await ProcessExceptionAsync(httpContext, serialization.HResult, serialization, message);
            }
            catch (HttpStatusCodeException hsce)
            {
                string message = string.Empty;

                if (!string.IsNullOrEmpty(hsce.Message))
                {
                    message = $"trace ID: '{hsce.Message}'";
                }

                await ProcessExceptionAsync(httpContext, hsce.StatusCode, hsce, message);
            }
            catch (Exception ex)
            {
                await ProcessExceptionAsync(httpContext, StatusCodes.Status500InternalServerError, ex, "Application Internal Error");
            }
        }

        public async Task ProcessExceptionAsync(HttpContext httpContext, int statusCode, Exception exception, string message)
        {
            if (httpContext.Response.HasStarted)
            {
                throw exception;
            }

            Logger.LogTelemetry(httpContext, LogEventLevel.Error, $"Message {message} with exception {exception.Message} and status code {statusCode}.");

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsync(message);
        }
    }
}
