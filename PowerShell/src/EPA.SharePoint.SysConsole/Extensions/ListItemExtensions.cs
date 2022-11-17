using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EPA.SharePoint.SysConsole.Extensions
{
    /// <summary>
    /// Provides methods to error check and extract field values
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "CSOM unknown exceptions.")]
    public static class ListItemExtensions
    {
        /// <summary>
        /// Grabs column value and if populated returns string value
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static string RetrieveListItemValue(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return fieldItemValue.ToString();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return string.Empty;
        }

        /// <summary>
        /// Grabs the column value and if populated returns the FieldUserValue object otherwise null
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static string[] RetrieveListItemChoiceValues(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return (string[])fieldItemValue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Grabs the column value and if populated returns the FieldUserValue object otherwise null
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static FieldMultiChoice RetrieveListItemAsChoice(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return (FieldMultiChoice)fieldItemValue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Grabs the column value and if populated returns the FieldUserValue object otherwise null
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static FieldUserValue RetrieveListItemUserValue(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return (FieldUserValue)fieldItemValue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return null;
        }

        public static string ToUserValue(this FieldUserValue fieldItemValue)
        {
            if (fieldItemValue != null)
            {
                return fieldItemValue.LookupValue;
            }
            return string.Empty;
        }

        public static string ToUserEmailValue(this FieldUserValue fieldItemValue)
        {
            if (fieldItemValue != null)
            {
                return fieldItemValue.Email;
            }
            return string.Empty;
        }

        public static string ToUserEmailValue(this Microsoft.SharePoint.Client.User fieldItemValue)
        {
            if (fieldItemValue != null)
            {
                return fieldItemValue.Email;
            }
            return string.Empty;
        }

        /// <summary>
        /// Grabs the column value and if populated returns the FieldUserValue object otherwise null
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static FieldUserValue[] RetrieveListItemUserValues(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return fieldItemValue as FieldUserValue[];
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Parse the field user values into an array of strings
        /// </summary>
        /// <param name="fieldItemValue"></param>
        /// <returns></returns>
        public static IEnumerable<string> ToUserValues(this FieldUserValue[] fieldItemValue)
        {
            if (fieldItemValue != null)
            {
                return fieldItemValue.Select(s => s.ToUserValue());
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Grabs column value and if populated returns field lookup value object
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static FieldLookupValue RetrieveListItemValueAsLookup(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return (FieldLookupValue)fieldItemValue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Grabs column value and if populated returns field lookup value object
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static FieldLookupValue[] RetrieveListItemValueAsLookups(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return (FieldLookupValue[])fieldItemValue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return null;
        }

        public static string ToLookupValue(this FieldLookupValue fieldItemValue)
        {
            if (fieldItemValue != null)
            {
                return fieldItemValue.LookupValue;
            }
            return string.Empty;
        }


        /// <summary>
        /// Grabs column value and if populated returns field hyperlink value object
        /// </summary>
        /// <param name="requestItem"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static FieldUrlValue RetrieveListItemValueAsHyperlink(this ListItem requestItem, string columnName)
        {
            try
            {
                var fieldItemValue = requestItem[columnName];
                if (fieldItemValue != null)
                {
                    return (FieldUrlValue)fieldItemValue;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to retrieve {columnName} listitem {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Will parse the ListItem to pull properties required to download the file to a local directory
        /// </summary>
        /// <param name="item">The ListItem which should be a File</param>
        /// <param name="itemContext">The context for the Web</param>
        /// <param name="targetDirectory">The local file directory full path</param>
        /// <returns>The absolute path to the downloaded local file</returns>
        public static string DownloadFile(this ListItem item, ClientContext itemContext, string targetDirectory)
        {
            var contextWeb = itemContext.Web;
            if (!contextWeb.IsPropertyAvailable(wctx => wctx.Url))
            {
                itemContext.Load(contextWeb, wctx => wctx.Url);
                itemContext.ExecuteQueryRetry();
            }

            if (!item.IsPropertyAvailable("FileRef") || !item.IsPropertyAvailable("FileLeafRef"))
            {
                if (!item.IsPropertyAvailable("FileRef"))
                {
                    itemContext.Load(item, ictx => ictx["FileRef"]);
                }

                if (!item.IsPropertyAvailable("FileLeafRef"))
                {
                    itemContext.Load(item, ictx => ictx["FileLeafRef"]);
                }
                itemContext.ExecuteQueryRetry();
            }

            var fileRelativeUrl = item.RetrieveListItemValue("FileRef");
            var fileNameText = item.RetrieveListItemValue("FileLeafRef");

            var webUrl = new Uri(itemContext.Web.Url);
            var fileUrl = new Uri(webUrl, fileRelativeUrl);

            var baseUrl = fileUrl.GetLeftPart(UriPartial.Authority);
            var fileServerRelativeUrl = fileUrl.ToString().Replace(baseUrl, string.Empty);


            var downloadedFile = System.IO.Path.Combine(targetDirectory, fileNameText);

            // if the file already exists we should delete it
            if (System.IO.File.Exists(downloadedFile))
            {
                System.IO.File.Delete(downloadedFile);
            }

            // download file
            if (!System.IO.File.Exists(downloadedFile))
            {
                var fileAbsoluteUrl = fileUrl.AbsolutePath;
                using var openFile = File.OpenBinaryDirect(itemContext, fileAbsoluteUrl);
                using var fileStream = new System.IO.FileStream(downloadedFile, System.IO.FileMode.Create);
                openFile.Stream.CopyTo(fileStream);
            }
            return downloadedFile;
        }
    }
}
