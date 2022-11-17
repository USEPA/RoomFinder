using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using OutlookRoomFinder.Core.Extensions;
using OutlookRoomFinder.Core.Models;
using Serilog.Context;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OutlookRoomFinder.Core
{
    [SuppressMessage("Critical Code Smell", "S1541:Methods and properties should not be too complex", Justification = "simple conditionals")]
    public static class LogHelper
    {
        public static void Logging(this Serilog.ILogger logger, LogEventLevel logLevel, string operation, [CallerMemberName]string memberName = "", [CallerLineNumber]int lineNumber = 0)
            => Logging(logger, logLevel, operation, Array.Empty<string>(), memberName, lineNumber);

        [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "builds logs")]
        public static void Logging(this Serilog.ILogger logger, LogEventLevel logLevel, string operation, IEnumerable<string> operationProperties, [CallerMemberName]string memberName = "", [CallerLineNumber]int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            var  properties = operationProperties.ConvertIntoSureEnumerable().ToList();
            FormatOperations(memberName, lineNumber).ForEach((operation) => { properties.Add(operation); });

            var logEntry = new LogEntry
            {
                LogLevel = logLevel,
                LogType = LogEntryType.Debugging,
                Operation = operation,
                OperationProperties = properties
            };

            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("Operation", logEntry.Operation))
            using (LogContext.PushProperty("OperationProperties", logEntry.OperationProperties))
            {
                LogForType(logger, logEntry);
            }
        }


        public static void LogTelemetry(this Serilog.ILogger logger, HttpContext httpContext, LogEventLevel logLevel, string operation, [CallerMemberName]string memberName = "", [CallerLineNumber]int lineNumber = 0)
            => LogTelemetry(logger, httpContext, logLevel, operation, Array.Empty<string>(), memberName, lineNumber);

        [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "builds logs")]
        public static void LogTelemetry(this Serilog.ILogger logger, HttpContext httpContext, LogEventLevel logLevel, string operation, IEnumerable<string> operationProperties, [CallerMemberName]string memberName = "", [CallerLineNumber]int lineNumber = 0)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var properties = operationProperties.ConvertIntoSureEnumerable().ToList();
            FormatOperations(memberName, lineNumber).ForEach((operation) => { properties.Add(operation); });

            var logEntry = new LogEntry
            {
                LogLevel = logLevel,
                LogType = LogEntryType.Telemetry,
                Operation = operation,
                OperationProperties = properties
            };

            var userAgent = new StringValues("N/A");
            if (httpContext.Request.Headers.ContainsKey("User-Agent"))
            {
                httpContext.Request.Headers.TryGetValue("User-Agent", out userAgent);
            }

            using (LogContext.PushProperty("InstanceId", httpContext.TraceIdentifier))
            using (LogContext.PushProperty("UserAgent", userAgent))
            using (LogContext.PushProperty("UPN", httpContext.User?.Identity?.Name))
            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("Operation", logEntry?.Operation))
            using (LogContext.PushProperty("OperationProperties", operationProperties))
            {
                LogForType(logger, logEntry);
            }
        }

        private static void LogForType(Serilog.ILogger logger, LogEntry logEntry)
        {
            if (logEntry == null)
            {
                return;
            }

            var msg = $"RoomFinder=>{logEntry?.Operation}";
            switch (logEntry.LogLevel)
            {
                case LogEventLevel.Information:
                    logger.Information(msg);
                    break;
                case LogEventLevel.Debug:
                    logger.Debug(msg);
                    break;
                case LogEventLevel.Warning:
                    logger.Warning(msg);
                    break;
                case LogEventLevel.Error:
                    logger.Error(msg);
                    break;
                case LogEventLevel.Fatal:
                    logger.Fatal(msg);
                    break;
                default:
                    logger.Verbose(msg);
                    break;
            }
        }

        private static string[] FormatOperations(string memberName = "", int lineNumber = 0)
        {
            return new[] {
                    $"Member name: {memberName}",
                    $"Line Number {lineNumber}"
                };
        }
    }
}
