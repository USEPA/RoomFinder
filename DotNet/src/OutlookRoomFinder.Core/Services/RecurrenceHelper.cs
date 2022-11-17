using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using OutlookRoomFinder.Core.Models.Filter;
using OutlookRoomFinder.Core.Models.Outlook;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TimeZoneConverter;
using MSGraph = Microsoft.Graph;

namespace OutlookRoomFinder.Core
{
    public class RecurrenceHelper
    {
        public RecurrenceHelper(ILogger logger)
        {
            Logger = logger;
            dayPairs = new List<KeyValuePair<string, DayOfWeek>>
            {
                new KeyValuePair<string, DayOfWeek>("sun", DayOfWeek.Sunday),
                new KeyValuePair<string, DayOfWeek>("mon", DayOfWeek.Monday),
                new KeyValuePair<string, DayOfWeek>("tue", DayOfWeek.Tuesday),
                new KeyValuePair<string, DayOfWeek>("wed", DayOfWeek.Wednesday),
                new KeyValuePair<string, DayOfWeek>("thu", DayOfWeek.Thursday),
                new KeyValuePair<string, DayOfWeek>("fri", DayOfWeek.Friday),
                new KeyValuePair<string, DayOfWeek>("sat", DayOfWeek.Saturday)
            };

            monthPairs = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("jan", 1),
                new KeyValuePair<string, int>("feb", 2),
                new KeyValuePair<string, int>("mar", 3),
                new KeyValuePair<string, int>("apr", 4),
                new KeyValuePair<string, int>("may", 5),
                new KeyValuePair<string, int>("jun", 6),
                new KeyValuePair<string, int>("jul", 7),
                new KeyValuePair<string, int>("aug", 8),
                new KeyValuePair<string, int>("sep", 9),
                new KeyValuePair<string, int>("oct", 10),
                new KeyValuePair<string, int>("nov", 11),
                new KeyValuePair<string, int>("dec", 12)
            };
        }

        internal ILogger Logger { get; private set; }
        private readonly List<KeyValuePair<string, DayOfWeek>> dayPairs;
        private readonly List<KeyValuePair<string, int>> monthPairs;


