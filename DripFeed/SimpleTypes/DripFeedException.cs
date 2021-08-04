using System;
using System.Runtime.Serialization;

namespace DripFeed
{
    [Serializable]
    internal class DripFeedException : Exception
    {
        public DripFeedException()
        {
        }

        public DripFeedException(string message) : base(message)
        {
        }

        public DripFeedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DripFeedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}