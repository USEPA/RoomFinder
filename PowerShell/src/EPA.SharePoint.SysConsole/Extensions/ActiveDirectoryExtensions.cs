using System;
using System.Collections.Generic;
using System.Linq;

namespace EPA.SharePoint.PowerShell.Extensions
{
    /// <summary>
    /// Provides extension methods to extend AD interactions
    /// </summary>
    public static class ActiveDirectoryExtensions
    {
        /// <summary>
        /// Scans the dictionary for the property until it finds a non-blank entry
        /// </summary>
        /// <param name="UserProperties"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string RetrieveUserProperty(this IDictionary<string, string> UserProperties, string[] propertyName)
        {
            var propertyValue = string.Empty;
            foreach (var property in propertyName)
            {
                var propertyResult = UserProperties.RetrieveUserProperty(property);
                if (!string.IsNullOrEmpty(propertyResult))
                {
                    propertyValue = propertyResult;
                    break;
                }
            }

            return propertyValue;
        }

        /// <summary>
        /// Enumerates the collection to find the property name and return the first value
        /// </summary>
        /// <param name="UserProperties"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static string RetrieveUserProperty(this IDictionary<string, string> UserProperties, string propertyName)
        {
            var propertyValue = string.Empty;
            try
            {
                var Prop = UserProperties.FirstOrDefault(w => w.Key.Equals(propertyName, StringComparison.CurrentCultureIgnoreCase));
                if (!Prop.Equals(default(KeyValuePair<string, string>)))
                {
                    propertyValue = Prop.Value;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning("Property name {0} not found with message {1}", propertyName, ex.Message);
            }
            return propertyValue;
        }
    }
}
