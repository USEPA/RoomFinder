using System;

namespace EPA.Office365.Exceptions
{
    /// <summary>
    /// Defines a Maximum Retry Attemped Exception
    /// </summary>
    [Serializable]
    public class MaximumRetryAttemptedException : Exception
    {
        private int RetriesAttempted { get; }

        public MaximumRetryAttemptedException()
        {
            RetriesAttempted = 0;
        }

        public MaximumRetryAttemptedException(int retriesAttempted, string message) : this(message)
        {
            RetriesAttempted = retriesAttempted;
        }

        public MaximumRetryAttemptedException(string message) : base(message)
        {
        }

        public MaximumRetryAttemptedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MaximumRetryAttemptedException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }

        public override string ToString()
        {
            return $"Retries {RetriesAttempted} attempted with message {this.Message}";
        }
    }
}
