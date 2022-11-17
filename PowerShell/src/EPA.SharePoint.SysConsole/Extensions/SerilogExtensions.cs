using EPA.Office365.oAuth;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;

namespace EPA.SharePoint.SysConsole.Extensions
{
    public static class SerilogExtensions
    {
        /// <summary>
        /// Configure and Initialize the Diagnostics Logger
        /// </summary>
        /// <param name="Configuration"></param>
        /// <param name="appSettings"></param>
        /// <param name="logFilename"></param>
        /// <param name="defaultLevel"></param>
        /// <returns></returns>
        public static Logger GetLogger(this IConfigurationRoot Configuration, IAppSettings appSettings, string logFilename, LogEventLevel defaultLevel = LogEventLevel.Error)
        {
            var loggingSection = Configuration.GetSection("Logging");
            if (!Enum.TryParse(loggingSection["Level"], true, out LogEventLevel loggingLevel)
                || defaultLevel != LogEventLevel.Error)
            {
                loggingLevel = defaultLevel;
            }

            var loggerConfiguration = new LoggerConfiguration().WriteTo.Logger(consoleLogger =>
            {
                consoleLogger.MinimumLevel.Warning().WriteTo.Console().Enrich.FromLogContext();
            });

            loggerConfiguration = loggerConfiguration.WriteTo.Logger(eventLogger =>
            {
                eventLogger.WriteTo
                .File($"{appSettings.Commands.AppLogDirectory}\\{logFilename}", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: loggingLevel)
                .Enrich
                .FromLogContext();
            });

            var logger = loggerConfiguration.CreateLogger();
            Office365.Diagnostics.Log.InitializeLogger(logger);
            return logger;
        }
    }
}
