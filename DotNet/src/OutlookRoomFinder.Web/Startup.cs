using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OutlookRoomFinder.Core;
using OutlookRoomFinder.Core.Services;
using OutlookRoomFinder.Web.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OutlookRoomFinder.Web
{
    public class Startup
    {
        private IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment)
        {
            HostingEnvironment = environment;
            Configuration = BuildConfiguration();
        }

        private IConfiguration BuildConfiguration()
        {
            ConfigurationBuilder builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddUserSecrets(typeof(Startup).Assembly)
                .AddAzureKeyVaultIfAvailable();

            if (HostingEnvironment.IsDevelopment())
            {
                // Re-add User secrets so it takes precedent for local development
                builder.AddUserSecrets(typeof(Startup).Assembly);
            }

            return builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddCors();

            services.AddAzureAdBearer(options => Configuration.Bind(options));


            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IAppSettings, AppSettings>((serviceProvider) =>
            {
                var config = Configuration.Get<AppSettings>();
                config.EnvironmentName = HostingEnvironment.EnvironmentName;
                config.WebRootPath = HostingEnvironment.WebRootPath;
                config.DeployedVersion = Environment.GetEnvironmentVariable("ASPNETCORE_RELEASE");
                return config;
            });
            services.AddTransient<IKeyVaultHelper, AzureKeyVaultExtensions>();

            // if EXO is set then use JSON file on disk
            var exchangeSection = Configuration.GetSection("Exchange");
            if (exchangeSection.Exists())
            {
                services.AddSingleton<IExchangeContext, JsonExchangeContext>();
            }

            // Configure logging
            services.AddLogging(Configuration);

            // if EXO [use graph] otherwise use the [ManagedEWS]
            services.AddSingleton<IExchangeService, ExchangeWebService>();

            services.AddControllers((opts) => opts.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

            if (!HostingEnvironment.IsDevelopment())
            {
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                });
            }

            services.AddOpenApi();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Bug", "S2583:Conditionally executed code should be reachable", Justification = "Debug macro set at runtime.")]
        public void Configure(IApplicationBuilder app)
        {
            if (HostingEnvironment.IsDevelopment())
            {
                var developmentSettings = new AppSettingsDevelopment();
                Configuration.Bind("Development", developmentSettings);
                var cors = new List<string>();
                if (developmentSettings.Cors?.Any() == true)
                {
                    cors.AddRange(developmentSettings.Cors);
                }

                app.UseExceptionHandlerMiddleware();
                app.UseCors(builder => builder.WithOrigins(cors.ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
            }
            else
            {
                app.UseExceptionHandlerMiddleware();
                app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
            }

            app.UseHsts();

            if (!HostingEnvironment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            // enable AzureAD Tokens and authentication
            app.UseAuthentication();
            app.UseAuthorization();
            // enable serilog
            app.UseTelemetryLoggingMiddleware();
            // enable Security-Policy headers
            app.UseCsp(csp =>
            {
                csp.FrameAncestors(config => config.Self()).FrameSources(config =>
                    config.CustomSources("outlook.com", "login.microsoftonline.us", "login.microsoftonline.com", "outlook.office.com", "outlook.office365.com")
                    .Self());
                csp.FrameSources(config => config.Self()).FrameAncestors(config =>
                    config.CustomSources("outlook.com", "login.microsoftonline.us", "login.microsoftonline.com", "outlook.office.com", "outlook.office365.com")
                    .Self()
                );
            });
            // register swagger after UseStaticFiles and UseAuthentication
            app.UseOpenApi();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // outlook addin
            app.Map("/apps/outlook", builder =>
            {
                builder.UseSpa(spa =>
                {
                    if (HostingEnvironment.IsDevelopment())
                    {
                        spa.UseProxyToSpaDevelopmentServer($"http://localhost:4200/");
                    }
                    else
                    {
                        var currentPath = Directory.GetCurrentDirectory();
                        var staticPath = Path.Combine(currentPath, $"wwwroot/apps/outlook");
                        var fileOptions = new StaticFileOptions { FileProvider = new PhysicalFileProvider(staticPath) };
                        app.UseSpaStaticFiles(options: fileOptions);

                        spa.Options.DefaultPageStaticFileOptions = fileOptions;
                    }
                });
            });

            // reporting | default application
            app.UseSpa(spa =>
            {
                if (HostingEnvironment.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer($"http://localhost:4201/");
                }
                else
                {
                    var currentPath = Directory.GetCurrentDirectory();
                    var staticPath = Path.Combine(currentPath, $"wwwroot/apps/reporting");
                    var fileOptions = new StaticFileOptions { FileProvider = new PhysicalFileProvider(staticPath) };
                    app.UseSpaStaticFiles(options: fileOptions);

                    spa.Options.DefaultPageStaticFileOptions = fileOptions;
                }
            });
        }
    }
}