        public GraphRecurrencePattern Evaluate(FindResourceRecurrenceFilter filter, bool includeReferenceDateInResults)
        {
            if (filter?.Recurrence == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            var graphRecurrence = filter.Recurrence;

            var timeZoneInfo = TZConvert.GetTimeZoneInfo(filter.Recurrence.RecurrenceTimeZone.Name);

            var startDateTimeZone = MSGraph.DateTimeTimeZone.FromDateTime(DateTime.Parse(filter.RoomFilter.Start), timeZoneInfo);
            var endDateTimeZone = MSGraph.DateTimeTimeZone.FromDateTime(DateTime.Parse(filter.RoomFilter.End), timeZoneInfo);

            var meetingWindows = new MeetingTimeWindow(startDateTimeZone, endDateTimeZone, CultureInfo.DefaultThreadCurrentUICulture);
            var evtStart = new CalDateTime(meetingWindows.StartTime);

            var endTimeset = evtStart.AddMinutes(graphRecurrence.SeriesTime.DurationMinutes);
            var evtEnd = new CalDateTime(graphRecurrence.SeriesTime.EndYear, graphRecurrence.SeriesTime.EndMonth, graphRecurrence.SeriesTime.EndDay, endTimeset.Hour, endTimeset.Minute, endTimeset.Second);

            var recurrence = BuildXml(graphRecurrence, evtEnd.Value);
            var i = new RecurrencePatternEvaluator(recurrence)
            {
                EvaluationStartBounds = evtStart.Value,
                EvaluationEndBounds = evtStart.Value.AddMinutes(graphRecurrence.SeriesTime.DurationMinutes)
            };

            IDateTime evtReferenceDate = new CalDateTime(evtStart.Value, TimeZoneInfo.Local.Id);
            var evaluation = i.Evaluate(evtReferenceDate, evtStart.Value, evtEnd.Value, includeReferenceDateInResults);
            System.Diagnostics.Trace.TraceInformation($"Discovered {evaluation.Count} starting at {i.EvaluationStartBounds}");

            TimeSpan duration = i.EvaluationEndBounds.Subtract(i.EvaluationStartBounds);

            var pattern = new GraphRecurrencePattern(recurrence)
            {
                EvaluationStartBounds = evtStart.AsDateTimeOffset,
                EvaluationEndBounds = evtStart.AsDateTimeOffset.AddMinutes(graphRecurrence.SeriesTime.DurationMinutes),
                DurationMinutes = graphRecurrence.SeriesTime.DurationMinutes,
                RecurrenceTimeZone = graphRecurrence.RecurrenceTimeZone,
                Calendar = new GraphRecurrenceCalendar(i.Calendar),
                Periods = new HashSet<GraphRecurrencePeriods>(evaluation.Select(period => { return new GraphRecurrencePeriods(period.StartTime, null, duration); }))
            };


            Logger.Logging(LogEventLevel.Information, $"Offset TZ {timeZoneInfo.Id} Start=>{pattern.EvaluationStartBounds} // End=>{pattern.EvaluationEndBounds}");

            return pattern;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1541:Methods and properties should not be too complex", Justification = "Finite criteria, too challenging to split.")]
        public RecurrencePattern BuildXml(GraphRecurrence graphRecurrence, DateTime endDateTime)
        {
            if (graphRecurrence?.RecurrenceType == null)
            {
                throw new ArgumentNullException(nameof(graphRecurrence));
            }

            var rp = new RecurrencePattern(graphRecurrence.RecurrenceType)
            {
                Until = endDateTime,
                Interval = (graphRecurrence.RecurrenceProperties?.Interval == 0) ? 1 : (graphRecurrence.RecurrenceProperties?.Interval ?? 1)
            };

            if (graphRecurrence.RecurrenceProperties.Days?.Any() == true)
            {
                rp.ByDay = new List<WeekDay>();
                foreach (var dayOfWeek in graphRecurrence.RecurrenceProperties.Days)
                {
                    var byDayOfWeek = dayPairs.FirstOrDefault(dp => dp.Key.Equals(dayOfWeek, StringComparison.OrdinalIgnoreCase));
                    rp.ByDay.Add(new WeekDay(byDayOfWeek.Value));
                }
            }

            if (!string.IsNullOrEmpty(graphRecurrence.RecurrenceProperties.FirstDayOfWeek))
            {
                var firstDayOfWeekValue = graphRecurrence.RecurrenceProperties.FirstDayOfWeek;
                var firstDayOfWeek = dayPairs.FirstOrDefault(dp => dp.Key.Equals(firstDayOfWeekValue, StringComparison.OrdinalIgnoreCase));
                rp.FirstDayOfWeek = firstDayOfWeek.Value;
            }

            if (!string.IsNullOrEmpty(graphRecurrence.RecurrenceProperties.DayOfWeek))
            {
                var dayOfWeekValue = graphRecurrence.RecurrenceProperties.DayOfWeek;
                var dayOfWeek = dayPairs.FirstOrDefault(dp => dp.Key.Equals(dayOfWeekValue, StringComparison.OrdinalIgnoreCase));
                rp.ByDay.Add(graphRecurrence.RecurrenceProperties.WeekNumber.HasValue ? new WeekDay(dayOfWeek.Value, graphRecurrence.RecurrenceProperties.WeekNumber ?? Ical.Net.FrequencyOccurrence.First) : new WeekDay(dayOfWeek.Value));
            }

            if (graphRecurrence.RecurrenceProperties.DayOfMonth.HasValue)
            {
                rp.ByMonthDay.Add(graphRecurrence.RecurrenceProperties.DayOfMonth ?? 1);
            }

            if (!string.IsNullOrEmpty(graphRecurrence.RecurrenceProperties.Month))
            {
                var monthOfYearValue = graphRecurrence.RecurrenceProperties.Month;
                var monthOfYear = monthPairs.FirstOrDefault(dp => dp.Key.Equals(monthOfYearValue, StringComparison.OrdinalIgnoreCase));
                rp.ByMonth.Add(monthOfYear.Value);
            }

            System.Diagnostics.Trace.TraceInformation($"Recurrence Pattern {rp}");
            return rp;
        }

        public AppointmentMeetingTimes GetAppointments(int setIndex, int setSize, GraphRecurrencePattern recurrence)
        {
            if (recurrence == null)
            {
                throw new ArgumentNullException(nameof(recurrence));
            }
            var result = new AppointmentMeetingTimes
            {
                Meetings = new List<MeetingTimeWindow>(),
                IsLastSet = false
            };
            DateTime endSeriesOccurence = DateTime.MaxValue;

            MeetingTimeWindow masterInstance = new MeetingTimeWindow(recurrence.EvaluationStartBounds.DateTime, recurrence.EvaluationEndBounds.DateTime);
            

            var numberOfOccurrences = recurrence.Periods?.Count() ?? 0;
            if (numberOfOccurrences > 0)
            {
                endSeriesOccurence = recurrence.Periods.Select(ob => ob.StartTime.AsUtc).OrderByDescending(ob => ob).FirstOrDefault();
            }
            if (setSize == -1) // Return all Occurrences in Result
            {
                setSize = numberOfOccurrences;
            }
            var skip = (setIndex * setSize);

            var offSet = TZConvert.GetTimeZoneInfo(recurrence.RecurrenceTimeZone?.Name ?? TimeZoneInfo.Utc.Id);

            // Final step - convert from UTC to local time
            result.Meetings = ProcessTimeOccurences(recurrence, endSeriesOccurence, masterInstance, numberOfOccurrences, setSize, skip, offSet);
            result.IsLastSet = result.Meetings.Count == 0 || result.Meetings.Last().StartTime.Date >= endSeriesOccurence.Date;

            return result;
        }

        private IList<MeetingTimeWindow> ProcessTimeOccurences(GraphRecurrencePattern recurrence, DateTime endSeriesOccurence, MeetingTimeWindow masterInstance, int numberOfOccurrences, int setSize, int skip, TimeZoneInfo offSet)
        {
            MeetingTimeWindow lastOccurence = masterInstance;
            var recurrenceTicks = masterInstance.Duration.Ticks;
            var meetings = new List<MeetingTimeWindow>();
            for (int occurenceIndex = skip;
                meetings.Count < setSize && occurenceIndex < numberOfOccurrences && lastOccurence.StartTime.Date <= endSeriesOccurence;
                occurenceIndex++)
            {
                var recurrencePeriod = recurrence.Periods.ElementAtOrDefault(occurenceIndex);
                var offsetStartTime = RetreiveOffset(recurrencePeriod.StartTime.AsActualDateTime, offSet);
                var offsetEndTime = RetreiveOffset(recurrencePeriod.EndTime?.AsActualDateTime ?? recurrencePeriod.StartTime.AsActualDateTime.AddTicks(recurrenceTicks), offSet);
                MeetingTimeWindow nextEvaluateOccurence = new MeetingTimeWindow(offsetStartTime, offsetEndTime);

                if (occurenceIndex >= skip
                    && nextEvaluateOccurence != null && nextEvaluateOccurence.StartTime <= endSeriesOccurence)
                {
                    meetings.Add(nextEvaluateOccurence);
                }

                lastOccurence = nextEvaluateOccurence;
            }

            return meetings;
        }

        public DateTime RetreiveOffset(DateTimeOffset time, TimeZoneInfo destinationTimeZone)
        {
            if (destinationTimeZone == null)
            {
                throw new ArgumentNullException(nameof(destinationTimeZone));
            }

            var offsetTime = time.DateTime;
            DateTime convertedTime;
            TimeSpan offset;

            if (offsetTime.Kind == DateTimeKind.Local && !destinationTimeZone.Equals(TimeZoneInfo.Local))
            {
                convertedTime = TimeZoneInfo.ConvertTime(offsetTime, TimeZoneInfo.Local, destinationTimeZone);
            }
            else if (offsetTime.Kind == DateTimeKind.Utc && !destinationTimeZone.Equals(TimeZoneInfo.Utc))
            {
                convertedTime = TimeZoneInfo.ConvertTime(offsetTime, TimeZoneInfo.Utc, destinationTimeZone);
            }
            else
            {
                convertedTime = time.DateTime;
            }

            offset = destinationTimeZone.GetUtcOffset(time);
            if (time.DateTime == convertedTime)
            {
                Logger.Logging(LogEventLevel.Information, $"{time} {(destinationTimeZone.IsDaylightSavingTime(time) ? destinationTimeZone.DaylightName : destinationTimeZone.StandardName)}");
                Logger.Logging(LogEventLevel.Information, $"=>It differs from UTC by {offset.Hours} hours, {offset.Minutes} minutes.");
            }
            else
            {
                Logger.Logging(LogEventLevel.Information, $"{time} {(offsetTime.Kind == DateTimeKind.Utc ? "UTC" : TimeZoneInfo.Local.Id)} ");
                Logger.Logging(LogEventLevel.Information, $"=>converts to {convertedTime} {destinationTimeZone.Id}.");
                Logger.Logging(LogEventLevel.Information, $"=>It differs from UTC by {offset.Hours} hours, {offset.Minutes} minutes.");
            }

            return convertedTime;
        }
    }

}
