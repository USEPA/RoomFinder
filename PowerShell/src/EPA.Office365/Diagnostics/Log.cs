using Serilog;
using Serilog.Context;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace EPA.Office365.Diagnostics
{
    /// <summary>
    /// Logging class
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Gets or sets ILogger object
        /// </summary>
        public static ILogger Logger { get; set; }

        public static void InitializeLogger(ILogger logger)
        {
            if (Logger == null)
            {
                Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }
        }

        #region Public Members

        #region Error
        public static void LogError(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = memberName,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Error(entry);
        }
        public static void LogError(Exception exception, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Exception = exception,
                Source = memberName,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Error(entry);
        }
        /// <summary>
        /// Logs error message and source
        /// </summary>
        /// <param name="source">Error source</param>
        /// <param name="message">Error message</param>
        /// <param name="args">Arguments object</param>
        public static void Error(string source, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                LogType = LogEntryType.Debugging
            };
            Error(entry);
        }
        /// <summary>
        /// Logs error message, source and exception
        /// </summary>
        /// <param name="ex">Exception object</param>
        /// <param name="source">Error source</param>
        /// <param name="message">Error message</param>
        /// <param name="args">Arguments object</param>
        public static void Error(Exception ex, string source, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                Exception = ex,
                LogType = LogEntryType.Debugging
            };
            Error(entry);
        }
        /// <summary>
        /// Error LogEntry
        /// </summary>
        /// <param name="logEntry">LogEntry object</param>
        public static void Error(LogEntry logEntry)
        {
            using (LogContext.PushProperty("Source", logEntry?.Source))
            using (LogContext.PushProperty("Exception", logEntry?.Exception))
            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("OperationProperties", logEntry?.OperationProperties))
            {
                Logger.Error(logEntry?.Message);
            }
        }
        #endregion

        #region Info
        public static void LogInformation(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = memberName,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Info(entry);
        }

        /// <summary>
        /// Log Information
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="message">Message string</param>
        /// <param name="args">Arguments object</param>
        public static void Info(string source, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                LogType = LogEntryType.Debugging
            };
            Info(entry);
        }
        /// <summary>
        /// Log Information
        /// </summary>
        /// <param name="ex">Exception object</param>
        /// <param name="source">Source string</param>
        /// <param name="message">Message string</param>
        /// <param name="args">Arguments option</param>
        public static void Info(Exception ex, string source, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                Exception = ex,
                LogType = LogEntryType.Debugging
            };
            Info(entry);
        }
        /// <summary>
        /// Log Information
        /// </summary>
        /// <param name="logEntry">LogEntry object</param>
        public static void Info(LogEntry logEntry)
        {
            using (LogContext.PushProperty("Source", logEntry?.Source))
            using (LogContext.PushProperty("Exception", logEntry?.Exception))
            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("OperationProperties", logEntry?.OperationProperties))
            {
                Logger.Information(logEntry?.Message);
            }
        }
        #endregion

        #region Warning
        /// <summary>
        /// Warning Log
        /// </summary>
        /// <param name="message"></param>
        /// <param name="memberName">(OPTIONAL) will use reflection to write StackTrace Method name</param>
        /// <param name="filePath">(OPTIONAL) will use reflection to write StackTrace file name with path</param>
        /// <param name="lineNumber">(OPTIONAL) will use reflection to write StackTrace class file line number</param>
        public static void LogWarning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = memberName,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Warning(entry);
        }
        /// <summary>
        /// Warning Log
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        /// <param name="memberName">(OPTIONAL) will use reflection to write StackTrace Method name</param>
        /// <param name="filePath">(OPTIONAL) will use reflection to write StackTrace file name with path</param>
        /// <param name="lineNumber">(OPTIONAL) will use reflection to write StackTrace class file line number</param>
        public static void LogWarning(Exception ex, string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = memberName,
                Exception = ex,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Warning(entry);
        }
        /// <summary>
        /// Warning Log
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="message">Message string</param>
        /// <param name="args">Arguments object</param>
        public static void Warning(string source, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                LogType = LogEntryType.Debugging
            };
            Warning(entry);
        }
        /// <summary>
        /// Warning Log
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="ex">Exception object</param>
        /// <param name="message">Message string</param>
        /// <param name="args">Arguments object</param>
        public static void Warning(string source, Exception ex, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                Exception = ex,
                LogType = LogEntryType.Debugging
            };
            Warning(entry);
        }

        /// <summary>
        /// Warning Log
        /// </summary>
        /// <param name="logEntry">LogEntry object</param>
        public static void Warning(LogEntry logEntry)
        {
            using (LogContext.PushProperty("Source", logEntry?.Source))
            using (LogContext.PushProperty("Exception", logEntry?.Exception))
            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("OperationProperties", logEntry?.OperationProperties))
            {
                Logger.Warning(logEntry?.Message);
            }
        }
        #endregion

        #region Debug
        public static void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = memberName,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Debug(entry);
        }
        /// <summary>
        /// Debug Log
        /// </summary>
        /// <param name="source">Source stirng</param>
        /// <param name="message">Message string</param>
        /// <param name="args">Arguments object</param>
        public static void Debug(string source, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                LogType = LogEntryType.Debugging
            };
            Debug(entry);
        }


        /// <summary>
        /// Debug Log
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="ex">Exception object</param>
        /// <param name="message">Message string</param>
        /// <param name="args">Arguments object</param>
        public static void Debug(string source, Exception ex, string message, params object[] args)
        {
            var entry = new LogEntry()
            {
                Message = string.Format(message, args),
                Source = source,
                Exception = ex,
                LogType = LogEntryType.Debugging
            };
            Debug(entry);
        }

        /// <summary>
        /// Debug Log
        /// </summary>
        /// <param name="logEntry">LogEntry object</param>
        public static void Debug(LogEntry logEntry)
        {
            using (LogContext.PushProperty("Source", logEntry?.Source))
            using (LogContext.PushProperty("Exception", logEntry?.Exception))
            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("OperationProperties", logEntry?.OperationProperties))
            {
                Logger.Debug(logEntry?.Message);
            }
        }
        #endregion

        #region Verbose

        public static void LogVerbose(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            var entry = new LogEntry()
            {
                Message = message,
                Source = memberName,
                LogType = LogEntryType.Debugging,
                OperationProperties = GetProperties(filePath, lineNumber)
            };
            Verbose(entry);
        }
        /// <summary>
        /// Verbose Log
        /// </summary>
        /// <param name="logEntry">LogEntry object</param>
        public static void Verbose(LogEntry logEntry)
        {
            using (LogContext.PushProperty("Source", logEntry?.Source))
            using (LogContext.PushProperty("Type", logEntry?.LogType))
            using (LogContext.PushProperty("OperationProperties", logEntry?.OperationProperties))
            {
                Logger.Verbose(logEntry?.Message);
            }
        }

        #endregion

        private static string GetFileName(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                return $"File => {Path.GetFileName(filePath)}";
            }
            return $"File => No File";
        }

        private static string[] GetProperties(string filePath, int lineNumber)
        {
            return new[]
            {
                GetFileName(filePath),
                $"LineNumber => {lineNumber}"
            };
        }

        #endregion
    }
}