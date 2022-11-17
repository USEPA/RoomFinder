using Microsoft.EntityFrameworkCore;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;

namespace EPA.Office365.Database
{
    public static class AnalyticDbExtensions
    {
        public static void HandleExceptionAndRethrow(this Exception ex)
        {
            if (ex.GetType() == typeof(DbUpdateException))
            {
                var validationException = (DbUpdateException)ex;
                foreach (var kv in validationException?.Data)
                {
                    Trace.TraceError($"Error {kv}");
                }
                throw new DataValidationException("One or more items failed to save due to validation errors.", validationException);
            }
            else if(ex is SqlException se)
            {
                Trace.TraceError($"SqlException {se}");
            }
            else
            {
                Trace.TraceError("An error occurred while saving items");
                throw new Exception("An error occurred while saving items", ex);
            }
        }

        public static bool LogException(this DbUpdateException ex, string[] errStrings = null)
        {
            Trace.TraceError("UpdateException:{0}", ex.Message);
            if (errStrings != null
                && errStrings.Count() > 0
                && errStrings.Any(es => ex.Message.IndexOf(es, StringComparison.CurrentCultureIgnoreCase) != -1))
            {
                return true;
            }

            if (ex.InnerException != null)
            {
                if (ex.InnerException.GetType() == typeof(SqlException))
                {
                    return LogException(ex.InnerException as SqlException, errStrings);
                }
                else
                {
                    return ex.InnerException.LogException(errStrings);
                }
            }

            return false;
        }

        public static bool LogException(this SqlException ex, string[] errStrings = null)
        {
            Trace.TraceError("SqlException:{0}", ex.Message);
            if (errStrings != null
                && errStrings.Count() > 0
                && errStrings.Any(es =>
                    ex.Message.IndexOf(es, StringComparison.CurrentCultureIgnoreCase) != -1
                    || ex.Number.ToString() == es))
            {
                return true;
            }

            if (ex.InnerException != null)
            {
                return ex.InnerException.LogException(errStrings);
            }

            return false;
        }

        public static bool LogException(this System.Exception ex, string[] errStrings = null)
        {
            Trace.TraceError("Exception:{0}", ex.Message);
            if (errStrings != null
                && errStrings.Count() > 0
                && errStrings.Any(es => ex.Message.IndexOf(es, StringComparison.CurrentCultureIgnoreCase) != -1))
            {
                return true;
            }

            if (ex.InnerException != null)
            {
                if (ex.InnerException.GetType() == typeof(SqlException))
                {
                    return LogException(ex.InnerException as SqlException, errStrings);
                }
                else
                {
                    return LogException(ex.InnerException, errStrings);
                }
            }

            return false;
        }
    }
}
