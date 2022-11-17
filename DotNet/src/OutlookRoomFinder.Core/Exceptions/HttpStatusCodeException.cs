using System;
using System.Runtime.Serialization;

namespace OutlookRoomFinder.Core.Exceptions
{
    [Serializable]
    public class HttpStatusCodeException : Exception
    {
        /// <summary>
        /// for status codes, see https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.statuscodes
        /// </summary>
        public int StatusCode { get; set; }

        public HttpStatusCodeException(int statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCodeException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCodeException(int statusCode, string message, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCodeException()
        {
        }

        public HttpStatusCodeException(string message) : base(message)
        {
        }

        public HttpStatusCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:OutlookRoomFinder.Core.Exceptions.HttpStatusCodeException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected HttpStatusCodeException(SerializationInfo serializationInfo, StreamingContext streamingContext)
             : base(serializationInfo, streamingContext)
        {
        }
    }
}
