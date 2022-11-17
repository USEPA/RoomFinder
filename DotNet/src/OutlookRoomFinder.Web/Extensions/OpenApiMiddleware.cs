using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace OutlookRoomFinder.Web.Extensions
{
    [ExcludeFromCodeCoverage]
    public class OpenApiMiddleware
    {
        private readonly RequestDelegate next;

        public OpenApiMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Ensures requests containing swagger are authenticated, is required that this is added after authentication and authorization middlewares
        /// </summary>
        /// <param name="httpContext">contains securty context</param>        
        [SuppressMessage("Major Code Smell", "S3900:Arguments of public methods should be validated against null", Justification = "not null per framework")]
        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.Request.Path.StartsWithSegments("/swagger") && !httpContext.User.Identity.IsAuthenticated)
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next.Invoke(httpContext);
        }
    }

    /// <summary>
    /// Extension method used to add the middleware to the HTTP request pipeline. 
    /// </summary>
    public static class OpenApiMiddlewareExtensions
    {
        public static IApplicationBuilder UseOpenApiMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OpenApiMiddleware>();
        }
    }
}