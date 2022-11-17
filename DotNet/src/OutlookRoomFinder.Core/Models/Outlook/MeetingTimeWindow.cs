using Microsoft.Graph;
using OutlookRoomFinder.Core.Exceptions;
using System;
using System.Globalization;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// Represents a time period.
    /// </summary>
    public sealed class MeetingTimeWindow : ISelfValidate
    {
        const string DateOnlyFormat = "yyyy-MM-ddT00:00:00";

        /// <summary>
        /// Initializes a new instance of the <see cref="MeetingTimeWindow"/> class.
        /// </summary>
        internal MeetingTimeWindow()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeetingTimeWindow"/> class.
        /// </summary>
        /// <param name="startTime">The start date and time.</param>
        /// <param name="endTime">The end date and time.</param>
        /// <exception cref="ArgumentException">Exception if <paramref name="endTime"/> is before <paramref name="startTime"/></exception>
        public MeetingTimeWindow(DateTime startTime, DateTime endTime)
            : this()
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            IsValid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeetingTimeWindow"/> class.
        /// </summary>
        /// <param name="startTime">The start date and time.</param>
        /// <param name="endTime">The end date and time.</param>
        /// <exception cref="ArgumentException">Exception if <paramref name="endTime"/> is before <paramref name="startTime"/></exception>
        public MeetingTimeWindow(string startTime, string endTime)
            : this()
        {
            this.StartTime = DateTime.Parse(startTime);
            this.EndTime = DateTime.Parse(endTime);
            IsValid();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeetingTimeWindow"/> class.
        /// </summary>
        /// <param name="startTime">The start date and time including timezone.</param>
        /// <param name="endTime">The end date and time including timezone.</param>
        /// <param name="formatProvider">The date formater used to parse</param>
        /// <exception cref="ArgumentException">Exception if <paramref name="endTime"/> is before <paramref name="startTime"/></exception>
        /// <exception cref="ArgumentNullException">Exception if <paramref name="startTime"/> or <paramref name="endTime"/> is null</exception>
        public MeetingTimeWindow(DateTimeTimeZone startTime, DateTimeTimeZone endTime, IFormatProvider formatProvider)
            : this()
        {
            if (startTime == null)
            {
                throw new ArgumentNullException(nameof(startTime));
            }
            if (endTime == null)
            {
                throw new ArgumentNullException(nameof(endTime));
            }
            this.StartTime = DateTime.Parse(startTime.DateTime, formatProvider);
            this.EndTime = DateTime.Parse(endTime.DateTime, formatProvider);
            IsValid();
        }

        /// <summary>
        /// Gets or sets the start date and time.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end date and time.
        /// </summary>
        public DateTime EndTime { get; set; }

        internal string StartTimeAsString
        {
            get { return this.StartTime.ToString(DateOnlyFormat, CultureInfo.InvariantCulture); }
        }

        internal string EndTimeAsString
        {
            get { return this.EndTime.ToString(DateOnlyFormat, CultureInfo.InvariantCulture); }
        }

        /// <summary>
        /// Gets the duration.
        /// </summary>
        internal TimeSpan Duration
        {
            get { return this.EndTime - this.StartTime; }
        }

        /// <summary>
        /// Converts duration into XML String (defaults to PT30M)
        /// </summary>
        /// <returns></returns>
        internal string GetGraphTimeSpan()
        {
            if (StartTime != default && EndTime != default)
            {
                return System.Xml.XmlConvert.ToString(Duration);
            }

            return "PT30M";
        }

        #region ISelfValidate Members

        /// <summary>
        /// Validates this instance.
        /// </summary>
        void ISelfValidate.Validate()
        {
            if (this.StartTime >= this.EndTime)
            {
                throw new ArgumentException(ResourceStrings.TimeWindowStartTimeMustBeGreaterThanEndTime);
            }
        }

        #endregion


        /// <summary>
        /// Advances the <seealso cref="StartTime"/> TimeWindow the the specified ticks (DateTime into an big integer)
        /// </summary>
        /// <param name="ticks"></param>
        public void AdvanceDuration(long ticks)
        {
            this.StartTime = this.StartTime.AddTicks(ticks);
        }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public void IsValid()
        {
            if (this.StartTime >= this.EndTime)
            {
                throw new TimeWindowException(this.StartTime.ToString("d"), this.EndTime.ToString("d"), ResourceStrings.TimeWindowStartTimeMustBeGreaterThanEndTime);
            }
        }
    }
}