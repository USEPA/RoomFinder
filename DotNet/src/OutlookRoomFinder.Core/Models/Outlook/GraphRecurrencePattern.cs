using Ical.Net.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrencePattern
    {
        public GraphRecurrencePattern()
        {
            this.Calendar = new GraphRecurrenceCalendar(CultureInfo.CurrentCulture.Calendar);
        }

        public GraphRecurrencePattern(RecurrencePattern pattern)
            : this()
        {
            this.Pattern = pattern;
            this.Periods = new HashSet<GraphRecurrencePeriods>();
        }

        public DateTimeOffset EvaluationStartBounds { get; set; }

        public DateTimeOffset EvaluationEndBounds { get; set; }

        public int DurationMinutes { get; set; }

        public HashSet<GraphRecurrencePeriods> Periods { get; set; }

        public GraphRecurrenceTimeZone RecurrenceTimeZone { get; set; }

        public GraphRecurrenceCalendar Calendar { get; set; }

        protected RecurrencePattern Pattern { get; set; }
    }
}
