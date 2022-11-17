using EPA.Office365.Diagnostics;
using EPA.Office365.oAuth;
using System;

namespace EPA.SharePoint.SysConsole.Commands
{
    public abstract class BaseAdalCommand<T> where T : ICommonOptions
    {
        public IAppSettings Settings { get; }
        public Serilog.ILogger TraceLogger { get; set; }
        public virtual T Opts { get; }

        protected BaseAdalCommand(T opts, IAppSettings settings, Serilog.ILogger traceLogger)
        {
            Opts = opts ?? throw new ArgumentNullException(nameof(opts), "CommonOptions object is required.");
            Settings = settings ?? throw new ArgumentNullException(nameof(settings), "IAppSettings is required.");
            TraceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger), "ILogger is required.");
            Log.InitializeLogger(traceLogger);
        }

        /// <summary>
        /// Provides cache store for ADAL Tokens
        /// </summary>
        public IOAuthTokenCache TokenCache { get; set; }

        public int Run()
        {
            OnBeginInit();
            return OnRun();
        }

        public abstract void OnBeginInit();

        public abstract int OnRun();

        /// <summary>
        /// If WhatIf in arguments we should evaulate if it should run
        /// </summary>
        /// <param name="message"></param>
        /// <returns>
        /// (true) if WhatIf is null or false
        /// (false) if WhatIf is not null
        /// </returns>
        public bool ShouldProcess(string message)
        {
            var process = !(Opts.WhatIf ??= false);
            TraceLogger.Warning(message);
            return process;
        }
    }
}
