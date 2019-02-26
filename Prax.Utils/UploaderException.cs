using System;
using System.Runtime.Serialization;

namespace Prax.Utils
{
    public class UploaderException : Exception {
        public UploaderException() {}
        public UploaderException(string message) : base(message) {}
        public UploaderException(string message, Exception innerException) : base(message, innerException) {}
        protected UploaderException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }

    // All those moments will be lost in time, like tears in rain. Time to die.
    public class FatalUploaderException : UploaderException {
        public FatalUploaderException(string message) : base(message) {}
        public FatalUploaderException(string message, Exception innerException) : base(message, innerException) {}
        protected FatalUploaderException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }
}
