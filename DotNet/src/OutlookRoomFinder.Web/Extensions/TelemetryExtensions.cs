using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System;

namespace Microsoft.AspNetCore.Authentication
{
    public static class TelemetryExtensions
    {
        private static bool hasFile = true;

        public static void AddLogging(this IServiceCollection services, IConfiguration configuration)
        {
            var loggingSection = configuration.GetSection("Logging");
            var writeToFile = loggingSection.GetValue<string>("LogFile");
            Enum.TryParse(loggingSection["Level"], true, out LogEventLevel loggingLevel);

            if (string.IsNullOrEmpty(writeToFile))
            {
                System.Diagnostics.Trace.TraceError($"Logging file missing from JSON or Azure Key Vault.");
                hasFile = false;
            }

            services.AddSingleton(implementationInstance =>
            {
                var loggerConfiguration = new LoggerConfiguration()
                        .WriteTo.Logger(consoleLogger =>
                        {
                            consoleLogger.MinimumLevel.Information().WriteTo.Console();
                        });

                if (hasFile)
                {
                    loggerConfiguration = loggerConfiguration.WriteTo.Logger(eventLogger =>
                    {
                        eventLogger.WriteTo
                        .File(writeToFile, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: loggingLevel)
                        .Enrich
                        .FromLogContext();
                    });
                }

                Log.Logger = loggerConfiguration.CreateLogger();
                return Log.Logger;
            });

            services.AddSingleton<ILoggerFactory>(provider =>
            {
                var logger = provider.GetRequiredService<Serilog.ILogger>();
                return new SerilogLoggerFactory(logger, true);
            });
        }
    }
}
