using Microsoft.AspNetCore.Builder;

namespace OutlookRoomFinder.Web.Extensions
{

    /// <summary>
    /// Extension method used to add the middleware to the HTTP request pipeline.
    /// </summary>    
    public static class ExceptionHandlerMiddlewareExtension
    {
        public static IApplicationBuilder UseExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlerMiddleware>();
        }
    }
}
