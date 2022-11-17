using System;
using System.Runtime.Serialization;

namespace OutlookRoomFinder.Core.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a service operation fails locally (e.g. validation error).
    /// </summary>
    [Serializable]
    public class ServiceLocalException : Exception
    {
        /// <summary>
        /// ServiceLocalException Constructor.
        /// </summary>
        public ServiceLocalException()
        {
        }

        /// <summary>
        /// ServiceLocalException Constructor.
        /// </summary>
        /// <param name="message">Error message text.</param>
        public ServiceLocalException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// ServiceLocalException Constructor.
        /// </summary>
        /// <param name="message">Error message text.</param>
        /// <param name="innerException">Inner exception.</param>
        public ServiceLocalException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OutlookRoomFinder.Core.Exceptions.ServiceLocalException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected ServiceLocalException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}