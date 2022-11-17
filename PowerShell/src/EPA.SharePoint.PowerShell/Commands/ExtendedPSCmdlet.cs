using System;
using System.Management.Automation;
using System.Reflection;

namespace EPA.SharePoint.PowerShell.Commands
{
    /// <summary>
    /// Base class for all the Microsoft Graph related cmdlets
    /// </summary>
    public abstract class ExtendedPSCmdlet : PSCmdlet
    {
        /// <summary>
        /// Storage for the cmdlet in the current thread
        /// </summary>
        private string m_cmdLetName { get; set; }
        internal string CmdLetName
        {
            get
            {
                if (string.IsNullOrEmpty(m_cmdLetName))
                {
                    var runningAssembly = Assembly.GetExecutingAssembly();
                    m_cmdLetName = this.GetType().Name;
                }
                return m_cmdLetName;
            }
        }

        /// <summary>
        /// Initializers the logger from the cmdlet
        /// </summary>
        protected virtual void OnBeginInitialize()
        {
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            var runningDirectory = this.SessionState.Path.CurrentFileSystemLocation;
            var runningAssembly = Assembly.GetExecutingAssembly();
            var runningAssemblyName = runningAssembly.ManifestModule.Name;

            OnBeginInitialize();
            LogVerbose($">>> Begin {CmdLetName} at {DateTime.UtcNow}-UTC");
        }

        public virtual void ExecuteCmdlet()
        { }

        protected override void ProcessRecord()
        {
            ExecuteCmdlet();
        }

        /// <summary>
        /// End Processing cleanup or write logs
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();
            LogVerbose($"<<< End {CmdLetName} at {DateTime.UtcNow}-UTC");
        }

        /// <summary>
        /// Log: ERROR
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogError(Exception ex, string message, params object[] args)
        {
            if (string.IsNullOrEmpty(message))
            {
                System.Diagnostics.Trace.TraceError("Exception: {0}", ex.Message);
            }
            else
            {
                System.Diagnostics.Trace.TraceError(message, args);
            }
        }

        /// <summary>
        /// Log: DEBUG
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogDebugging(string message, params object[] args)
        {
            System.Diagnostics.Trace.TraceInformation(message, args);
            if (!Stopping)
            {
                WriteDebug(string.Format(message, args));
            }
        }

        /// <summary>
        /// Writes a warning message to the cmdlet and logs to directory
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogWarning(string message, params object[] args)
        {
            System.Diagnostics.Trace.TraceWarning(message, args);
            if (!Stopping)
            {
                WriteWarning(string.Format(message, args));
            }
        }

        /// <summary>
        /// Log: VERBOSE
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        protected virtual void LogVerbose(string message, params object[] args)
        {
            System.Diagnostics.Trace.TraceInformation(message, args);
            if (!Stopping)
            {
                WriteVerbose(string.Format(message, args));
            }
        }
    }
}