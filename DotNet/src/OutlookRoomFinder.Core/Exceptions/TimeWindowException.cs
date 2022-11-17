using System;
using System.Runtime.Serialization;

namespace OutlookRoomFinder.Core.Exceptions
{
    [Serializable]
    public class TimeWindowException : Exception
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }

        public TimeWindowException()
        {
        }

        public TimeWindowException(string startTime, string endTime)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        public TimeWindowException(string startTime, string endTime, string message) : base(message)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        public TimeWindowException(string startTime, string endTime, string message, Exception innerException) : base(message, innerException)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
        }

        public TimeWindowException(string message) : base(message)
        {
        }

        public TimeWindowException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OutlookRoomFinder.Core.Exceptions.TimeWindowException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected TimeWindowException(SerializationInfo serializationInfo, StreamingContext streamingContext)
             : base(serializationInfo, streamingContext)
        {
        }
    }
}
