using System;

namespace OutlookRoomFinder.Core.Exceptions
{
    // We use this class to indicate that the inner exception has been logged/traced, but it's
    // wrapped and passed on to the caller to indicate failure
    [Serializable]
    public class LoggedException : Exception
    {
        public LoggedException()
        {
        }

        public LoggedException(string message) : base(message)
        {
        }

        public LoggedException(Exception ex) : this(ex?.Message, ex)
        {

        }

        public LoggedException(string message, Exception ex) : base(message, ex)
        {

        }

        /// <summary>
        /// Initializes a new instance of the class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected LoggedException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
             : base(serializationInfo, streamingContext)
        {
        }
    }
}
