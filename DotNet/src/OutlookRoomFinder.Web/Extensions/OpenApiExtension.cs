using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace OutlookRoomFinder.Web.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class OpenApiExtension
    {
        private const string VersionKey = "v2.0";
        private const string OpenApiSpecNameKey = "RoomFinder - v2.0";

        /// <summary>
        /// Configure swagger info with authentication
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddOpenApi(this IServiceCollection services)
        {
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(VersionKey, new OpenApiInfo { Title = OpenApiSpecNameKey, Version = VersionKey });
                c.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT Authorization header using the Bearer scheme.",
                    BearerFormat = "JWT",
                    Scheme = "bearer",
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Header
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" } },
                        System.Array.Empty<string>()
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// Wire middleware, use of swagger and swagger endpoint
        /// </summary>
        /// <param name="app">used to wire swagger</param>        
        public static IApplicationBuilder UseOpenApi(this IApplicationBuilder app)
        {
            // ensure swagger endpoint is authorized
            app.UseOpenApiMiddleware();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{VersionKey}/swagger.json", OpenApiSpecNameKey);
            });

            return app;
        }
    }
}