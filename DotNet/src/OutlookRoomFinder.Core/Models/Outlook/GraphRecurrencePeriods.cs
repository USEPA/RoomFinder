using Ical.Net.DataTypes;
using System;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrencePeriods
    {
        public GraphRecurrencePeriods() { }

        public GraphRecurrencePeriods(Period period, TimeSpan duration)
        {
            if (period == null)
            {
                throw new ArgumentNullException(nameof(period));
            }
            this.Duration = duration;
            this.StartTime = OnParseIntoTime(period.StartTime);
            this.EndTime = OnParseIntoTime(period.StartTime, period.EndTime, duration);
        }

        public GraphRecurrencePeriods(IDateTime startTime, IDateTime endTime, TimeSpan duration)
        {
            this.Duration = duration;
            this.StartTime = OnParseIntoTime(startTime);
            this.EndTime = OnParseIntoTime(startTime, endTime, duration);
        }

        public GraphRecurrenceDateTime StartTime { get; set; }

        public GraphRecurrenceDateTime EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Parse into controlled Member
        /// </summary>
        /// <param name="periodDateTime"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private GraphRecurrenceDateTime OnParseIntoTime(IDateTime periodDateTime)
        {
            if (periodDateTime == null)
            {
                throw new ArgumentNullException(nameof(periodDateTime));
            }

            var period = new GraphRecurrenceDateTime
            {
                AsSystemLocal = periodDateTime.AsSystemLocal,
                IsUtc = periodDateTime.IsUtc,
                TimeZoneName = periodDateTime.TimeZoneName,
                AsActualDateTime = periodDateTime.Value,
                HasDate = periodDateTime.HasDate,
                HasTime = periodDateTime.HasTime,
                TzId = periodDateTime.TzId,
                AsDateTimeOffset = periodDateTime.AsDateTimeOffset,
                Year = periodDateTime.Year,
                Ticks = periodDateTime.Ticks,
                AsUtc = periodDateTime.AsUtc,
                DayOfWeek = periodDateTime.DayOfWeek,
                DayOfYear = (periodDateTime as CalDateTime).DayOfYear,
            };

            return period;
        }

        /// <summary>
        /// Parse into controlled Member
        /// </summary>
        /// <param name="periodDateTime"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private GraphRecurrenceDateTime OnParseIntoTime(IDateTime periodDateTime, IDateTime endPeriodDateTime, TimeSpan? duration)
        {
            GraphRecurrenceDateTime period;
            if (endPeriodDateTime != null)
            {
                period = OnParseIntoTime(endPeriodDateTime);
            }
            else if (duration == null)
            {
                period = OnParseIntoTime(periodDateTime);
            }
            else
            {
                period = OnParseIntoTime(periodDateTime.AddTicks(duration?.Ticks ?? 0));
            }

            return period;
        }
    }
}
