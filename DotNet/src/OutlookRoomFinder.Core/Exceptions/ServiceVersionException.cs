using System;
using System.Runtime.Serialization;

namespace OutlookRoomFinder.Core.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a request cannot be handled due to a service version mismatch.
    /// </summary>
    [Serializable]
    public sealed class ServiceVersionException : ServiceLocalException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceVersionException"/> class.
        /// </summary>
        public ServiceVersionException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceVersionException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServiceVersionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceVersionException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ServiceVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OutlookRoomFinder.Core.Exceptions.ServiceVersionException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        private ServiceVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}