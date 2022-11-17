using Microsoft.Graph;
using OutlookRoomFinder.Core.Models;
using OutlookRoomFinder.Core.Models.FileModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OutlookRoomFinder.Core.Extensions
{
    public static class ResourceExtensions
    {
        internal const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";

        public static RestrictionType GetResourceRestriction(this IResourceJsonObject resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            var restrictedAttribute = resource.RestrictionType;
            if (string.Compare(restrictedAttribute, "ApprovalRequired", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                return RestrictionType.ApprovalRequired;
            }
            else if (string.Compare(restrictedAttribute, "Restricted", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                return RestrictionType.Restricted;
            }
            else if (string.Compare(restrictedAttribute, "Disabled", StringComparison.CurrentCultureIgnoreCase) == 0) // dont show in add in.
            {
                return RestrictionType.Restricted;
            }
            else
            {
                return RestrictionType.None;
            }
        }

        /// <summary>
        /// Converts an nullable collection into an empty collection or returns the current collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataSet"></param>
        /// <returns></returns>
        public static IEnumerable<T> ConvertIntoSureEnumerable<T>(this IEnumerable<T> dataSet) where T : class
        {
            return dataSet ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Determines whether every element in the collection matches the conditions defined by the specified predicate.
        /// </summary>
        /// <typeparam name="T">Entry type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="predicate">Predicate that defines the conditions to check against the elements.</param>
        /// <returns>True if every element in the collection matches the conditions defined by the specified predicate; otherwise, false.</returns>
        internal static bool TrueForAll<T>(this IEnumerable<T> collection, Predicate<T> predicate)
        {
            foreach (T entry in collection)
            {
                if (!predicate(entry))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Call an action for each member of a collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="action">The action to apply.</param>
        /// <typeparam name="T">Collection element type.</typeparam>
        internal static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (T entry in collection)
            {
                action(entry);
            }
        }

        /// <summary>
        /// Converts the DataTimeTimeZone into a current representation of the DateTime, converts UTC to Local
        /// </summary>
        /// <param name="icalDate"></param>
        /// <returns></returns>
        public static DateTime ConvertUtcDateToLocalTimeZone(this DateTimeTimeZone icalDate) =>
            ConvertDateToTimeZone(icalDate, TimeZoneInfo.Utc, TimeZoneInfo.Local);


        /// <summary>
        /// Converts the DataTimeTimeZone into a current representation of the DateTime, converting from sourceTimeZone into destinationTimeZone
        /// </summary>
        /// <param name="icalDate"></param>
        /// <param name="sourceTimeZone">The source timezone from DateTimeTimeZone</param>
        /// <param name="destinationTimeZone">The destination or desired timezone for the DateTime</param>
        /// <returns></returns>
        public static DateTime ConvertDateToTimeZone(this DateTimeTimeZone icalDate, TimeZoneInfo sourceTimeZone, TimeZoneInfo destinationTimeZone)
        {
            _ = icalDate ?? throw new ArgumentNullException(nameof(icalDate));
            var timeZoneDate = icalDate.ToDateTime();
            return TimeZoneInfo.ConvertTime(timeZoneDate, sourceTimeZone, destinationTimeZone);
        }

        public static DateTime ToDateTime(this DateTimeTimeZone dateTimeTimeZone)
        {
            _ = dateTimeTimeZone ?? throw new ArgumentNullException(nameof(dateTimeTimeZone));

            DateTime dateTime = DateTime.ParseExact(dateTimeTimeZone.DateTime, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            // Now we need to determine which DateTimeKind to set based on the time zone specified in the input object.
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(dateTimeTimeZone.TimeZone);

            DateTimeKind kind;
            if (timeZoneInfo.Id == TimeZoneInfo.Utc.Id)
            {
                kind = DateTimeKind.Utc;
            }
            else if (timeZoneInfo.Id == TimeZoneInfo.Local.Id)
            {
                kind = DateTimeKind.Local;
            }
            else
            {
                kind = DateTimeKind.Unspecified;
            }

            return DateTime.SpecifyKind(dateTime, kind);
        }

        /// <summary>
        /// Converts DateTimeTimeZone which is a Complex Type to DateTimeOffset
        /// </summary>
        /// <param name="dateTimeTimeZone"></param>
        /// <returns></returns>
        public static DateTimeOffset ToDateTimeOffset(this DateTimeTimeZone dateTimeTimeZone)
        {
            _ = dateTimeTimeZone ?? throw new ArgumentNullException(nameof(dateTimeTimeZone));
            // The resulting DateTimeOffset will have the correct offset for the time zone specified in the input object.

            DateTime dateTime = DateTime.ParseExact(dateTimeTimeZone.DateTime, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(dateTimeTimeZone.TimeZone);
            return dateTime.ToDateTimeOffset(timeZoneInfo);
        }

        internal static DateTimeOffset ToDateTimeOffset(this DateTime dateTime, TimeZoneInfo timeZoneInfo)
        {
            TimeSpan offset;
            if (timeZoneInfo.IsAmbiguousTime(dateTime))
            {
                // Ambiguous times happen when during backward transitions, such as the end of daylight saving time.
                // Since we were just told this time is ambiguous, there will always be exactly two offsets in the following array:
                TimeSpan[] offsets = timeZoneInfo.GetAmbiguousTimeOffsets(dateTime);

                // A reasonable common practice is to pick the first occurrence, which is always the largest offset in the array.
                // Ex: 2019-11-03T01:30:00 in the Pacific time zone occurs twice.  First at 1:30 PDT (-07:00), then an hour later
                //     at 1:30 PST (-08:00).  We choose PDT (-07:00) because it comes first sequentially.
                offset = TimeSpan.FromMinutes(Math.Max(offsets[0].TotalMinutes, offsets[1].TotalMinutes));
            }
            else if (timeZoneInfo.IsInvalidTime(dateTime))
            {
                // Invalid times happen during the gap created by a forward transition, such as the start of daylight saving time.
                // While they are not values that actually occur in correct local time, they are sometimes encountered
                // in scenarios such as a scheduled daily task.  In such cases, a reasonable common practice is to advance
                // to a valid local time by the amount of the transition gap (usually 1 hour).
                // Ex: 2019-03-10T02:30:00 does not exist in Pacific time.
                //     The local time went from 01:59:59 PST (-08:00) directly to 03:00:00 PDT (-07:00).
                //     We will advance by 1 hour to 03:30:00 which is in PDT (-07:00).

                // The gap is usually 1 hour, but not always - so it must be calculated
                TimeSpan earlierOffset = timeZoneInfo.GetUtcOffset(dateTime.AddDays(-1));
                TimeSpan laterOffset = timeZoneInfo.GetUtcOffset(dateTime.AddDays(1));
                TimeSpan gap = laterOffset - earlierOffset;

                dateTime = dateTime.Add(gap);
                offset = laterOffset;
            }
            else
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                offset = timeZoneInfo.GetUtcOffset(dateTime);
            }

            return new DateTimeOffset(dateTime, offset);
        }
    }
}
