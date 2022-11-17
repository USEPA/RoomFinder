using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EPA.Office365.API.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace EPA.Office365.API
{
    public class Startup
    {
        private IWebHostEnvironment HostingEnvironment { get; }

        private IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment environment)
        {
            HostingEnvironment = environment;
            Configuration = BuildConfiguration();
            BuildLogger();
        }

        private IConfiguration BuildConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddUserSecrets(typeof(Startup).Assembly);

            if (HostingEnvironment.IsDevelopment())
            {
                // Re-add User secrets so it takes precedent for local development
                configurationBuilder.AddUserSecrets(typeof(Startup).Assembly);
            }

            return configurationBuilder.Build();
        }

        private void BuildLogger()
        {
            var loggerConfiguration = new LoggerConfiguration().WriteTo.Logger(consoleLogger =>
            {
                consoleLogger.MinimumLevel.Information().WriteTo.Console();
            });

            var logger = loggerConfiguration
                 .WriteTo.Logger(eventLogger =>
                 {
                     eventLogger.WriteTo.Trace(Serilog.Events.LogEventLevel.Information);
                 })
                 .CreateLogger();

            Log.Logger = logger;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            var imvcBuilder = services
                .AddControllers(opts => opts.EnableEndpointRouting = false)
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(pts =>
                {
                    pts.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    pts.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                });
            AddAzureMonitoringWebHookIfConfigured(imvcBuilder, Configuration);

            if (!HostingEnvironment.IsDevelopment())
            {
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!HostingEnvironment.IsDevelopment())
            {
                app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }


        private static void AddAzureMonitoringWebHookIfConfigured(IMvcBuilder builder, IConfiguration config)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var webhooks = config["WebHooks:usepa:SecretKey:default"];
            if (!string.IsNullOrEmpty(webhooks))
            {
                WebHookMetadata.Register<AzureWebhookMetadata>(builder.Services);
                builder.AddWebHooks();
            }
        }
    }
}
