using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OutlookRoomFinder.Core;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authentication
{
    public static class AzureAdExtensions
    {
        public static AuthenticationBuilder AddAzureAd(this IServiceCollection services)
            => services.AddAzureAd(_ => { });

        public static AuthenticationBuilder AddAzureAd(this IServiceCollection services, Action<AppSettings> configureOptions)
        {

            services.Configure(configureOptions);
            services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, ConfigureAzureOptions>();

            var builder = services
                .AddAuthentication(sharedOptions =>
                {
                    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddOpenIdConnect()
                .AddCookie();

            return builder;
        }

        [SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Called by runtime.")]
        private class ConfigureAzureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
        {
            private AppSettingsAzureAd AzureOptions { get; }

            public ConfigureAzureOptions(IOptions<AppSettings> options)
            {
                AzureOptions = options.Value.AzureAd;
            }

            public void Configure(string name, OpenIdConnectOptions options)
            {
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                options.ClientId = AzureOptions.ClientId;
                options.ClientSecret = AzureOptions.ClientSecret;
                options.Authority = $"{AzureOptions.Instance}{AzureOptions.TenantId}";
                options.UseTokenLifetime = true;
                options.RequireHttpsMetadata = false;

                options.Scope.Add(OidcConstants.ScopeOfflineAccess);
                options.Scope.Add(OidcConstants.ScopeUserRead);
                options.Scope.Add(AzureOptions.Audience);


                // Handling the auth redemption by MSAL.NET so that a token is available in the token cache
                // where it will be usable from Controllers later (through the TokenAcquisition service)
                var handler = options.Events.OnAuthorizationCodeReceived;
                options.Events.OnAuthorizationCodeReceived = async context =>
                {
                    var tenantId = context.Principal.FindFirst(OidcConstants.SchemaTenantId);
                    var userObjectId = context.Principal.FindFirst(OidcConstants.SchemaObjectId);
                    System.Diagnostics.Trace.TraceInformation($"OnAuthorization {tenantId} user=>({userObjectId})");
                    System.Diagnostics.Trace.TraceInformation($"OnAuthorization {tenantId} code=>({context.ProtocolMessage.Code})");
                    await handler(context).ConfigureAwait(false);
                };

                options.TokenValidationParameters = new IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = false
                };

                // Avoids having users being presented the select account dialog when they are already signed-in
                // for instance when going through incremental consent
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    var login = context.Properties.GetParameter<string>(OpenIdConnectParameterNames.LoginHint);
                    if (!string.IsNullOrWhiteSpace(login))
                    {
                        context.ProtocolMessage.LoginHint = login;
                        context.ProtocolMessage.DomainHint = context.Properties.GetParameter<string>(
                            OpenIdConnectParameterNames.DomainHint);

                        // delete the login_hint and domainHint from the Properties when we are done otherwise
                        // it will take up extra space in the cookie.
                        context.Properties.Parameters.Remove(OpenIdConnectParameterNames.LoginHint);
                        context.Properties.Parameters.Remove(OpenIdConnectParameterNames.DomainHint);
                    }

                    // Additional claims
                    if (context.Properties.Items.ContainsKey(OidcConstants.AdditionalClaims))
                    {
                        context.ProtocolMessage.SetParameter(
                            OidcConstants.AdditionalClaims,
                            context.Properties.Items[OidcConstants.AdditionalClaims]);
                    }

                    return Task.FromResult(0);
                };
            }

            public void Configure(OpenIdConnectOptions options)
            {
                Configure(Options.DefaultName, options);
            }
        }
    }
}
