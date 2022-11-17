using System;
using System.Runtime.Serialization;

namespace OutlookRoomFinder.Core.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a validation check fails.
    /// </summary>
    [Serializable]
    public sealed class ServiceValidationException : ServiceLocalException
    {
        /// <summary>
        /// ServiceValidationException Constructor.
        /// </summary>
        public ServiceValidationException()
        {
        }

        /// <summary>
        /// ServiceValidationException Constructor.
        /// </summary>
        /// <param name="message">Error message text.</param>
        public ServiceValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// ServiceValidationException Constructor.
        /// </summary>
        /// <param name="message">Error message text.</param>
        /// <param name="innerException">Inner exception.</param>
        public ServiceValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OutlookRoomFinder.Core.Exceptions.ServiceValidationException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        private ServiceValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}