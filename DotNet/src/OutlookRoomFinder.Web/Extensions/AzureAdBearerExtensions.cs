using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OutlookRoomFinder.Core;
using System;
using System.Threading.Tasks;

namespace OutlookRoomFinder.Web.Extensions
{
    public static class AzureAdBearerExtensions
    {
        public static AuthenticationBuilder AddAzureAdBearer(this IServiceCollection services)
            => services.AddAzureAdBearer(_ => { });

        public static AuthenticationBuilder AddAzureAdBearer(this IServiceCollection services, Action<AppSettings> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = services.AddAuthentication(o =>
            {
                o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            });

            builder.Services.Configure(configureOptions);
            builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureAzureOptions>();

            builder.AddJwtBearer()
            .AddCookie();

            return builder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Called by runtime.")]
        private class ConfigureAzureOptions : IConfigureNamedOptions<JwtBearerOptions>
        {
            private AppSettingsAzureAd AzureOptions { get; }

            public ConfigureAzureOptions(IOptions<AppSettings> azureOptions)
            {
                AzureOptions = azureOptions.Value.AzureAd;
            }

            public void Configure(string name, JwtBearerOptions options)
            {
                options.Audience = AzureOptions.ClientId;
                options.Authority = $"{AzureOptions.Instance}{AzureOptions.TenantId}";
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = AzureOptions.TenantId,
                    ValidateAudience = true,
                    ValidAudiences = new[]
                    {
                        AzureOptions.ClientId,
                        AzureOptions.Audience,
                        $"{AzureOptions.Audience}/access_as_user",
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var user = context.Principal?.Identity?.Name;
                        System.Diagnostics.Trace.TraceInformation($"Authentication for {user} expires UTC => {context.SecurityToken?.ValidTo}");
                        return Task.CompletedTask;
                    }
                };
            }

            public void Configure(JwtBearerOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
