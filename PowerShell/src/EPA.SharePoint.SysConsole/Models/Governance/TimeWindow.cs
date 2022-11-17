using System;

namespace EPA.SharePoint.SysConsole.Models.Governance
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "timewindow parser")]
    public sealed class TimeWindow : ISelfValidate
    {
        internal TimeWindow()
        {
        }

        public TimeWindow(DateTime startTime, DateTime endTime) 
            : this()
        {
            this.m_startTime = startTime;
            this.m_endTime = endTime;
        }


        void ISelfValidate.Validate()
        {
            if (this.StartTime >= this.EndTime)
            {
                throw new ArgumentException(Office365.CoreResources.TimeWindowStartTimeMustBeGreaterThanEndTime);
            }
        }


        internal TimeSpan Duration
        {
            get
            {
                return this.m_endTime - this.m_startTime;
            }
        }

        public double? TotalDays
        {
            get
            {
                double? totalDays;
                try
                {
                    totalDays = Duration.TotalDays;
                }
                catch
                {
                    totalDays = -1;
                }
                return totalDays;
            }
        }

        private DateTime m_endTime { get; set; }
        public DateTime EndTime
        {
            get
            {
                return this.m_endTime;
            }
            set
            {
                this.m_endTime = value;
            }
        }

        private DateTime m_startTime { get; set; }
        public DateTime StartTime
        {
            get
            {
                return this.m_startTime;
            }
            set
            {
                this.m_startTime = value;
            }
        }
    }
}

