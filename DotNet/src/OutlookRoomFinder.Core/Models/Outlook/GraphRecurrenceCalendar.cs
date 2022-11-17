using System;
using System.Collections.Generic;
using System.Globalization;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    public class GraphRecurrenceCalendar
    {
        public GraphRecurrenceCalendar()
        {
        }

        public GraphRecurrenceCalendar(Calendar calendar)
        {
            if (calendar == null)
            {
                throw new ArgumentNullException(nameof(calendar));
            }
            this.MaxSupportedDateTime = calendar.MaxSupportedDateTime;
            this.IsReadOnly = calendar.IsReadOnly;
            this.Eras = calendar.Eras;
            this.AlgorithmType = calendar.AlgorithmType;
            this.MinSupportedDateTime = calendar.MinSupportedDateTime;
            this.TwoDigitYearMax = calendar.TwoDigitYearMax;
        }

        public virtual DateTime MaxSupportedDateTime { get; set; }

        public bool IsReadOnly { get; set; }

        public IList<int> Eras { get; set; }

        public virtual CalendarAlgorithmType AlgorithmType { get; set; }

        public virtual DateTime MinSupportedDateTime { get; set; }

        public virtual int TwoDigitYearMax { get; set; }

        protected virtual int DaysInYearBeforeMinSupportedYear { get; set; }
    }
}
