using EPA.Office365.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace EPA.Office365.Graph.Reporting
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Processing protection")]
    public abstract class CustomGraphReportVisitor : IDisposable
    {
        #region Properties

        internal ICustomGraphReportProperties GraphReportProperties { get; private set; }

        internal ReportingProcessor ReportingProcessor { get; private set; } 

        public bool IsDisposed { get; set; }

        /// <summary>
        /// Represents the EF processing throttle
        /// </summary>
        internal readonly int throttle = 50;

        /// <summary>
        /// Default number of rows to process in the graph api calls
        /// </summary>
        internal readonly int defaultRows = 500;

        #endregion

        protected CustomGraphReportVisitor(ICustomGraphReportProperties properties, Serilog.ILogger logger, HttpClient client)
        {
            ReportingProcessor = new ReportingProcessor(logger, client);
            GraphReportProperties = properties;
            Log.InitializeLogger(logger);
        }

        /// <summary>
        /// Process acounts and "Period" based reporting endpoints
        ///     <paramref name="details"/> if specified Detail EndPoints should be called
        ///     pull distinct activity dates to pull a list of days with no information
        ///     if specified, take the diff between specified and runtildate
        ///     if not specified, pull the last run dates(max) and use that til runtildate
        /// </summary>
        /// <param name="details">(OPTIONAL) if specified will run the longer running reporting</param>
        public abstract void ProcessReporting(bool details);

        /// <summary>
        /// Execute any post data builds
        /// </summary>
        public virtual void OnProcessRollup()
        {
        }


        /// <summary>
        /// Converts nullable into default value
        /// </summary>
        /// <param name="webValue"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        protected static long ParseDefault(long? webValue, long defaultValue = 0)
        {
            long result = defaultValue;
            try
            {
                result = (long)webValue;
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Provides a collection of dates between <paramref name="from"/> to <paramref name="thru"/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="thru"></param>
        /// <param name="daysToAdd"></param>
        /// <returns></returns>
        protected static IEnumerable<DateTime> EachDay(DateTime from, DateTime thru, int daysToAdd = 1)
        {
            for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(daysToAdd))
                yield return day;
        }

        /// <summary>
        /// Returns the first date based on collection of distinct run dates and UTC runtime
        /// </summary>
        /// <param name="lastActivityDates"></param>
        /// <param name="reportDate"></param>
        /// <param name="currentRunMaxDate">(OPTIONAL) will ensure the return date is a minimum of value</param>
        /// <returns></returns>
        protected static DateTime FirstRunDate(IQueryable<DateTime> lastActivityDates, DateTime? reportDate, int currentRunMaxDate = 27)
        {
            DateTime currentRunUTC = DateTime.UtcNow.Date;
            var lastRunDate = new DateTime(currentRunUTC.Year, currentRunUTC.Month, 1);
            if (reportDate.HasValue)
            {
                lastRunDate = reportDate.Value.Date;
            }

            // if there are run dates; lets take the max and make sure we aren't requerying those dates
            if (lastActivityDates != null && lastActivityDates.Any())
            {
                var currentMaxDate = lastActivityDates.Max(f => f.Date);
                if (lastRunDate < currentMaxDate)
                {
                    lastRunDate = currentMaxDate;
                }
            }

            if (currentRunUTC.Subtract(lastRunDate).Days > currentRunMaxDate)
            {
                lastRunDate = currentRunUTC.AddDays(-1 * currentRunMaxDate).Date;
            }

            return lastRunDate;
        }

        protected abstract void OnDisposing();

        /// <summary>
        /// Disposing
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing
                && !IsDisposed)
            {
                OnDisposing();
            }
            IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
