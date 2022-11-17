using System;
using System.Net;

namespace EPA.Office365.Exceptions
{
    /// <summary>
    /// Defines a Graph URL Exception for later try/catch handling
    /// </summary>
    [Serializable]
    public class GraphWebException : Exception
    {
        private Uri ServiceFullUrl { get; }

        public GraphWebException()
        {
        }

        public GraphWebException(Uri serviceFullUrl, string message) 
            : this(message)
        {
            ServiceFullUrl = serviceFullUrl;
        }

        public GraphWebException(string message) 
            : base(message)
        {
        }

        public GraphWebException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        public GraphWebException(Uri serviceFullUrl, string message, Exception innerException)
            : this(message, innerException)
        {
            ServiceFullUrl = serviceFullUrl;
        }

        protected GraphWebException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) 
            : base(serializationInfo, streamingContext)
        {
        }

        public override string ToString()
        {
            return $"Graph API {ServiceFullUrl} exception: {this.Message}";
        }
    }
}